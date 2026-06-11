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

    [Header("Audio Sources")]
    public AudioSource musicSource; 
    public AudioSource audioSource; // used for gameplay SFX (Score, Crash)
    public AudioSource uiSource;    // dedicated UI Audio Source

    [Header("Gameplay Audio Clips")]
    public AudioClip scoreSound;
    public AudioClip gameOverSound; 

    [Header("UI Audio Clips")]
    public AudioClip clickSound;    // sound played when clicking menu buttons
    public AudioClip sliderSound;   // short tick sound played when dragging sliders

    // shared uniform key configurations to prevent lookup typos across files
    private const string MusicVolKey = "Setting_MusicVol";
    private const string SfxVolKey = "Setting_SFXVol";
    private const string UiVolKey = "Setting_UIVol";
    private const string ResIndexKey = "Setting_ResolutionIndex";
    private const string SlotKey = "Global_LastPlayedSlot";

    public int currentScore { get; private set; } = 0;
    public bool isGameOver { get; private set; } = false;

    // cooldown-based slider sound guard (matches FlappyMenuController pattern)
    // negative sentinel means "not ready yet" — armed at the end of Start()
    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f; 
        
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        TogglePauseUIContainers(true, false);
        
        UpdateScoreUI();
        WirePauseMenuAudio(); 
        InitializeResolutionDropdown(); 
        LoadAudioSettings(); 

        // arm the cooldown timer — any slider callbacks fired before this point are ignored
        lastSliderSoundTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused && pauseSettingsContainer && pauseSettingsContainer.activeSelf)
            {
                PlayClickAudio();
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
        PlayClickAudio();
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

    public void OpenPauseSettings()
    {
        PlayClickAudio();
        TogglePauseUIContainers(false, true);
    }

    public void ClosePauseSettings()
    {
        PlayClickAudio();
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

        Resolution[] rawResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        List<Resolution> uniqueResolutions = new List<Resolution>();
        int currentResIndex = 0;
        int savedResIndex = PlayerPrefs.GetInt(ResIndexKey, -1);

        // Filter out redundant entries from different refresh rates to prevent UI layout disruptions
        for (int i = 0; i < rawResolutions.Length; i++)
        {
            string option = $"{rawResolutions[i].width} x {rawResolutions[i].height}";
            if (!options.Contains(option))
            {
                options.Add(option);
                uniqueResolutions.Add(rawResolutions[i]);
            }
        }

        resolutions = uniqueResolutions.ToArray();

        for (int i = 0; i < resolutions.Length; i++)
        {
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
        resolutionDropdown.value = Mathf.Clamp(currentResIndex, 0, resolutions.Length - 1);
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex >= resolutions.Length) return;
        
        PlayClickAudio();
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        // Force instantaneous UI layout boundary updates for clean layout scaling
        Canvas.ForceUpdateCanvases();
        
        PlayerPrefs.SetInt(ResIndexKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    private void WirePauseMenuAudio()
    {
        ConfigureSlider(musicSlider, SetMusicVolume);
        ConfigureSlider(sfxSlider, SetSFXVolume);
        ConfigureSlider(uiSlider, SetUIVolume);

        // auto-wire all buttons under the pause panel so any new button added
        // in the Inspector is automatically covered — no manual hookup needed
        if (pausePanel != null)
        {
            Button[] pauseButtons = pausePanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in pauseButtons)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(PlayClickAudio);
            }
        }
    }

    private void ConfigureSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
        slider.onValueChanged.AddListener((val) => PlaySliderTickAudio());
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

    public void SetMusicVolume(float val) 
    {
        ApplyVolumeToMixer("MusicVol", val);
        PlaySliderTickAudio();
    }
    
    public void SetSFXVolume(float val) 
    {
        ApplyVolumeToMixer("SFXVol", val);
        PlaySliderTickAudio();
    }
    
    public void SetUIVolume(float val) 
    {
        ApplyVolumeToMixer("UIVol", val);
        PlaySliderTickAudio();
    }

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

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        if (audioSource != null && gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound);
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
        
        // Removed default 500 credit automatic allocation fallback injection
        int currentCredits = PlayerPrefs.GetInt(creditsKey, 0);
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
        PlayClickAudio();
        if (isPaused) TogglePause(); 
    }

    public void TryAgain()
    {
        PlayClickAudio();
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        PlayClickAudio();
        Time.timeScale = 1f; 
        SaveAudioSettingsToDisk(); 
        SceneManager.LoadScene("FlappyMenu"); 
    }

    // helper method for ui audio
    public void PlayClickAudio()
    {
        if (uiSource != null && clickSound != null)
        {
            uiSource.PlayOneShot(clickSound);
        }
    }

    private void PlaySliderTickAudio()
    {
        if (lastSliderSoundTime < 0f) return;

        if (Time.unscaledTime - lastSliderSoundTime >= sliderSoundCooldown)
        {
            if (uiSource != null && sliderSound != null)
            {
                // tiny jiggle makes slider dragging sound more organic
                uiSource.pitch = Random.Range(0.95f, 1.05f);
                uiSource.PlayOneShot(sliderSound);
                uiSource.pitch = 1f; // reset pitch
                lastSliderSoundTime = Time.unscaledTime;
            }
        }
    }
}