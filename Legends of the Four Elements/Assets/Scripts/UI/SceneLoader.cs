using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Loads scenes behind a brief lore-quote splash - a black screen with a
/// line from the world while the level loads (shown at least minDisplay
/// seconds so it's readable). Call SceneLoader.Load("Level1") anywhere you'd
/// call SceneManager.LoadScene. Builds its own overlay; zero wiring.
/// </summary>
public static class SceneLoader
{
    private static readonly string[] Quotes =
    {
        "\"Balance begins with survival.\" — Elder Miza",
        "\"The shadow is riding her, not replacing her.\" — Kesu",
        "\"Every kingdom is grabbing what it can while the world reels.\" — Elder Miza",
        "\"The sea takes everything back. Especially us.\" — Kalani of the Weeping Ice",
        "\"I held this kingdom on my shoulders for sixty years.\" — Boruk, the Mountain That Walks",
        "\"The fire is the ONLY answer left.\" — Ashan, the Dawnbringer",
        "\"Before your elements had names, I WAS.\" — Umbriss, the First Shadow",
        "\"Air to lift, water to bind, earth to hold, fire to burn.\" — Zephyra of the Hollow Sky",
        "\"Terrain is a weapon, Commander - use theirs against them.\" — Kesu",
        "\"The dull work of digging wins more battles than any duel.\" — Elder Miza",
        "\"Protect the villagers and they will remember it.\" — Elder Miza",
        "\"Even mountains erode.\" — Elder Miza",
        "Dark spirits fear only one thing: the four elements in a single pair of hands.",
        "Villages pay tribute to whoever protects them. Or whoever remains.",
        "An Avatar's energy grows in battle. So does everything hunting them.",
    };

    private static float minDisplaySeconds = 2.2f;

    public static void Load(string sceneName)
    {
        // Someone must run the coroutine; GameFeel is always around.
        if (GameFeel.Instance == null)
        {
            SceneManager.LoadScene(sceneName); // extremely early call: just load
            return;
        }
        GameFeel.Instance.StartCoroutine(LoadRoutine(sceneName));
    }

    private static IEnumerator LoadRoutine(string sceneName)
    {
        GameObject overlay = BuildOverlay(Quotes[Random.Range(0, Quotes.Length)]);
        Time.timeScale = 1f;

        float start = Time.unscaledTime;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null) // bad scene name: show the error path gracefully
        {
            Object.Destroy(overlay);
            yield break;
        }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        while (Time.unscaledTime - start < minDisplaySeconds) yield return null;
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // Brief fade-out after the new scene is up.
        CanvasGroup group = overlay.GetComponentInChildren<CanvasGroup>();
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            if (group != null) group.alpha = 1f - t / 0.4f;
            yield return null;
        }
        Object.Destroy(overlay);
    }

    private static GameObject BuildOverlay(string quote)
    {
        GameObject root = new GameObject("LoadingQuote");
        Object.DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = true;

        GameObject bg = new GameObject("BG", typeof(RectTransform));
        bg.transform.SetParent(root.transform, false);
        Image image = bg.AddComponent<Image>();
        image.color = Color.black;
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject labelGo = new GameObject("Quote", typeof(RectTransform));
        labelGo.transform.SetParent(root.transform, false);
        RectTransform rect = labelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.35f);
        rect.anchorMax = new Vector2(0.85f, 0.65f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = quote;
        label.fontSize = 26;
        label.fontStyle = FontStyles.Italic;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.95f, 0.8f);
        label.textWrappingMode = TextWrappingModes.Normal;

        return root;
    }
}
