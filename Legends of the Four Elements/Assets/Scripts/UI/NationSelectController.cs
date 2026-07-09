using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Drives the nation-select panel in the main menu. Wire four nation buttons
/// to SelectNation(0..3), mode buttons/dropdown to SetMode, and the Start
/// button to StartGame. The panel works with plain UI Buttons - see
/// docs/FINISHING_GUIDE.md for the layout recipe.
/// </summary>
public class NationSelectController : MonoBehaviour
{
    [Header("Scene")]
    public string levelSceneName = "Level1";

    [Header("Feedback (optional)")]
    public TextMeshProUGUI selectedNationLabel;
    public TextMeshProUGUI selectedNationDescription;

    [Header("Skirmish")]
    [Range(1, 3)] public int aiOpponentCount = 1;

    private Nation selectedNation = Nation.Air;
    private GameMode selectedMode = GameMode.Survival;

    private void OnEnable()
    {
        RefreshLabels();
    }

    /// <summary>Wire nation buttons: 0=Air, 1=Water, 2=Earth, 3=Fire.</summary>
    public void SelectNation(int nationIndex)
    {
        selectedNation = (Nation)Mathf.Clamp(nationIndex, 0, 3);
        RefreshLabels();
    }

    /// <summary>Wire mode buttons/dropdown: 0=Survival, 1=Skirmish.</summary>
    public void SetMode(int modeIndex)
    {
        selectedMode = (GameMode)Mathf.Clamp(modeIndex, 0, 1);
    }

    public void SetAIOpponentCount(float count)
    {
        aiOpponentCount = Mathf.Clamp(Mathf.RoundToInt(count), 1, 3);
    }

    public void StartGame()
    {
        if (selectedMode == GameMode.Survival)
        {
            GameSetup.ConfigureSurvival(selectedNation);
        }
        else
        {
            GameSetup.ConfigureSkirmish(selectedNation, aiOpponentCount);
        }

        Debug.Log($"Starting {selectedMode} as {NationInfo.DisplayName(selectedNation)}");
        SceneManager.LoadScene(levelSceneName);
    }

    private void RefreshLabels()
    {
        if (selectedNationLabel != null)
        {
            selectedNationLabel.text = NationInfo.DisplayName(selectedNation);
            selectedNationLabel.color = NationInfo.ThemeColor(selectedNation);
        }

        if (selectedNationDescription != null)
        {
            NationDatabase db = NationDatabase.Load();
            NationData data = db != null ? db.Get(selectedNation) : null;
            selectedNationDescription.text = data != null ? data.description : "";
        }
    }
}
