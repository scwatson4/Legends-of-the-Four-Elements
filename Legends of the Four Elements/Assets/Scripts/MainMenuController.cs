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

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        aboutButton.onClick.AddListener(OnAboutClicked);
        aboutBackButton.onClick.AddListener(OnAboutBackClicked);
        aboutPanel.SetActive(false);
    }

    private void OnPlayClicked()
    {
        SceneManager.LoadScene("Level1");
        Debug.Log("Loading Level1");
    }

    private void OnAboutClicked()
    {
        aboutPanel.SetActive(true);
        playButton.gameObject.SetActive(false);
        aboutButton.gameObject.SetActive(false);
        titleText.gameObject.SetActive(false);
        Debug.Log("Showing About panel");
    }

    private void OnAboutBackClicked()
    {
        aboutPanel.SetActive(false);
        playButton.gameObject.SetActive(true);
        aboutButton.gameObject.SetActive(true);
        titleText.gameObject.SetActive(true);
        Debug.Log("Hiding About panel");
    }
}