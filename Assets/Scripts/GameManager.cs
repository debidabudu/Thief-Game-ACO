using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public GameObject thief;
    public List<GameObject> homeownerPrefabs;
    public Transform items_to_steal;
    public GameObject gameOverPanel;
    public GameObject youWinPanel; 

    public int lives { get; private set; }

    public int items_taken = 0;
    private int items_per_speed_increase = 2;
    
    private float speed_increase_amount = 0.5f;

    private void Start()
    {
        NewGame();
    }

    private void Update()
    {
        if (gameOverPanel.activeSelf || youWinPanel.activeSelf)
        {
            Time.timeScale = 0f;
            return;
        }

        if (AllItemsTaken() && thief.activeSelf)
        {
            YouWin();
        }
    }
    public void NewGame()
    {
        gameOverPanel.SetActive(false);
        youWinPanel.SetActive(false);

        Time.timeScale = 1f;

        SetLives(1);

        ResetHomeownerSpeed();
        NewRound();
    }
    private void NewRound()
    {
        foreach (Transform item in this.items_to_steal)
        {
            item.gameObject.SetActive(true);
        }

        this.thief.gameObject.SetActive(true);
    }

    public void ThiefCaught()
    {
        SetLives(0);
        GameOver();
    }
    private void GameOver()
    {
        foreach (Transform item in this.items_to_steal)
        {
            item.gameObject.SetActive(false);
        }

        gameOverPanel.SetActive(true);
    }
    private void YouWin()
    {
        youWinPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private bool AllItemsTaken()
    {
        foreach (Transform item in items_to_steal)
        {
            if (item.gameObject.activeSelf)
            {
                return false;
            }
        }
        return true;
    }
    public void ItemTaken(Items item)
    {   
        item.gameObject.SetActive(false);

        items_taken = items_taken + 1;
        
        if (!HasRemainingItems()) 
        {
            Invoke(nameof(NewRound), 3.0f);
        }

        if (items_taken % items_per_speed_increase == 0)
        {
            IncreaseHomeownerSpeed();
        }

        if (items_taken >= 2)
        {
            foreach (GameObject homeowner in homeownerPrefabs)
            {
                if (!homeowner.activeSelf)
                {
                    homeowner.SetActive(true);
                }
            }
        }
    }
    private bool HasRemainingItems()
    {
        foreach (Transform item in this.items_to_steal)
        {
            if (item.gameObject.activeSelf) 
            {
                return true;
            }
        }
        return false;
    }

    private void SetLives(int lives)
    {
        this.lives = lives;
    }

    private void IncreaseHomeownerSpeed()
    {
        foreach (GameObject homeowner in homeownerPrefabs)
        {
            HomeownerAI homeownerAI = homeowner.GetComponent<HomeownerAI>();
            if (homeownerAI != null)
            {
                homeownerAI.IncreaseSpeed(speed_increase_amount);
            }
        }
    }
    private void ResetHomeownerSpeed()
    {
        foreach (GameObject homeowner in homeownerPrefabs)
        {
            HomeownerAI homeownerAI = homeowner.GetComponent<HomeownerAI>();
            if (homeownerAI != null)
            {
                homeownerAI.ResetSpeed();
            }
        }
    }

    public void PlayAgain()
    {
        // Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        NewGame();
    }
    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

}
