using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    //public void ToggleZoom()
    //{
    //    // Example toggle logic
    //    Debug.Log("Zoom Toggled!");
    //}

    //public void ToggleMusic()
    //{
    //    // Example toggle logic
    //    Debug.Log("Music Toggled!");
    //}

    //public void SetDifficulty(string level)
    //{
    //    PlayerPrefs.SetString("Difficulty", level);
    //    Debug.Log("Difficulty set to: " + level);
    //}

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
