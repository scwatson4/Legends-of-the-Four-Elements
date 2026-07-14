using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The whole in-game HUD, built and wired IN CODE - drop this one component
/// in a scene and you get a working control panel with zero manual UnityEvent
/// wiring:
///   - top bar: Silver + Population counters
///   - right panel, three tabs: TRAIN (roster units), BUILD (buildings +
///     Found Base), UPGRADE (the nation's tech tree)
///   - a feedback line that echoes purchases and refusals
///
/// It reads the local player's NationData for button labels/counts, routes
/// training to the player's command-center spawner, building placement to
/// BuildingPlacer, and upgrades to UpgradePurchaser (so the tech-tree gate
/// and multiplayer relays all apply). Assign nothing; it self-builds and
/// self-populates. The scene builder (Legends > Build Playable Scene) adds
/// it automatically.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Tooltip("Silver the local player starts a generated skirmish with.")]
    public int startingSilver = 800;

    private TextMeshProUGUI silverLabel;
    private TextMeshProUGUI feedbackLabel;
    private UpgradePurchaser purchaser;

    private RectTransform panelContent;
    private readonly List<GameObject> tabPanels = new List<GameObject>();

    private void Start()
    {
        EnsureEventSystem();
        BuildCanvas();

        // Give the player a working wallet.
        if (PlayerResources.Instance != null)
        {
            PlayerResources.Instance.SetCredits(startingSilver);
        }
    }

    // ------------------------------------------------------------------
    // Canvas assembly
    // ------------------------------------------------------------------

    private void BuildCanvas()
    {
        GameObject canvasGo = new GameObject("GameHUDCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        BuildTopBar(canvasGo.transform);
        BuildSidePanel(canvasGo.transform);
        BuildFeedback(canvasGo.transform);
    }

    private void BuildTopBar(Transform parent)
    {
        GameObject bar = Panel(parent, "TopBar", new Vector2(0f, 0.95f), new Vector2(1f, 1f),
            new Color(0.05f, 0.06f, 0.08f, 0.85f));

        silverLabel = Label(bar.transform, "Silver", new Vector2(0.01f, 0f), new Vector2(0.3f, 1f), 26);
        silverLabel.alignment = TextAlignmentOptions.Left;
        silverLabel.color = new Color(1f, 0.9f, 0.55f);

        // PlayerResources drives the silver number; point it at our label.
        PlayerResources resources = PlayerResources.Instance;
        if (resources == null)
        {
            resources = gameObject.AddComponent<PlayerResources>();
        }
        resources.creditsText = silverLabel;

        // PopulationHUD drives the population counter.
        TextMeshProUGUI popLabel = Label(bar.transform, "Population", new Vector2(0.32f, 0f), new Vector2(0.6f, 1f), 24);
        popLabel.alignment = TextAlignmentOptions.Left;
        PopulationHUD popHud = GetComponent<PopulationHUD>();
        if (popHud == null) popHud = gameObject.AddComponent<PopulationHUD>();
        popHud.populationLabel = popLabel;

        TextMeshProUGUI hint = Label(bar.transform, "Hint",
            new Vector2(0.6f, 0f), new Vector2(0.99f, 1f), 18);
        hint.alignment = TextAlignmentOptions.Right;
        hint.color = new Color(0.7f, 0.8f, 0.9f);
        hint.text = "B: found base   E: select army   Q: shield   T/G: Avatar";
    }

    private void BuildSidePanel(Transform parent)
    {
        GameObject panel = Panel(parent, "SidePanel", new Vector2(0.82f, 0.04f), new Vector2(1f, 0.94f),
            new Color(0.05f, 0.06f, 0.08f, 0.82f));

        purchaser = GetComponent<UpgradePurchaser>();
        if (purchaser == null) purchaser = gameObject.AddComponent<UpgradePurchaser>();
        purchaser.feedbackLabel = null; // GameHUD owns the feedback line

        // Tab buttons across the top of the panel.
        string[] tabs = { "Train", "Build", "Upgrade" };
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            float x0 = 0.02f + i * 0.325f;
            MakeButton(panel.transform, tabs[i],
                new Vector2(x0, 0.94f), new Vector2(x0 + 0.30f, 0.99f), () => ShowTab(index));
        }

        // Scroll content region for whichever tab is active.
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(panel.transform, false);
        panelContent = content.GetComponent<RectTransform>();
        panelContent.anchorMin = new Vector2(0.02f, 0.01f);
        panelContent.anchorMax = new Vector2(0.98f, 0.93f);
        panelContent.offsetMin = Vector2.zero;
        panelContent.offsetMax = Vector2.zero;

        BuildTrainTab();
        BuildBuildTab();
        BuildUpgradeTab();
        ShowTab(0);
    }

    private void BuildFeedback(Transform parent)
    {
        GameObject strip = Panel(parent, "Feedback", new Vector2(0.3f, 0.005f), new Vector2(0.8f, 0.045f),
            new Color(0f, 0f, 0f, 0.5f));
        feedbackLabel = Label(strip.transform, "Feedback", new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), 20);
        feedbackLabel.color = new Color(1f, 0.95f, 0.8f);
        feedbackLabel.text = "";
    }

    // ------------------------------------------------------------------
    // Tabs
    // ------------------------------------------------------------------

    private void BuildTrainTab()
    {
        GameObject tab = TabRoot("TrainTab");
        NationData data = LocalNation();
        int row = 0;

        if (data != null && data.units != null)
        {
            for (int i = 0; i < data.units.Length; i++)
            {
                NationData.UnitEntry entry = data.units[i];
                if (entry == null) continue;
                int index = i;
                RosterButton(tab.transform, row++, $"{entry.unitName}", entry.cost, () => TrainUnit(index));
            }
        }
        if (row == 0) EmptyNote(tab.transform, "No roster — run Legends ► Bootstrap.");
    }

    private void BuildBuildTab()
    {
        GameObject tab = TabRoot("BuildTab");
        int row = 0;

        RosterButton(tab.transform, row++, "Found Base (Command Center)", -1, () =>
        {
            if (BuildingPlacer.Instance != null) BuildingPlacer.Instance.BeginCommandCenterPlacement();
        });

        NationData data = LocalNation();
        if (data != null && data.buildings != null)
        {
            for (int i = 0; i < data.buildings.Length; i++)
            {
                NationData.BuildingEntry entry = data.buildings[i];
                if (entry == null) continue;
                int index = i;
                RosterButton(tab.transform, row++, entry.buildingName, entry.cost, () =>
                {
                    if (BuildingPlacer.Instance != null) BuildingPlacer.Instance.BeginPlacement(index);
                });
            }
        }
    }

    private void BuildUpgradeTab()
    {
        GameObject tab = TabRoot("UpgradeTab");
        NationData data = LocalNation();
        int row = 0;

        if (data != null && data.upgrades != null)
        {
            for (int i = 0; i < data.upgrades.Length; i++)
            {
                UpgradeData upgrade = data.upgrades[i];
                if (upgrade == null) continue;
                int index = i;
                RosterButton(tab.transform, row++, $"{upgrade.displayName}", upgrade.baseCost,
                    () => BuyUpgrade(index, upgrade));
            }
        }
        if (row == 0) EmptyNote(tab.transform, "No upgrades — run Legends ► Bootstrap.");
    }

    private GameObject TabRoot(string name)
    {
        GameObject tab = new GameObject(name, typeof(RectTransform));
        tab.transform.SetParent(panelContent, false);
        RectTransform rect = tab.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        tabPanels.Add(tab);
        return tab;
    }

    private void ShowTab(int index)
    {
        for (int i = 0; i < tabPanels.Count; i++)
        {
            if (tabPanels[i] != null) tabPanels[i].SetActive(i == index);
        }
    }

    // ------------------------------------------------------------------
    // Actions
    // ------------------------------------------------------------------

    private void TrainUnit(int rosterIndex)
    {
        UnitSpawner trainer = FindLocalTrainer();
        if (trainer == null)
        {
            SetFeedback("Found your base first (Build ► Found Base).");
            return;
        }
        trainer.QueueRosterUnit(rosterIndex);
    }

    private void BuyUpgrade(int index, UpgradeData upgrade)
    {
        int factionId = FactionManager.LocalPlayerFactionId;
        int level = UpgradeManager.GetLevel(factionId, upgrade);

        if (purchaser != null) purchaser.Purchase(index);

        // Echo the outcome ourselves (purchaser's own label is unset).
        if (UpgradeManager.GetLevel(factionId, upgrade) > level)
        {
            SetFeedback($"{upgrade.displayName} → level {level + 1}");
        }
        else if (!UpgradeManager.CanPurchase(factionId, upgrade, out string reason))
        {
            SetFeedback(reason);
        }
        else
        {
            SetFeedback($"Not enough silver for {upgrade.displayName}.");
        }
    }

    /// <summary>The command center's spawner is the player's default trainer;
    /// falls back to any local production building with a spawner.</summary>
    private static UnitSpawner FindLocalTrainer()
    {
        int factionId = FactionManager.LocalPlayerFactionId;

        foreach (CommandCenter cc in FindObjectsByType<CommandCenter>(FindObjectsSortMode.None))
        {
            if (FactionUtility.GetFactionId(cc.gameObject) != factionId) continue;
            UnitSpawner spawner = cc.GetComponentInChildren<UnitSpawner>();
            if (spawner != null) return spawner;
        }
        foreach (UnitSpawner spawner in FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None))
        {
            if (FactionUtility.GetFactionId(spawner.gameObject) == factionId) return spawner;
        }
        return null;
    }

    private static NationData LocalNation()
    {
        NationDatabase db = NationDatabase.Load();
        if (db == null) return null;
        Faction local = FactionManager.Get(FactionManager.LocalPlayerFactionId);
        return db.Get(local != null ? local.nation : GameSetup.PlayerNation);
    }

    public void SetFeedback(string message)
    {
        if (feedbackLabel != null) feedbackLabel.text = message;
    }

    // ------------------------------------------------------------------
    // uGUI helpers
    // ------------------------------------------------------------------

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
    }

    private static GameObject Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        return go;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.Normal;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        return label;
    }

    private Button MakeButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Button_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI label = Label(go.transform, "Text", new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), 18);
        label.text = text;
        return button;
    }

    /// <summary>A full-width panel button in a vertical list (row 0 at top).</summary>
    private void RosterButton(Transform parent, int row, string name, int cost, UnityEngine.Events.UnityAction onClick)
    {
        const float rowHeight = 0.075f;
        const float gap = 0.008f;
        float top = 1f - row * (rowHeight + gap);
        string label = cost >= 0 ? $"{name}   ({cost})" : name;
        MakeButton(parent, label, new Vector2(0.02f, top - rowHeight), new Vector2(0.98f, top), onClick);
    }

    private void EmptyNote(Transform parent, string text)
    {
        TextMeshProUGUI note = Label(parent, "Empty", new Vector2(0.02f, 0.85f), new Vector2(0.98f, 1f), 16);
        note.color = new Color(0.9f, 0.6f, 0.5f);
        note.text = text;
    }
}
