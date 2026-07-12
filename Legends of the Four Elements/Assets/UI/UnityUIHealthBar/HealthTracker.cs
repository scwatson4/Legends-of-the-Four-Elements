using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthTracker : MonoBehaviour
{
    public Slider HealthBarSlider;
    public Image sliderFill;

    public Material greenEmission;
    public Material yellowEmission;
    public Material redEmission;

    [Header("Smart Visibility")]
    [Tooltip("Show only when damaged, selected, or recently hit (declutters the battlefield).")]
    public bool hideWhenIrrelevant = true;
    public float showAfterHitSeconds = 3f;

    private Coroutine smoothHealthChangeCoroutine;
    private CanvasGroup canvasGroup;
    private GameObject owner;
    private float lastHitTime = -99f;
    private float currentFraction = 1f;

    private void Awake()
    {
        // CanvasGroup (not Canvas.enabled) so fog-of-war hiding and smart
        // visibility can both act without fighting each other.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Unit unit = GetComponentInParent<Unit>();
        Structure structure = GetComponentInParent<Structure>();
        CommandCenter cc = GetComponentInParent<CommandCenter>();
        owner = unit != null ? unit.gameObject
            : structure != null ? structure.gameObject
            : cc != null ? cc.gameObject : null;
    }

    private void Update()
    {
        if (!hideWhenIrrelevant || canvasGroup == null) return;

        bool dead = currentFraction <= 0.001f;
        bool damaged = currentFraction < 0.999f;
        bool recentlyHit = Time.time - lastHitTime < showAfterHitSeconds;
        bool selected = owner != null && UnitSelectionManager.Instance != null &&
                        UnitSelectionManager.Instance.selectedUnitsList.Contains(owner);

        canvasGroup.alpha = (!dead && (damaged || recentlyHit || selected)) ? 1f : 0f;
    }


    // Call this method to update the health bar and color
    public void UpdateSliderValue(float currentHealth, float maxHealth)
    {
        // Calculate the health percentage
        float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);

        if (healthPercentage < currentFraction) lastHitTime = Time.time; // took a hit
        currentFraction = healthPercentage;

        // Update the slider value and size
       // HealthBarSlider.value = healthPercentage;



        // If there is an ongoing smooth health change coroutine, stop it
        if (smoothHealthChangeCoroutine != null)
        {
            StopCoroutine(smoothHealthChangeCoroutine);
        }

        // Start a new coroutine for smooth health change
        smoothHealthChangeCoroutine = StartCoroutine(SmoothHealthChange(HealthBarSlider.value, healthPercentage, 0.5f));



        // Update the color based on health percentage
        UpdateColor(healthPercentage);
    }

    // Coroutine for smooth health change
    private IEnumerator SmoothHealthChange(float startValue, float targetValue, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            HealthBarSlider.value = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        HealthBarSlider.value = targetValue;

        // Clear the coroutine reference after it's finished
        smoothHealthChangeCoroutine = null;
    }


    // Set the color based on the health percentage
    private void UpdateColor(float healthPercentage)
    {
        if (healthPercentage >= 0.6f)
        {
            sliderFill.material = greenEmission;
        }
        else if (healthPercentage >= 0.3f)
        {
            sliderFill.material = yellowEmission;
        }
        else
        {
            sliderFill.material = redEmission;
        }
    }

}
