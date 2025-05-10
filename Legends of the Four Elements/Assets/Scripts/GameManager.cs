using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool GameIsOver { get; private set; } // Added for wave system

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button winMainMenuButton;
    [SerializeField] private GameObject sidePanel;

    private string currentLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Another GameManager exists in {Instance.gameObject.scene.name}. Destroying this duplicate in {gameObject.scene.name}.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log($"GameManager initialized in {gameObject.scene.name}. Parent: {(transform.parent != null ? transform.parent.name : "None")}");

        currentLevel = SceneManager.GetActiveScene().name;
        GameIsOver = false; // Initialize game state
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (sidePanel != null) sidePanel.SetActive(true);
        LogUIAssignments("Awake");
    }

    public void OnPlayerCommandCenterDestroyed()
    {
        if (gameOverPanel == null || sidePanel == null)
        {
            Debug.LogError($"Cannot show Game Over screen. Missing assignments: GameOverPanel: {(gameOverPanel == null ? "Null" : "Assigned")}, SidePanel: {(sidePanel == null ? "Null" : "Assigned")}. Check GameManager Inspector in Level1.");
            return;
        }

        GameIsOver = true; // Signal game over for wave system
        gameOverPanel.SetActive(true);
        sidePanel.SetActive(false);
        Time.timeScale = 0f;
        Debug.Log($"Game Over: Player lost in {currentLevel}");
    }

    public void OnEnemyCommandCenterDestroyed()
    {
        if (winPanel == null || sidePanel == null)
        {
            Debug.LogError($"Cannot show Win screen. Missing assignments: WinPanel: {(winPanel == null ? "Null" : "Assigned")}, SidePanel: {(sidePanel == null ? "Null" : "Assigned")}. Check GameManager Inspector in Level1.");
            return;
        }

        GameIsOver = true; // Signal game over for wave system
        winPanel.SetActive(true);
        sidePanel.SetActive(false);
        Time.timeScale = 0f;
        Debug.Log("Level won");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Debug.Log($"RestartLevel: Loading scene {currentLevel}");
        SceneManager.LoadScene(currentLevel);
        Debug.Log($"RestartLevel: Scene {currentLevel} load initiated.");
    }

    public void GoToMainMenu()
    {
        Debug.Log("GoToMainMenu called. Checking MainMenu scene validity.");
        if (SceneUtility.GetBuildIndexByScenePath("Scenes/MainMenuScene") == -1)
        {
            Debug.LogError("MainMenu scene not found in Build Settings! Add Scenes/MainMenuScene.unity to File > Build Settings > Scenes in Build.");
            return;
        }

        Time.timeScale = 1f;
        Debug.Log("GoToMainMenu: Loading MainMenu scene.");
        SceneManager.LoadScene("MainMenuScene");
        Debug.Log("GoToMainMenu: MainMenu scene load initiated.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentLevel = scene.name;
        Debug.Log($"OnSceneLoaded: {currentLevel}");

        if (currentLevel != "Level1")
        {
            Debug.Log($"Skipping UI initialization in {currentLevel} (not Level1)");
            return;
        }

        GameIsOver = false; // Reset for new level
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (sidePanel != null) sidePanel.SetActive(true);
        Time.timeScale = 1f;

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                Debug.Log("RestartButton clicked.");
                RestartLevel();
            });
            Debug.Log("RestartButton listener assigned.");
        }
        else
        {
            Debug.LogError("RestartButton is null. Assign in GameManager Inspector.");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() =>
            {
                Debug.Log("GameOver MainMenuButton clicked.");
                GoToMainMenu();
            });
            Debug.Log("GameOver MainMenuButton listener assigned.");
        }
        else
        {
            Debug.LogError("MainMenuButton is null. Assign in GameManager Inspector.");
        }

        if (winMainMenuButton != null)
        {
            winMainMenuButton.onClick.RemoveAllListeners();
            winMainMenuButton.onClick.AddListener(() =>
            {
                Debug.Log("Win MainMenuButton clicked.");
                GoToMainMenu();
            });
            Debug.Log("WinMainMenuButton listener assigned.");
        }
        else
        {
            Debug.LogError("WinMainMenuButton is null. Assign in GameManager Inspector.");
        }

        LogUIAssignments("OnSceneLoaded");
    }

    private void LogUIAssignments(string context)
    {
        Debug.Log($"UI Assignments ({context} in {currentLevel}): " +
                  $"GameOverPanel: {(gameOverPanel != null ? "Assigned" : "Null")}, " +
                  $"RestartButton: {(restartButton != null ? "Assigned" : "Null")}, " +
                  $"MainMenuButton: {(mainMenuButton == null ? "Null" : "Assigned")}, " +
                  $"WinPanel: {(winPanel != null ? "Assigned" : "Null")}, " +
                  $"WinMainMenuButton: {(winMainMenuButton == null ? "Null" : "Assigned")}, " +
                  $"SidePanel: {(sidePanel != null ? "Assigned" : "Null")}");
    }
}