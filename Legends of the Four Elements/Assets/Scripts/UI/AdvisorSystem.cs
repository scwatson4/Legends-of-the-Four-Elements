using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Elder Miza's advice: contextual coaching that appears as a quiet toast
/// when the game notices you struggling - silver running dry, population
/// capped, no workers, no Avatar. Each tip fires at most once per mission,
/// never more than one tip per 45 seconds, and never during dialogue.
/// Self-bootstraps and builds its own toast; zero wiring.
/// </summary>
public class AdvisorSystem : MonoBehaviour
{
    public float checkInterval = 10f;
    public float minSecondsBetweenTips = 45f;
    public float toastSeconds = 7f;

    private readonly HashSet<string> saidThisMission = new HashSet<string>();
    private float checkTimer = 20f; // grace period before the nagging starts
    private float lastTipTime = -99f;
    private float lowSilverSince = -1f;

    private GameObject toast;
    private TextMeshProUGUI toastLabel;
    private float toastRemaining;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<AdvisorSystem>() != null) return;
        GameObject go = new GameObject("AdvisorSystem");
        go.AddComponent<AdvisorSystem>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
        UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Fresh mission, fresh advice.
        saidThisMission.Clear();
        checkTimer = 20f;
        lowSilverSince = -1f;
        if (toast != null) toast.SetActive(false);
    }

    private void Update()
    {
        // Toast lifetime.
        if (toast != null && toast.activeSelf)
        {
            toastRemaining -= Time.unscaledDeltaTime;
            if (toastRemaining <= 0f) toast.SetActive(false);
        }

        if (GameManager.Instance == null || GameManager.Instance.GameIsOver) return;
        if (DialogueUI.IsShowing || PauseMenu.IsPaused) return;

        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f) return;
        checkTimer = checkInterval;

        if (Time.time - lastTipTime < minSecondsBetweenTips) return;

        int factionId = FactionManager.LocalPlayerFactionId;

        // --- Silver runs dry -------------------------------------------
        int silver = Economy.GetBalance(factionId);
        if (silver < 60)
        {
            if (lowSilverSince < 0f) lowSilverSince = Time.time;
            if (Time.time - lowSilverSince > 20f &&
                Say("silver", "Our silver runs dry, Commander. The villages pay tribute to whoever protects them - and the mines won't dig themselves."))
            {
                return;
            }
        }
        else lowSilverSince = -1f;

        // --- Population capped ------------------------------------------
        if (PopulationManager.GetPopulation(factionId) >= PopulationManager.GetCap(factionId) &&
            Say("population", "Our camps overflow. Raise housing, Commander - an army needs beds before it needs blades."))
        {
            return;
        }

        // --- No workers --------------------------------------------------
        if (CountOwned<ResourceCollector>() == 0 &&
            Say("workers", "No hands are gathering, Commander. Train workers - wars are won in the fields before the battlefield."))
        {
            return;
        }

        // --- No Avatar, but rich enough ----------------------------------
        if (!AvatarUnit.FactionHasAvatar(factionId) && silver > 700 &&
            Say("avatar", "The silver sits idle while the Avatar's seat stands empty. Summon them - no shadow was ever beaten by coin."))
        {
            return;
        }
    }

    private int CountOwned<T>() where T : Component
    {
        int count = 0;
        foreach (T component in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (FactionUtility.IsLocallyControlled(component.gameObject)) count++;
        }
        return count;
    }

    private bool Say(string tipId, string message)
    {
        if (saidThisMission.Contains(tipId)) return false;
        saidThisMission.Add(tipId);
        lastTipTime = Time.time;

        if (toast == null) BuildToast();
        toastLabel.text = $"Elder Miza:  \"{message}\"";
        toast.SetActive(true);
        toastRemaining = toastSeconds;

        Debug.Log($"[Advisor] {message}");
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySelectBark();
        return true;
    }

    private void BuildToast()
    {
        GameObject canvasGo = new GameObject("AdvisorCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        toast = new GameObject("Toast", typeof(RectTransform));
        toast.transform.SetParent(canvasGo.transform, false);
        Image bg = toast.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.06f, 0.85f);
        RectTransform rect = toast.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.62f, 0.84f);
        rect.anchorMax = new Vector2(0.99f, 0.95f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject labelGo = new GameObject("Text", typeof(RectTransform));
        labelGo.transform.SetParent(toast.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.04f, 0.08f);
        labelRect.anchorMax = new Vector2(0.96f, 0.92f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        toastLabel = labelGo.AddComponent<TextMeshProUGUI>();
        toastLabel.fontSize = 15;
        toastLabel.fontStyle = FontStyles.Italic;
        toastLabel.color = new Color(1f, 0.95f, 0.8f);
        toastLabel.textWrappingMode = TextWrappingModes.Normal;

        toast.SetActive(false);
    }
}
