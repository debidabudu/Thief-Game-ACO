using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMainMenu : MonoBehaviour
{
    public void GoToMainMenu()
    {
        SceneManager.LoadSceneAsync("Main Menu"); // Replace "MainMenu" with your actual Main Menu scene name
    }
}
