using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync("ThiefSimulator");
    }

    public void HowToPlay()
    {
        SceneManager.LoadSceneAsync("How To Play");
    }

    public void Credits()
    {
        SceneManager.LoadSceneAsync("Credits");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
