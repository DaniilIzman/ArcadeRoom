using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System; // required for DateTime

public class FlappyGameManager : MonoBehaviour
{
    public static FlappyGameManager Instance { get; private set; }

    [Header("In-Game UI")]
    public TextMeshProUGUI scoreText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip scoreSound;
    public AudioClip deathSound;

    public int currentScore { get; private set; } = 0;
    public bool isGameOver { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        gameOverPanel.SetActive(false);
        UpdateScoreUI();
    }

    public void AddScore()
    {
        if (isGameOver) return;

        currentScore++;
        UpdateScoreUI();

        if (audioSource != null && scoreSound != null)
        {
            audioSource.PlayOneShot(scoreSound);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // show Game Over screen
        gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {currentScore}";

        SaveFlightData(); // log the attempt
    }

    private void SaveFlightData()
    {
        // figure out which save slot is playing
        int activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        string prefsKey = $"FlappyHistory_Slot{activeSlot}";
        
        // load the existing history
        string json = PlayerPrefs.GetString(prefsKey, "");
        FlappyLeaderboard board = new FlappyLeaderboard();
        
        if (!string.IsNullOrEmpty(json))
        {
            board = JsonUtility.FromJson<FlappyLeaderboard>(json);
        }

        // create the new flight log entry
        FlappyScoreEntry newEntry = new FlappyScoreEntry();
        newEntry.attemptNumber = board.entries.Count + 1;
        newEntry.date = DateTime.Now.ToString("MM/dd/yy HH:mm"); // e.g., 10/24/26 14:30
        newEntry.score = currentScore;

        // save it back to PlayerPrefs
        board.entries.Add(newEntry);
        PlayerPrefs.SetString(prefsKey, JsonUtility.ToJson(board));
        PlayerPrefs.Save();
    }

    // button functions

    public void TryAgain()
    {
        // reloads the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("FlappyMenu"); 
    }
}