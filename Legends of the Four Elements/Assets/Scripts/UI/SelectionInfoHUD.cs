using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom-left selection panel: one unit selected shows its name, health,
/// damage and special stats (Avatar energy, transport passengers, worker
/// state); a group shows a count breakdown by type ("4x Airbender Monk,
/// 2x Sky Bison"). Assign a TMP label on your HUD, or leave it empty and a
/// simple panel builds itself. Self-bootstraps into gameplay scenes.
/// </summary>
public class SelectionInfoHUD : MonoBehaviour
{
    public TextMeshProUGUI infoLabel;
    public float refreshInterval = 0.25f;

    private GameObject autoPanel;
    private float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<SelectionInfoHUD>() != null) return;
        GameObject go = new GameObject("SelectionInfoHUD");
        go.AddComponent<SelectionInfoHUD>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = refreshInterval;

        // Only meaningful in gameplay scenes.
        if (UnitSelectionManager.Instance == null)
        {
            SetText("");
            return;
        }

        if (infoLabel == null) BuildDefaultUI();
        SetText(DescribeSelection());
    }

    private string DescribeSelection()
    {
        List<GameObject> selected = UnitSelectionManager.Instance.selectedUnitsList;
        if (selected.Count == 0) return "";

        if (selected.Count == 1 && selected[0] != null)
        {
            return DescribeSingle(selected[0]);
        }

        // Group: count by unit type.
        Dictionary<string, int> counts = new Dictionary<string, int>();
        int total = 0;
        foreach (GameObject go in selected)
        {
            if (go == null) continue;
            Unit unit = go.GetComponent<Unit>();
            string key = unit != null ? unit.unitType.ToString() : go.name;
            counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
            total++;
        }

        List<string> parts = new List<string>();
        foreach (KeyValuePair<string, int> pair in counts)
        {
            parts.Add($"{pair.Value}x {pair.Key}");
        }
        return $"{total} units:  {string.Join("   ", parts)}";
    }

    private string DescribeSingle(GameObject go)
    {
        Unit unit = go.GetComponent<Unit>();
        if (unit == null) return go.name;

        string line = $"{go.name}   HP {Mathf.CeilToInt(unit.CurrentHealth)}/{Mathf.CeilToInt(unit.maxUnitHealth)}";

        AttackController attack = go.GetComponent<AttackController>();
        if (attack != null) line += $"   DMG {attack.unitDamage}";

        AvatarUnit avatar = go.GetComponent<AvatarUnit>();
        if (avatar != null)
        {
            line += $"\nAVATAR — {avatar.currentElement} bending   Energy {Mathf.RoundToInt(avatar.CurrentEnergy)}/{Mathf.RoundToInt(avatar.maxEnergy)}" +
                    (avatar.AvatarStateActive ? "   ⚡ AVATAR STATE" : "   (G / Shift+G / Ctrl+G)");
        }

        Transport transport = go.GetComponent<Transport>();
        if (transport != null)
        {
            line += $"\nAboard: {transport.PassengerCount}/{transport.capacity}   (U to unload)";
        }

        if (go.GetComponent<ResourceCollector>() != null)
        {
            line += "\nWorker — right-click a node to harvest, a damaged building to repair";
        }
        return line;
    }

    private void SetText(string text)
    {
        if (infoLabel != null) infoLabel.text = text;
        if (autoPanel != null) autoPanel.SetActive(!string.IsNullOrEmpty(text));
    }

    // ------------------------------------------------------------------

    private void BuildDefaultUI()
    {
        GameObject canvasGo = new GameObject("SelectionInfoCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        autoPanel = new GameObject("Panel", typeof(RectTransform));
        autoPanel.transform.SetParent(canvasGo.transform, false);
        Image bg = autoPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.09f, 0.8f);
        RectTransform rect = autoPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.01f, 0.01f);
        rect.anchorMax = new Vector2(0.34f, 0.13f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject labelGo = new GameObject("Info", typeof(RectTransform));
        labelGo.transform.SetParent(autoPanel.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.03f, 0.06f);
        labelRect.anchorMax = new Vector2(0.97f, 0.94f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        infoLabel = labelGo.AddComponent<TextMeshProUGUI>();
        infoLabel.fontSize = 15;
        infoLabel.alignment = TextAlignmentOptions.TopLeft;
        infoLabel.textWrappingMode = TextWrappingModes.Normal;

        autoPanel.SetActive(false);
    }
}
