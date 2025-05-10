using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Level1_Scene");
    }

    public void OpenCredits()
    {
        SceneManager.LoadScene("CreditsScene");
    }

    public void OpenOptions()
    {
        SceneManager.LoadScene("OptionsScene");
    }
}
