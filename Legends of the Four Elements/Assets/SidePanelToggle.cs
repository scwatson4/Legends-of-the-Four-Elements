using UnityEngine;
using UnityEngine.UI;

public class SidePanelToggle : MonoBehaviour
{
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public Vector2 hiddenPosition = new Vector2(500f, 0f); // How far off-screen the panel slides
    private Vector2 visiblePosition;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isVisible = false;
    private bool isAnimating = false;

    [Header("Optional UI Button")]
    public Button toggleButton;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        visiblePosition = rectTransform.anchoredPosition;

        // Start hidden
        rectTransform.anchoredPosition = visiblePosition + hiddenPosition;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isVisible = false;

        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (isAnimating) return;
        StopAllCoroutines();
        StartCoroutine(AnimatePanel(!isVisible));
    }

    private System.Collections.IEnumerator AnimatePanel(bool show)
    {
        isAnimating = true;

        float time = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = show ? visiblePosition : visiblePosition + hiddenPosition;

        float startAlpha = canvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (time < animationDuration)
        {
            float t = time / animationDuration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            time += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = endPos;
        canvasGroup.alpha = endAlpha;
        isVisible = show;

        if (show)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        isAnimating = false;
    }
}
