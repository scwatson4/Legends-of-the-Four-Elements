using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boot Camp: an interactive tutorial that watches for REAL player actions
/// and advances step by step - select, move, found your base, harvest,
/// train, control groups, attack-move, win a fight, befriend a spirit.
/// Attached automatically by CampaignManager on tutorial levels
/// (CampaignLevel.isTutorial); it builds its own hint panel, so nothing
/// needs wiring. Also usable standalone: drop it in any scene.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private class Step
    {
        public string hint;
        public Func<bool> isComplete;
        public Action onEnter;
    }

    private readonly List<Step> steps = new List<Step>();
    private int currentStep = -1;

    private GameObject panel;
    private TextMeshProUGUI hintLabel;

    private int unitCountAtStepStart;
    private bool enemySeen;

    private void Start()
    {
        BuildUI();
        BuildSteps();
        Advance();
    }

    private void Update()
    {
        if (currentStep < 0 || currentStep >= steps.Count) return;
        if (GameManager.Instance != null && GameManager.Instance.GameIsOver)
        {
            Destroy(gameObject.GetComponent<TutorialManager>());
            return;
        }

        if (steps[currentStep].isComplete())
        {
            Advance();
        }
    }

    private void Advance()
    {
        currentStep++;
        if (currentStep >= steps.Count)
        {
            SetHint("Training complete - finish the mission, Commander!");
            Destroy(panel, 8f);
            enabled = false;
            return;
        }

        Step step = steps[currentStep];
        step.onEnter?.Invoke();
        SetHint($"({currentStep + 1}/{steps.Count})  {step.hint}");
    }

    // ------------------------------------------------------------------
    // The Boot Camp curriculum
    // ------------------------------------------------------------------

    private void BuildSteps()
    {
        steps.Add(new Step
        {
            hint = "Left-click one of your units to select it. (Your Avatar has a golden glow of destiny... or will, once you add VFX.)",
            isComplete = AnyOwnedUnitSelected
        });

        steps.Add(new Step
        {
            hint = "Right-click on open ground to move your unit.",
            isComplete = AnyOwnedUnitMoving
        });

        steps.Add(new Step
        {
            hint = "Hold left-click and DRAG a box around both your units to select them together.",
            isComplete = () => SelectedOwnedCount() >= 2
        });

        steps.Add(new Step
        {
            hint = "Press B and place your COMMAND CENTER on flat ground (green ghost = valid, R rotates, right-click cancels).",
            isComplete = OwnedCommandCenterExists
        });

        steps.Add(new Step
        {
            hint = "Your worker gathers silver automatically - or right-click a resource node to assign them. Get them harvesting.",
            isComplete = AnyWorkerNearNode
        });

        steps.Add(new Step
        {
            hint = "Train a new unit: select the command center's build buttons (or your production panel).",
            onEnter = () => unitCountAtStepStart = OwnedUnitCount(),
            isComplete = () => OwnedUnitCount() > unitCountAtStepStart
        });

        steps.Add(new Step
        {
            hint = "Assign a CONTROL GROUP: select your army, then press Ctrl+1. Press 1 anytime to reselect them.",
            isComplete = () => (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                               Input.GetKeyDown(KeyCode.Alpha1) && SelectedOwnedCount() > 0
        });

        steps.Add(new Step
        {
            hint = "ATTACK-MOVE: select your army, press F, then left-click distant ground. They'll fight anything they meet.",
            isComplete = () => Input.GetKeyDown(KeyCode.F) && SelectedOwnedCount() > 0
        });

        steps.Add(new Step
        {
            hint = "Fire Nation scouts inbound! Destroy them. (Tip: your Avatar cycles elements with T.)",
            onEnter = () => enemySeen = false,
            isComplete = () =>
            {
                bool hostilesAlive = AnyHostileUnitsAlive();
                if (hostilesAlive) enemySeen = true;
                return enemySeen && !hostilesAlive;
            }
        });

        steps.Add(new Step
        {
            hint = "A friendly spirit wanders nearby. Select your AVATAR and right-click the spirit - only the Avatar can energy-bend it to your side.",
            isComplete = () => !AnyUntamedTameableExists() // tamed, or none in the scene
        });
    }

    // ------------------------------------------------------------------
    // Condition helpers
    // ------------------------------------------------------------------

    private static bool AnyOwnedUnitSelected()
    {
        if (UnitSelectionManager.Instance == null) return false;
        foreach (GameObject go in UnitSelectionManager.Instance.selectedUnitsList)
        {
            if (go != null && FactionUtility.IsLocallyControlled(go)) return true;
        }
        return false;
    }

    private static int SelectedOwnedCount()
    {
        if (UnitSelectionManager.Instance == null) return 0;
        int count = 0;
        foreach (GameObject go in UnitSelectionManager.Instance.selectedUnitsList)
        {
            if (go != null && FactionUtility.IsLocallyControlled(go)) count++;
        }
        return count;
    }

    private static bool AnyOwnedUnitMoving()
    {
        if (UnitSelectionManager.Instance == null) return false;
        foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
        {
            if (go == null || !FactionUtility.IsLocallyControlled(go)) continue;
            UnityEngine.AI.NavMeshAgent agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.2f) return true;
        }
        return false;
    }

    private static int OwnedUnitCount()
    {
        if (UnitSelectionManager.Instance == null) return 0;
        int count = 0;
        foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
        {
            if (go != null && FactionUtility.IsLocallyControlled(go)) count++;
        }
        return count;
    }

    private static bool OwnedCommandCenterExists()
    {
        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(cc.gameObject)) return true;
        }
        return false;
    }

    private static bool AnyWorkerNearNode()
    {
        foreach (ResourceCollector collector in FindObjectsByType<ResourceCollector>(FindObjectsSortMode.None))
        {
            if (!FactionUtility.IsLocallyControlled(collector.gameObject)) continue;
            foreach (ResourceNode node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (Vector3.Distance(collector.transform.position, node.transform.position) < 6f) return true;
            }
        }
        return false;
    }

    private static bool AnyHostileUnitsAlive()
    {
        if (UnitSelectionManager.Instance == null) return false;
        foreach (GameObject go in UnitSelectionManager.Instance.allUnitsList)
        {
            if (go == null) continue;
            int factionId = FactionUtility.GetFactionId(go);
            if (factionId == FactionManager.HostileSpiritsFaction) continue; // spirits aren't the lesson
            if (FactionManager.AreHostile(FactionManager.LocalPlayerFactionId, factionId)) return true;
        }
        return false;
    }

    private static bool AnyUntamedTameableExists()
    {
        foreach (Tameable tameable in FindObjectsByType<Tameable>(FindObjectsSortMode.None))
        {
            if (!tameable.IsTamed) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------
    // Auto-built hint UI (top-center banner)
    // ------------------------------------------------------------------

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("TutorialCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        panel = new GameObject("HintPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGo.transform, false);
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.05f, 0.07f, 0.05f, 0.85f);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 0.9f);
        rect.anchorMax = new Vector2(0.8f, 0.985f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject labelGo = new GameObject("Hint", typeof(RectTransform));
        labelGo.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.02f, 0.05f);
        labelRect.anchorMax = new Vector2(0.98f, 0.95f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        hintLabel = labelGo.AddComponent<TextMeshProUGUI>();
        hintLabel.fontSize = 19;
        hintLabel.alignment = TextAlignmentOptions.Center;
        hintLabel.color = new Color(1f, 0.95f, 0.75f);
        hintLabel.textWrappingMode = TextWrappingModes.Normal;
    }

    private void SetHint(string text)
    {
        Debug.Log($"[Tutorial] {text}");
        if (hintLabel != null) hintLabel.text = text;
    }
}
