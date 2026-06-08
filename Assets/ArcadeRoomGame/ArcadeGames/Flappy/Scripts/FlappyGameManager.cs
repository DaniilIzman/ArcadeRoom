using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
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
    public TMP_Dropdown resolutionDropdown; 
    private Resolution[] resolutions;       

    [Header("Economy Settings")]
    public int pipesPerCredit = 3;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip scoreSound;
    public AudioClip deathSound;

    // shared uniform key configurations to prevent lookup typos across files
    private const string MusicVolKey = "Setting_MusicVol";
    private const string SfxVolKey = "Setting_SFXVol";
    private const string UiVolKey = "Setting_UIVol";
    private const string ResIndexKey = "Setting_ResolutionIndex";
    private const string SlotKey = "Global_LastPlayedSlot";

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
        
        // initialize baseline default state for runtime user interfaces
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        TogglePauseUIContainers(true, false);
        
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
            if (isPaused && pauseSettingsContainer && pauseSettingsContainer.activeSelf)
            {
                ClosePauseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        
        if (pausePanel) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            TogglePauseUIContainers(true, false);
            LoadAudioSettings();
        }
        else
        {
            SaveAudioSettingsToDisk();
        }
    }

    public void OpenPauseSettings() => TogglePauseUIContainers(false, true);

    public void ClosePauseSettings()
    {
        SaveAudioSettingsToDisk(); 
        TogglePauseUIContainers(true, false);
    }

    private void TogglePauseUIContainers(bool menuActive, bool settingsActive)
    {
        if (pauseMenuContainer) pauseMenuContainer.SetActive(menuActive);
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(settingsActive);
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        // get all resolutions supported by the player's monitor
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;
        int savedResIndex = PlayerPrefs.GetInt(ResIndexKey, -1);

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
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

        if (savedResIndex != -1) currentResIndex = savedResIndex;

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex >= resolutions.Length) return;
        
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        PlayerPrefs.SetInt(ResIndexKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    private void WirePauseMenuAudio()
    {
        ConfigureSlider(musicSlider, SetMusicVolume);
        ConfigureSlider(sfxSlider, SetSFXVolume);
        ConfigureSlider(uiSlider, SetUIVolume);
    }

    private void ConfigureSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
    }

    private void LoadAudioSettings()
    {
        float targetMusic = PlayerPrefs.GetFloat(MusicVolKey, 0.75f);
        float targetSfx = PlayerPrefs.GetFloat(SfxVolKey, 0.75f);
        float targetUi = PlayerPrefs.GetFloat(UiVolKey, 0.75f);

        if (musicSlider) musicSlider.value = targetMusic;
        if (sfxSlider) sfxSlider.value = targetSfx;
        if (uiSlider) uiSlider.value = targetUi;

        SetMusicVolume(targetMusic);
        SetSFXVolume(targetSfx);
        SetUIVolume(targetUi);
    }

    public void SetMusicVolume(float val) => ApplyVolumeToMixer("MusicVol", val);
    public void SetSFXVolume(float val) => ApplyVolumeToMixer("SFXVol", val);
    public void SetUIVolume(float val) => ApplyVolumeToMixer("UIVol", val);

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (audioMixer == null) return;
        float targetDb = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, targetDb);
    }

    private void SaveAudioSettingsToDisk()
    {
        if (musicSlider) PlayerPrefs.SetFloat(MusicVolKey, musicSlider.value);
        if (sfxSlider) PlayerPrefs.SetFloat(SfxVolKey, sfxSlider.value);
        if (uiSlider) PlayerPrefs.SetFloat(UiVolKey, uiSlider.value);
        PlayerPrefs.Save();
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

        int earnedCredits = currentScore / pipesPerCredit;
        SaveFlightData(earnedCredits); 

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {currentScore}";
        if (creditsEarnedText != null) creditsEarnedText.text = $"EARNED: {earnedCredits} CREDITS";
    }

    private void SaveFlightData(int creditsToAdd)
    {
        int activeSlot = PlayerPrefs.GetInt(SlotKey, 1);
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";
        
        int currentCredits = PlayerPrefs.GetInt(creditsKey, 500);
        PlayerPrefs.SetInt(creditsKey, currentCredits + creditsToAdd);

        string prefsKey = $"FlappyHistory_Slot{activeSlot}";
        FlappyLeaderboard board = new FlappyLeaderboard();
        string json = PlayerPrefs.GetString(prefsKey, "");
        
        if (!string.IsNullOrEmpty(json))
        {
            board = JsonUtility.FromJson<FlappyLeaderboard>(json);
        }

        FlappyScoreEntry newEntry = new FlappyScoreEntry
        {
            attemptNumber = board.entries.Count + 1,
            date = System.DateTime.Now.ToString("MM/dd/yy HH:mm"),
            score = currentScore
        };

        board.entries.Add(newEntry);
        PlayerPrefs.SetString(prefsKey, JsonUtility.ToJson(board));
        PlayerPrefs.Save();
    }

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