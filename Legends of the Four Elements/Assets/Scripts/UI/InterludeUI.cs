using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Chapter interlude: a full-screen story panel (big title, prose, optional
/// illustration) shown when a new chapter begins - the breath between
/// battles. Click / Space dismisses. Builds its own overlay; assign
/// InterludeUI.NextImage before calling Show to add chapter art later.
/// </summary>
public static class InterludeUI
{
    /// <summary>Optional illustration for the next interlude (set then forget).</summary>
    public static Sprite NextImage;

    public static void Show(string title, string body, Action onDone)
    {
        if (GameFeel.Instance == null)
        {
            onDone?.Invoke();
            return;
        }

        GameObject overlay = Build(title, body);
        GameFeel.Instance.StartCoroutine(WaitForDismiss(overlay, onDone));
    }

    private static System.Collections.IEnumerator WaitForDismiss(GameObject overlay, Action onDone)
    {
        float previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;

        yield return null; // swallow the click that opened us
        while (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space) &&
               !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.Escape))
        {
            yield return null;
        }

        Time.timeScale = previousTimeScale;
        UnityEngine.Object.Destroy(overlay);
        onDone?.Invoke();
    }

    private static GameObject Build(string title, string body)
    {
        GameObject root = new GameObject("Interlude");
        UnityEngine.Object.DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Black backdrop.
        Image bg = CreateStretched<Image>("BG", root.transform, Vector2.zero, Vector2.one);
        bg.color = new Color(0.02f, 0.02f, 0.04f, 0.98f);

        // Optional chapter illustration.
        if (NextImage != null)
        {
            Image art = CreateStretched<Image>("Art", root.transform,
                new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.88f));
            art.sprite = NextImage;
            art.preserveAspect = true;
            NextImage = null;
        }

        TextMeshProUGUI titleLabel = CreateStretched<TextMeshProUGUI>("Title", root.transform,
            new Vector2(0.1f, NextImage != null ? 0.3f : 0.62f), new Vector2(0.9f, NextImage != null ? 0.4f : 0.78f));
        titleLabel.text = title;
        titleLabel.fontSize = 40;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.color = new Color(1f, 0.85f, 0.4f);

        TextMeshProUGUI bodyLabel = CreateStretched<TextMeshProUGUI>("Body", root.transform,
            new Vector2(0.18f, 0.28f), new Vector2(0.82f, 0.58f));
        bodyLabel.text = body;
        bodyLabel.fontSize = 22;
        bodyLabel.fontStyle = FontStyles.Italic;
        bodyLabel.alignment = TextAlignmentOptions.Center;
        bodyLabel.textWrappingMode = TextWrappingModes.Normal;

        TextMeshProUGUI hint = CreateStretched<TextMeshProUGUI>("Hint", root.transform,
            new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.14f));
        hint.text = "click to continue";
        hint.fontSize = 14;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(1f, 1f, 1f, 0.45f);

        return root;
    }

    private static T CreateStretched<T>(string name, Transform parent, Vector2 min, Vector2 max)
        where T : Component
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go.AddComponent<T>();
    }
}
