using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SpaceInvadersManager : MonoBehaviour
{
    public static SpaceInvadersManager Instance { get; private set; }

    [Header("Game Stats")]
    public int playerLives = 3;
    public int costPerPlay = 10;
    
    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI warningText;
    
    [Header("Scene Routing")]
    public string mainMenuSceneName = "SpaceInvadersMenu";

    private int activeSlot;
    private int currentCredits;
    private bool isGameOver = false;

    private void Awake()
    {
        // set up the Singleton so other scripts can find this manager instantly
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f; // ensure time is running normally when the scene loads
        gameOverPanel.SetActive(false);
        if (warningText) warningText.gameObject.SetActive(false);

        // load the player's wallet data
        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        UpdateUI();
    }

    public void LoseLife()
    {
        if (isGameOver) return;

        playerLives--;
        UpdateUI();

        if (playerLives <= 0)
        {
            TriggerGameOver();
        }
        else
        {
            Debug.Log($"Life lost! Remaining lives: {playerLives}");
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        
        // start the safe freeze routine instead of doing it instantly
        StartCoroutine(SafeGameOverRoutine());
    }

    private IEnumerator SafeGameOverRoutine()
    {
        // wait until the very end of the current frame (lets PhysX finish its job)
        yield return new WaitForEndOfFrame();

        Time.timeScale = 0f; // now it is safe to freeze time
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    // ui button event - try again
    public void TryAgain()
    {
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";
        currentCredits = PlayerPrefs.GetInt(creditsKey, 500);

        if (currentCredits >= costPerPlay)
        {
            // deduct credits and save
            currentCredits -= costPerPlay;
            PlayerPrefs.SetInt(creditsKey, currentCredits);
            PlayerPrefs.Save();

            // unfreeze and reload the current active scene
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            // display warning if they are broke!
            if (warningText != null)
            {
                warningText.text = $"INSERT COIN! ({costPerPlay} REQ)";
                warningText.gameObject.SetActive(true);
            }
        }
    }

    // ui button event: Main Menu
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // always unfreeze before loading a new scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void UpdateUI()
    {
        if (livesText != null)
        {
            livesText.text = $"LIVES: {playerLives}";
        }
    }
}