using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button aboutButton;
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private Button aboutBackButton;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Nation Select (optional)")]
    [Tooltip("If assigned, Play opens the nation-select panel instead of loading the level directly.")]
    [SerializeField] private GameObject nationSelectPanel;

    [Tooltip("Scene loaded when Play is clicked and no nation-select panel is wired up.")]
    [SerializeField] private string levelSceneName = "Level1";

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        aboutButton.onClick.AddListener(OnAboutClicked);
        aboutBackButton.onClick.AddListener(OnAboutBackClicked);
        aboutPanel.SetActive(false);
        if (nationSelectPanel != null) nationSelectPanel.SetActive(false);
    }

    private void OnPlayClicked()
    {
        if (nationSelectPanel != null)
        {
            nationSelectPanel.SetActive(true);
            SetMainButtonsVisible(false);
            return;
        }

        // Legacy flow: jump straight into the level as the Air Nomads.
        GameSetup.ConfigureSurvival(Nation.Air);
        SceneLoader.Load(levelSceneName);
    }

    /// <summary>Wire to the nation-select panel's Back button.</summary>
    public void CloseNationSelect()
    {
        if (nationSelectPanel != null) nationSelectPanel.SetActive(false);
        SetMainButtonsVisible(true);
    }

    private void SetMainButtonsVisible(bool visible)
    {
        playButton.gameObject.SetActive(visible);
        aboutButton.gameObject.SetActive(visible);
        titleText.gameObject.SetActive(visible);
    }

    private void OnAboutClicked()
    {
        aboutPanel.SetActive(true);
        SetMainButtonsVisible(false);
    }

    private void OnAboutBackClicked()
    {
        aboutPanel.SetActive(false);
        SetMainButtonsVisible(true);
    }
}
