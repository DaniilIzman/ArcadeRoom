using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class FlappyGameManager : MonoBehaviour
{
    public static FlappyGameManager Instance { get; private set; }

    [Header("In-Game UI")]
    public TextMeshProUGUI scoreText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI creditsEarnedText;

    [Header("Pause Menu UI (Escape)")]
    public GameObject pausePanel; 
    public GameObject pauseMenuContainer; 
    public GameObject pauseSettingsContainer; 
    public bool isPaused { get; private set; } = false;

    [Header("Pause Menu Settings")]
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public TMP_Dropdown resolutionDropdown; // resolution Dropdown
    private Resolution[] resolutions;       // stores available screen sizes

    [Header("Economy Settings")]
    public int pipesPerCredit = 3;

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
        Time.timeScale = 1f; 
        
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(true);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        
        UpdateScoreUI();
        WirePauseMenuAudio(); 
        InitializeResolutionDropdown(); 

        // apply the saved audio volumes the exact moment the level loads
        LoadAudioSettings(); 
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused && pauseSettingsContainer.activeSelf)
            {
                ClosePauseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    // pause & settings navigation

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        
        if (pausePanel != null) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            pauseMenuContainer.SetActive(true);
            pauseSettingsContainer.SetActive(false);
            LoadAudioSettings();
        }
        else
        {
            SaveAudioSettingsToDisk();
        }
    }

    public void OpenPauseSettings()
    {
        pauseMenuContainer.SetActive(false);
        pauseSettingsContainer.SetActive(true);
    }

    public void ClosePauseSettings()
    {
        SaveAudioSettingsToDisk(); 
        pauseSettingsContainer.SetActive(false);
        pauseMenuContainer.SetActive(true);
    }

    // video/resolution settings

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        // get all resolutions supported by the player's monitor
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;
        int savedResIndex = PlayerPrefs.GetInt("Setting_ResolutionIndex", -1);

        for (int i = 0; i < resolutions.Length; i++)
        {
            // format: 1920 x 1080
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // if no save file exists, default to the monitor's native resolution
            if (savedResIndex == -1)
            {
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = i;
                }
            }
        }

        // apply saved resolution if it exists
        if (savedResIndex != -1) currentResIndex = savedResIndex;

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        // listen for user changes
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        // save the index so it persists between the Arcade Room and Flappy Bird
        PlayerPrefs.SetInt("Setting_ResolutionIndex", resolutionIndex);
        PlayerPrefs.Save();
    }

    // audio settings

    private void WirePauseMenuAudio()
    {
        if (musicSlider)
        {
            musicSlider.onValueChanged.RemoveAllListeners(); 
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxSlider)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        if (uiSlider)
        {
            uiSlider.onValueChanged.RemoveAllListeners();
            uiSlider.onValueChanged.AddListener(SetUIVolume);
        }
    }

    private void LoadAudioSettings()
    {
        if (musicSlider) musicSlider.value = PlayerPrefs.GetFloat("Setting_MusicVol", 0.75f);
        if (sfxSlider) sfxSlider.value = PlayerPrefs.GetFloat("Setting_SFXVol", 0.75f);
        if (uiSlider) uiSlider.value = PlayerPrefs.GetFloat("Setting_UIVol", 0.75f);

        SetMusicVolume(musicSlider ? musicSlider.value : 0.75f);
        SetSFXVolume(sfxSlider ? sfxSlider.value : 0.75f);
        SetUIVolume(uiSlider ? uiSlider.value : 0.75f);
    }

    public void SetMusicVolume(float val) => ApplyVolumeToMixer("MusicVol", val);
    public void SetSFXVolume(float val) => ApplyVolumeToMixer("SFXVol", val);
    public void SetUIVolume(float val) => ApplyVolumeToMixer("UIVol", val);

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (audioMixer == null) return;
        
        if (sliderValue <= 0.0001f) audioMixer.SetFloat(parameterName, -80f); 
        else audioMixer.SetFloat(parameterName, Mathf.Log10(sliderValue) * 20f);
    }

    private void SaveAudioSettingsToDisk()
    {
        if (musicSlider) PlayerPrefs.SetFloat("Setting_MusicVol", musicSlider.value);
        if (sfxSlider) PlayerPrefs.SetFloat("Setting_SFXVol", sfxSlider.value);
        if (uiSlider) PlayerPrefs.SetFloat("Setting_UIVol", uiSlider.value);
        PlayerPrefs.Save();
    }

    // scoring and game over logic

    public void AddScore()
    {
        if (isGameOver) return;

        currentScore++;
        UpdateScoreUI();

        if (audioSource != null && scoreSound != null) audioSource.PlayOneShot(scoreSound);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (audioSource != null && deathSound != null) audioSource.PlayOneShot(deathSound);

        int earnedCredits = currentScore / pipesPerCredit;
        SaveFlightData(earnedCredits); 

        gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {currentScore}";
        if (creditsEarnedText != null) creditsEarnedText.text = $"EARNED: {earnedCredits} CREDITS";
    }

    private void SaveFlightData(int creditsToAdd)
    {
        // get the current active save slot
        int activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        
        // use the exact key from your Arcade Room GameManager
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";
        
        // fetch current balance (defaulting to 500 to match your startingCredits logic)
        int currentCredits = PlayerPrefs.GetInt(creditsKey, 500);
        
        // add the reward and save back to the registry
        PlayerPrefs.SetInt(creditsKey, currentCredits + creditsToAdd);

        // save Leaderboard Data
        string prefsKey = $"FlappyHistory_Slot{activeSlot}";
        FlappyLeaderboard board = new FlappyLeaderboard();
        string json = PlayerPrefs.GetString(prefsKey, "");
        if (!string.IsNullOrEmpty(json)) board = JsonUtility.FromJson<FlappyLeaderboard>(json);

        FlappyScoreEntry newEntry = new FlappyScoreEntry();
        newEntry.attemptNumber = board.entries.Count + 1;
        newEntry.date = System.DateTime.Now.ToString("MM/dd/yy HH:mm");
        newEntry.score = currentScore;

        board.entries.Add(newEntry);
        PlayerPrefs.SetString(prefsKey, JsonUtility.ToJson(board));
        PlayerPrefs.Save();
    }

    // button functions

    public void ResumeGame()
    {
        if (isPaused) TogglePause(); 
    }

    public void TryAgain()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f; 
        SaveAudioSettingsToDisk(); 
        SceneManager.LoadScene("FlappyMenu"); 
    }
}