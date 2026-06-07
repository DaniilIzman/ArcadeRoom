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
    
    [Header("Scoring & Economy")]
    public int currentScore = 0;
    [Tooltip("How many points equal 1 Arcade Credit?")]
    public int pointsPerCredit = 50; 
    
    [Header("UI - Mid Game")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText; //in-game score display
    public TextMeshProUGUI warningText;
    
    [Header("UI - Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;     // gme Over score
    public TextMeshProUGUI creditsEarnedText;  // game Over payout info
    
    [Header("Scene Routing")]
    public string mainMenuSceneName = "SpaceInvadersMenu";

    private int activeSlot;
    private int currentCredits;
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f; 
        gameOverPanel.SetActive(false);
        if (warningText) warningText.gameObject.SetActive(false);

        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        UpdateUI();
    }

    // called by invaders when they die
    public void AddScore(int points)
    {
        if (isGameOver) return;
        
        currentScore += points;
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
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        
        StartCoroutine(SafeGameOverRoutine());
    }

    private IEnumerator SafeGameOverRoutine()
    {
        yield return new WaitForEndOfFrame();

        Time.timeScale = 0f; 
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // credit conversion logic
        int creditsEarned = currentScore / pointsPerCredit;

        // load wallet, add newly earned credits, and save
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";
        currentCredits = PlayerPrefs.GetInt(creditsKey, 500);
        currentCredits += creditsEarned;
        
        PlayerPrefs.SetInt(creditsKey, currentCredits);
        PlayerPrefs.Save();

        // update game over ui
        if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {currentScore}";
        if (creditsEarnedText != null) 
        {
            creditsEarnedText.text = $"CREDITS EARNED: +{creditsEarned}\nNEW BALANCE: {currentCredits}";
        }
        
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void TryAgain()
    {
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";
        currentCredits = PlayerPrefs.GetInt(creditsKey, 500); // reads the newly updated balance

        if (currentCredits >= costPerPlay)
        {
            currentCredits -= costPerPlay;
            PlayerPrefs.SetInt(creditsKey, currentCredits);
            PlayerPrefs.Save();

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            if (warningText != null)
            {
                warningText.text = $"INSERT COIN! ({costPerPlay} REQ)";
                warningText.gameObject.SetActive(true);
            }
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void UpdateUI()
    {
        if (livesText != null) livesText.text = $"LIVES: {playerLives}";
        if (scoreText != null) scoreText.text = $"SCORE: {currentScore}";
    }
}