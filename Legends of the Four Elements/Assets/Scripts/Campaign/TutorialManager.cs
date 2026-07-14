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
///
/// NATION ACADEMIES: on a campaign level that forces a nation (the optional
/// prologue academies), the curriculum switches to that element's deep dive -
/// Air mobility, Water sustain, Earth fortification, Fire aggression - with
/// each technique confirmed through TutorialSignals counters.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public enum Track { General = 0, Air = 1, Water = 2, Earth = 3, Fire = 4 }

    [Tooltip("Which curriculum runs. Campaign academies set this automatically.")]
    public Track track = Track.General;

    private class Step
    {
        public string hint;
        public Func<bool> isComplete;   // null = an informational tip...
        public float autoAdvanceAfter;  // ...that advances itself after this long
        public Action onEnter;
    }

    private readonly List<Step> steps = new List<Step>();
    private int currentStep = -1;
    private float stepTimer;

    private GameObject panel;
    private TextMeshProUGUI hintLabel;

    private int unitCountAtStepStart;
    private bool enemySeen;

    private void Start()
    {
        ResolveTrack();
        BuildUI();
        BuildSteps();
        Advance();
    }

    /// <summary>Campaign academies force a nation; teach that nation.</summary>
    private void ResolveTrack()
    {
        if (track != Track.General) return; // hand-set in the inspector wins
        CampaignLevel level = CampaignManager.Instance != null ? CampaignManager.Instance.CurrentLevel : null;
        if (level == null || !level.forcesNation) return;

        switch (level.forcedNation)
        {
            case Nation.Air: track = Track.Air; break;
            case Nation.Water: track = Track.Water; break;
            case Nation.Earth: track = Track.Earth; break;
            case Nation.Fire: track = Track.Fire; break;
        }
    }

    private void Update()
    {
        if (currentStep < 0 || currentStep >= steps.Count) return;
        if (GameManager.Instance != null && GameManager.Instance.GameIsOver)
        {
            Destroy(gameObject.GetComponent<TutorialManager>());
            return;
        }

        Step step = steps[currentStep];
        if (step.isComplete == null)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f) Advance();
        }
        else if (step.isComplete())
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
        stepTimer = step.autoAdvanceAfter > 0f ? step.autoAdvanceAfter : 10f;
        SetHint($"({currentStep + 1}/{steps.Count})  {step.hint}");
    }

    // ------------------------------------------------------------------
    // Step builders
    // ------------------------------------------------------------------

    /// <summary>A step confirmed by a TutorialSignals counter rising above
    /// its value when the step began.</summary>
    private static Step Counter(string hint, Func<int> counter)
    {
        int baseline = 0;
        Step step = new Step { hint = hint };
        step.onEnter = () => baseline = counter();
        step.isComplete = () => counter() > baseline;
        return step;
    }

    /// <summary>An informational tip that advances itself.</summary>
    private static Step Tip(string hint, float seconds = 9f)
    {
        return new Step { hint = hint, isComplete = null, autoAdvanceAfter = seconds };
    }

    // ------------------------------------------------------------------
    // Curricula
    // ------------------------------------------------------------------

    private void BuildSteps()
    {
        switch (track)
        {
            case Track.Air: BuildAcademyCore("airbender"); BuildAirSteps(); return;
            case Track.Water: BuildAcademyCore("waterbender"); BuildWaterSteps(); return;
            case Track.Earth: BuildAcademyCore("earthbender"); BuildEarthSteps(); return;
            case Track.Fire: BuildAcademyCore("firebender"); BuildFireSteps(); return;
        }
        BuildGeneralSteps();
    }

    /// <summary>Every academy opens the same way: command, found, economy, train.</summary>
    private void BuildAcademyCore(string benderName)
    {
        steps.Add(new Step
        {
            hint = "Left-click or drag-select your starting units.",
            isComplete = AnyOwnedUnitSelected
        });

        steps.Add(new Step
        {
            hint = "Press B and place your COMMAND CENTER (green ghost = valid, R rotates).",
            isComplete = OwnedCommandCenterExists
        });

        steps.Add(new Step
        {
            hint = "Get your worker harvesting - right-click a resource node.",
            isComplete = AnyWorkerNearNode
        });

        steps.Add(new Step
        {
            hint = $"Train a {benderName} from your production buttons.",
            onEnter = () => unitCountAtStepStart = OwnedUnitCount(),
            isComplete = () => OwnedUnitCount() > unitCountAtStepStart
        });
    }

    private void BuildAirSteps()
    {
        steps.Add(Counter(
            "AIR SCOOTER: order an airbender somewhere FAR across the map - on long runs they conjure an air ball and ride it at +60% speed.",
            () => TutorialSignals.ScooterSprints));

        steps.Add(Counter(
            "STAFF GLIDER: order an airbender somewhere VERY far - they take flight, soaring over water, hills and buildings (around true mountains).",
            () => TutorialSignals.GliderFlights));

        steps.Add(Counter(
            "WIND SHIELD: select a bender and press Q - allies inside gain 40% damage reduction and cannot be knocked back.",
            () => TutorialSignals.ShieldsCast));

        steps.Add(Tip(
            "MOUNTAIN PERCHES: Air Nomad buildings can be placed ON steep mountainsides - out of reach of any army that cannot fly. Temples, sanctuaries, Sky Moorings... the high ground is yours."));

        steps.Add(Tip(
            "Sky Bison carry troops (right-click to board, U to unload) and Sky Moorings fly silver home from distant outposts. Now - clear out the intruders' camp!"));
    }

    private void BuildWaterSteps()
    {
        steps.Add(Counter(
            "Buy an UPGRADE - Healing Waters if it's available. One purchase empowers every waterbender you'll ever train.",
            () => TutorialSignals.UpgradesPurchased));

        steps.Add(Counter(
            "Let your benders fight beside wounded allies - upgraded waterbenders HEAL them (watch the health bars climb).",
            () => TutorialSignals.HealsDone));

        steps.Add(Counter(
            "WATER TECHNIQUES: land Ice Shards and Ice Prisons in battle - they chill and slow. Near a river, lake or fish shoal, Ice Prison freezes enemies SOLID.",
            () => TutorialSignals.WaterEffectsApplied));

        steps.Add(Counter(
            "ICE SHIELD: select a bender and press Q - a barrier of ice that absorbs damage outright before it shatters.",
            () => TutorialSignals.ShieldsCast));

        steps.Add(Tip(
            "Fight NEAR WATER whenever you can: watery climates empower your benders and enable true freezes. Fishing Docks + piers keep the silver flowing. Now break their warband!"));
    }

    private void BuildEarthSteps()
    {
        steps.Add(Counter(
            "FORTIFY: place a defensive structure - a Rock Launcher tower, or Stone Walls (walls need living earthbenders in your army to raise).",
            () => TutorialSignals.DefensesPlaced));

        steps.Add(Counter(
            "EARTH GRIP: fight! Your earthbenders' techniques clamp the ground around enemies' feet, holding them in place while the boulders land.",
            () => TutorialSignals.RootsApplied));

        steps.Add(Counter(
            "STONE SHIELD: select a bender and press Q - 60% damage reduction for allies (they move slower; mountains don't hurry).",
            () => TutorialSignals.ShieldsCast));

        steps.Add(Tip(
            "THE DEEP ARTS: master earthbenders can learn METALBENDING (tear machines and fortifications apart) and LAVABENDING (strikes ignite foes and melt walls). Both are ultimate upgrades - expensive, and worth it."));

        steps.Add(Tip(
            "Seismic Sensing extends your benders' sight through the fog. Build deep, hold the quarries - then bury their camp!"));
    }

    private void BuildFireSteps()
    {
        steps.Add(Counter(
            "IGNITE: attack! Firebender techniques like Flame Burst set enemies BURNING - damage that keeps ticking after the hit.",
            () => TutorialSignals.BurnsApplied));

        steps.Add(Counter(
            "LIGHTNING: keep fighting - your firebenders periodically channel Lightning Jolts for massive damage.",
            () => TutorialSignals.LightningThrown));

        steps.Add(Counter(
            "FLAME SHIELD: select a bender and press Q - allies take less damage and the barrier SCORCHES anything that presses in.",
            () => TutorialSignals.ShieldsCast));

        steps.Add(Tip(
            "BEWARE REDIRECTION: enemy masters with the Lightning Redirection upgrade can catch your bolts and hurl them back. You can learn it too."));

        steps.Add(Tip(
            "Fire wins by PRESSING: attack-move (F), burn their economy, and when your superweapon charges, press P and bring down the Comet Barrage. Now raze their camp!"));
    }

    private void BuildGeneralSteps()
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
