using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Story dialogue overlay: shows a queue of speaker lines with a typewriter
/// effect at the start of campaign levels. The game pauses (timeScale 0)
/// until the last line is dismissed. Click / Space / Enter advances; Escape
/// skips everything.
///
/// Zero wiring required: if the scene has no DialogueUI, a clean default
/// panel is built in code. To restyle it, add your own DialogueUI to the HUD
/// canvas and assign the panel/speaker/body fields.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("Optional custom UI (auto-built when empty)")]
    public GameObject panelRoot;
    public TextMeshProUGUI speakerLabel;
    public TextMeshProUGUI bodyLabel;
    public TextMeshProUGUI continueHint;

    [Header("Behaviour")]
    public float charactersPerSecond = 45f;
    public bool pauseGameWhileTalking = true;

    private readonly Queue<DialogueLine> queue = new Queue<DialogueLine>();
    private Action onComplete;
    private Coroutine typing;
    private bool lineFullyShown;
    private float previousTimeScale = 1f;

    /// <summary>Plays a dialogue sequence, creating the UI if needed.</summary>
    public static void Play(List<DialogueLine> lines, Action onComplete = null)
    {
        if (lines == null || lines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        if (Instance == null)
        {
            GameObject go = new GameObject("DialogueUI");
            Instance = go.AddComponent<DialogueUI>();
        }
        Instance.Begin(lines, onComplete);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot == null) BuildDefaultUI();
        panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Begin(List<DialogueLine> lines, Action onComplete)
    {
        queue.Clear();
        foreach (DialogueLine line in lines) queue.Enqueue(line);
        this.onComplete = onComplete;

        if (pauseGameWhileTalking)
        {
            previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
        }

        panelRoot.SetActive(true);
        NextLine();
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Finish();
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (!lineFullyShown)
            {
                // First press: reveal the whole line instantly.
                if (typing != null) StopCoroutine(typing);
                bodyLabel.maxVisibleCharacters = int.MaxValue;
                lineFullyShown = true;
            }
            else
            {
                NextLine();
            }
        }
    }

    private void NextLine()
    {
        if (queue.Count == 0)
        {
            Finish();
            return;
        }

        DialogueLine line = queue.Dequeue();
        speakerLabel.text = line.speaker;
        bodyLabel.text = line.text;
        lineFullyShown = false;

        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(Typewriter(line.text.Length));
    }

    private IEnumerator Typewriter(int totalCharacters)
    {
        bodyLabel.maxVisibleCharacters = 0;
        float shown = 0f;
        while (shown < totalCharacters)
        {
            shown += charactersPerSecond * Time.unscaledDeltaTime; // works while paused
            bodyLabel.maxVisibleCharacters = Mathf.FloorToInt(shown);
            yield return null;
        }
        bodyLabel.maxVisibleCharacters = int.MaxValue;
        lineFullyShown = true;
    }

    private void Finish()
    {
        panelRoot.SetActive(false);
        if (pauseGameWhileTalking) Time.timeScale = previousTimeScale;

        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    // ------------------------------------------------------------------
    // Default UI built in code (a dark letterbox bar along the bottom)
    // ------------------------------------------------------------------

    private void BuildDefaultUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        panelRoot = CreateChild("Panel", transform);
        Image background = panelRoot.AddComponent<Image>();
        background.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.02f);
        panelRect.anchorMax = new Vector2(0.92f, 0.24f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        speakerLabel = CreateLabel("Speaker", panelRoot.transform,
            new Vector2(0.03f, 0.68f), new Vector2(0.97f, 0.98f), 26, FontStyles.Bold);
        speakerLabel.color = new Color(1f, 0.85f, 0.4f);

        bodyLabel = CreateLabel("Body", panelRoot.transform,
            new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.66f), 22, FontStyles.Normal);

        continueHint = CreateLabel("Hint", panelRoot.transform,
            new Vector2(0.55f, 0.0f), new Vector2(0.97f, 0.14f), 14, FontStyles.Italic);
        continueHint.text = "click / space to continue — esc to skip";
        continueHint.alignment = TextAlignmentOptions.BottomRight;
        continueHint.color = new Color(1f, 1f, 1f, 0.5f);
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, float size, FontStyles style)
    {
        GameObject go = CreateChild(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.fontSize = size;
        label.fontStyle = style;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }
}
