using System.Collections;
using UnityEngine;

/// <summary>
/// Juice: camera shake (superweapons, boss phase shifts) and slow motion
/// (a chapter boss falling). Self-bootstraps; call the statics from anywhere:
///     GameFeel.Shake(0.5f, 0.8f);
///     GameFeel.SlowMoThen(0.3f, 1.5f, () => ShowVictory());
/// Shake offsets the main camera around whatever position its rig gives it,
/// so it composes with the RTS camera controller.
/// </summary>
public class GameFeel : MonoBehaviour
{
    public static GameFeel Instance { get; private set; }

    private float shakeIntensity;
    private float shakeRemaining;
    private Transform shakenCamera;
    private Vector3 appliedOffset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("GameFeel");
        Instance = go.AddComponent<GameFeel>();
        DontDestroyOnLoad(go);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Shake(float intensity, float duration)
    {
        if (Instance == null) return;
        Instance.shakeIntensity = Mathf.Max(Instance.shakeIntensity, intensity);
        Instance.shakeRemaining = Mathf.Max(Instance.shakeRemaining, duration);
    }

    /// <summary>Dramatic slow motion, then run the callback at normal speed.</summary>
    public static void SlowMoThen(float timeScale, float realSeconds, System.Action then)
    {
        if (Instance == null)
        {
            then?.Invoke();
            return;
        }
        Instance.StartCoroutine(Instance.SlowMoRoutine(timeScale, realSeconds, then));
    }

    private IEnumerator SlowMoRoutine(float timeScale, float realSeconds, System.Action then)
    {
        // Don't fight an existing pause (dialogue/pause menu).
        if (Time.timeScale > 0.01f)
        {
            Time.timeScale = timeScale;
            yield return new WaitForSecondsRealtime(realSeconds);
            if (Time.timeScale > 0.01f) Time.timeScale = 1f; // unless something paused meanwhile
        }
        then?.Invoke();
    }

    private void LateUpdate()
    {
        // Undo last frame's offset, then apply a fresh one - composes with
        // whatever the camera rig did this frame.
        if (shakenCamera != null)
        {
            shakenCamera.localPosition -= appliedOffset;
            appliedOffset = Vector3.zero;
        }

        if (shakeRemaining <= 0f) return;
        shakeRemaining -= Time.unscaledDeltaTime;

        Camera cam = Camera.main;
        if (cam == null) return;
        shakenCamera = cam.transform;

        float falloff = Mathf.Clamp01(shakeRemaining); // eases out over the last second
        appliedOffset = Random.insideUnitSphere * shakeIntensity * falloff;
        shakenCamera.localPosition += appliedOffset;

        if (shakeRemaining <= 0f) shakeIntensity = 0f;
    }
}
