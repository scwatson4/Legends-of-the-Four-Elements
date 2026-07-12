using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Esc pauses the game with a simple menu: Resume, Restart Mission, Main
/// Menu, master volume slider. Self-bootstraps into gameplay scenes (only
/// activates where a GameManager exists) and builds its own UI, so nothing
/// needs wiring. Esc politely yields to everything else that uses it:
/// dialogue, building placement, attack-move, embodiment, superweapon
/// targeting.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.isOpen;

    private GameObject panel;
    private bool isOpen;
    private float previousTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("PauseMenu");
        Instance = go.AddComponent<PauseMenu>();
        DontDestroyOnLoad(go);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;            // menus pause nothing
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (GameManager.Instance.GameIsOver) return;

        if (isOpen)
        {
            Close();
            return;
        }

        // Everything else that uses Esc gets first claim on it.
        if (DialogueUI.IsShowing) return;
        if (BuildingPlacer.IsPlacing) return;
        if (Superweapon.IsTargeting) return;
        if (EmbodimentController.IsActive) return;

        Open();
    }

    private void Open()
    {
        if (panel == null) BuildUI();
        isOpen = true;
        panel.SetActive(true);
        previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;
    }

    public void Close()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = previousTimeScale;
    }

    private void RestartMission()
    {
        Close();
        if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
    }

    private void GoToMainMenu()
    {
        Close();
        if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
    }

    // ------------------------------------------------------------------
    // Auto-built UI
    // ------------------------------------------------------------------

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("PauseCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        panel = CreateChild("Panel", canvasGo.transform);
        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.7f);
        Stretch(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject box = CreateChild("Box", panel.transform);
        Image boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        Stretch(box.GetComponent<RectTransform>(), new Vector2(0.36f, 0.25f), new Vector2(0.64f, 0.75f));

        CreateLabel(box.transform, "PAUSED", new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.97f), 30, FontStyles.Bold);

        CreateButton(box.transform, "Resume", new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.79f), Close);
        CreateButton(box.transform, "Restart Mission", new Vector2(0.1f, 0.50f), new Vector2(0.9f, 0.63f), RestartMission);
        CreateButton(box.transform, "Main Menu", new Vector2(0.1f, 0.34f), new Vector2(0.9f, 0.47f), GoToMainMenu);

        CreateLabel(box.transform, "Volume", new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.31f), 16, FontStyles.Normal);
        CreateVolumeSlider(box.transform, new Vector2(0.1f, 0.10f), new Vector2(0.9f, 0.20f));

        panel.SetActive(false);
    }

    private void CreateButton(Transform parent, string text, Vector2 min, Vector2 max, System.Action onClick)
    {
        GameObject go = CreateChild(text, parent);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.22f, 0.28f, 1f);
        Stretch(go.GetComponent<RectTransform>(), min, max);

        Button button = go.AddComponent<Button>();
        button.onClick.AddListener(() => onClick());

        CreateLabel(go.transform, text, Vector2.zero, Vector2.one, 20, FontStyles.Normal);
    }

    private void CreateVolumeSlider(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject go = CreateChild("Volume", parent);
        Image track = go.AddComponent<Image>();
        track.color = new Color(0.18f, 0.22f, 0.28f, 1f);
        Stretch(go.GetComponent<RectTransform>(), min, max);

        GameObject fillGo = CreateChild("Fill", go.transform);
        Image fill = fillGo.AddComponent<Image>();
        fill.color = new Color(1f, 0.85f, 0.4f, 1f);
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        Stretch(fillRect, Vector2.zero, new Vector2(AudioListener.volume, 1f));

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioListener.volume;
        slider.onValueChanged.AddListener(v => AudioListener.volume = v);
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateLabel(Transform parent, string text, Vector2 min, Vector2 max,
        float size, FontStyles style)
    {
        GameObject go = CreateChild("Label", parent);
        Stretch(go.GetComponent<RectTransform>(), min, max);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
    }
}
