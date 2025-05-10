using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerResources : MonoBehaviour
{
    public static PlayerResources Instance { get; private set; }

    [Header("Credit System")]
    public int Credits { get; private set; } = 300;
    private int displayedCredits = 300;

    [Header("UI")]
    public TextMeshProUGUI creditsText;

    [Header("Animation")]
    public float animationDuration = 0.5f;
    public Color normalColor = Color.white;
    public Color lowCreditsColor = Color.red;
    public int lowCreditsThreshold = 20;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gainClip;
    public AudioClip spendClip;

    [Header("Warning Popup")]
    public TextMeshProUGUI warningPopup;
    public float popupDuration = 1.5f;
    public float shakeIntensity = 5f;
    public float shakeDuration = 0.4f;

    private Coroutine animateRoutine;
    private Coroutine warningRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool SpendCredits(int amount)
    {
        if (Credits >= amount)
        {
            Credits -= amount;
            PlaySound(spendClip);
            UpdateCreditsDisplay();
            return true;
        }

        ShowNotEnoughCreditsPopup();
        return false;
    }

    public void AddCredits(int amount)
    {
        Credits += amount;
        PlaySound(gainClip);
        UpdateCreditsDisplay();
    }

    private void UpdateCreditsDisplay()
    {
        if (animateRoutine != null) StopCoroutine(animateRoutine);
        animateRoutine = StartCoroutine(AnimateCredits(displayedCredits, Credits));
    }

    private IEnumerator AnimateCredits(int startValue, int endValue)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;
            int value = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
            creditsText.text = value.ToString();
            elapsed += Time.deltaTime;
            yield return null;
        }

        creditsText.text = endValue.ToString();
        displayedCredits = endValue;

        if (Credits <= lowCreditsThreshold)
            StartCoroutine(FlashRed());
        else
            creditsText.color = normalColor;
    }

    private IEnumerator FlashRed()
    {
        float t = 0f;
        float flashTime = 0.5f;

        while (t < flashTime)
        {
            creditsText.color = Color.Lerp(lowCreditsColor, normalColor, Mathf.PingPong(t * 4f, 1f));
            t += Time.deltaTime;
            yield return null;
        }

        creditsText.color = lowCreditsColor;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void ShowNotEnoughCreditsPopup()
    {
        if (warningPopup == null) return;

        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(PopupRoutine());
    }

    private IEnumerator PopupRoutine()
    {
        float fadeInTime = 0.2f;
        float holdTime = popupDuration;
        float fadeOutTime = 0.3f;

        Vector3 originalPos = warningPopup.rectTransform.localPosition;
        Color originalColor = warningPopup.color;

        // Fade in
        for (float t = 0f; t < fadeInTime; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(0f, 1f, t / fadeInTime);
            warningPopup.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        warningPopup.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

        // Shake
        float shakeTime = 0f;
        while (shakeTime < shakeDuration)
        {
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity);
            warningPopup.rectTransform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            shakeTime += Time.deltaTime;
            yield return null;
        }
        warningPopup.rectTransform.localPosition = originalPos;

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade out
        for (float t = 0f; t < fadeOutTime; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / fadeOutTime);
            warningPopup.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        warningPopup.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }
}
