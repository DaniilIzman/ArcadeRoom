using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class EndlessRunnerManager : MonoBehaviour
{
    public static EndlessRunnerManager Instance { get; private set; }

    [Header("Gameplay Stats")]
    public int score = 0;
    public int distance = 0;
    public bool isGameOver { get; private set; } = false;
    public bool isPaused { get; private set; } = false;

    [Header("Economy Sync")]
    public int costPerPlay = 10;
    public int scorePerCredit = 500; // E.g., 500 points = 1 credit earned
    public string baseCreditsKey = "PlayerCredits";
    public string mainMenuSceneName = "EndlessRunnerMenu";

    [Header("Mid-Game UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI distanceText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalDistanceText;
    public TextMeshProUGUI creditsEarnedText;
    public TextMeshProUGUI insufficientCreditsWarningText;

    [Header("Pause Menu UI")]
    public GameObject pausePanel;
    public GameObject pauseSettingsContainer;
    public GameObject pauseMainContainer;
    
    [Header("Settings UI")]
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public TMP_Dropdown resolutionDropdown;

    [Header("Audio Sources")]
    public AudioSource sfxAudioSource;
    public AudioSource uiAudioSource;
    public AudioClip clickSound;
    public AudioClip sliderTickSound;
    public AudioClip coinPickupSound;
    public AudioClip crashSound;

    // matching exact keys from EndlessRunnerMenuController
    private const string mixerMusicParam = "MusicVol";
    private const string mixerSfxParam = "SFXVol";
    private const string mixerUiParam = "UIVol";
    private const string prefMusic = "Setting_MusicVol";
    private const string prefSfx = "Setting_SFXVol";
    private const string prefUi = "Setting_UIVol";
    private const string prefResWidth = "Setting_ResWidth";
    private const string prefResHeight = "Setting_ResHeight";
    private const string prefSlot = "Global_LastPlayedSlot";

    private Resolution[] resolutions;
    private int activeSlot;
    private string creditsPrefsKey;
    private float nextSliderSoundTime = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        
        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (insufficientCreditsWarningText) insufficientCreditsWarningText.gameObject.SetActive(false);

        UpdateGameplayUI();
        InitializeResolutionDropdown();
        WireMenuAudio();
        LoadAudioSettings();
        ApplySavedResolution();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused && pauseSettingsContainer != null && pauseSettingsContainer.activeSelf)
            {
                ClosePauseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    // gameplay loop

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        score += amount;
        UpdateGameplayUI();
    }

    public void AddDistance(int amount)
    {
        if (isGameOver) return;
        distance += amount;
        UpdateGameplayUI();
    }

    private void UpdateGameplayUI()
    {
        if (scoreText) scoreText.text = $"SCORE: {score}";
        if (distanceText) distanceText.text = $"DISTANCE: {distance}m";
    }

    public void PlayerCrashed()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (sfxAudioSource && crashSound) sfxAudioSource.PlayOneShot(crashSound);

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(0.5f); // brief delay to let animations/particles play out
        Time.timeScale = 0f;
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // calculate and save economy
        int currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0);
        int creditsEarned = score / scorePerCredit;
        currentCredits += creditsEarned;
        PlayerPrefs.SetInt(creditsPrefsKey, currentCredits);
        PlayerPrefs.Save();

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = $"FINAL SCORE: {score}";
        if (finalDistanceText) finalDistanceText.text = $"DISTANCE: {distance}m";
        if (creditsEarnedText) creditsEarnedText.text = $"CREDITS EARNED: +{creditsEarned}\nNEW BALANCE: {currentCredits}";
    }

    public void PlayCoinPickupSound()
    {
        if (sfxAudioSource && coinPickupSound) sfxAudioSource.PlayOneShot(coinPickupSound);
    }

    // scene routing and economy

    public void TryAgain()
    {
        PlayClickSound();
        int currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0);

        if (currentCredits >= costPerPlay)
        {
            currentCredits -= costPerPlay;
            PlayerPrefs.SetInt(creditsPrefsKey, currentCredits);
            PlayerPrefs.Save();

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            if (insufficientCreditsWarningText)
            {
                insufficientCreditsWarningText.text = $"INSERT COIN! ({costPerPlay} REQ)";
                insufficientCreditsWarningText.gameObject.SetActive(true);
            }
        }
    }

    public void ReturnToMainMenu()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        SaveAudioSettingsToDisk();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // synced pause and settings with menu 

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (pauseMainContainer) pauseMainContainer.SetActive(true);
            if (pauseSettingsContainer) pauseSettingsContainer.SetActive(false);
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            SaveAudioSettingsToDisk();
        }
    }

    public void OpenPauseSettings()
    {
        PlayClickSound();
        if (pauseMainContainer) pauseMainContainer.SetActive(false);
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(true);
    }

    public void ClosePauseSettings()
    {
        PlayClickSound();
        SaveAudioSettingsToDisk();
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(false);
        if (pauseMainContainer) pauseMainContainer.SetActive(true);
    }

    private void ApplySavedResolution()
    {
        int w = PlayerPrefs.GetInt(prefResWidth, Screen.currentResolution.width);
        int h = PlayerPrefs.GetInt(prefResHeight, Screen.currentResolution.height);
        Screen.SetResolution(w, h, Screen.fullScreen);
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        Resolution[] rawResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        List<Resolution> uniqueResolutions = new List<Resolution>();
        
        int savedWidth = PlayerPrefs.GetInt(prefResWidth, Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt(prefResHeight, Screen.currentResolution.height);
        int currentResIndex = 0;

        for (int i = 0; i < rawResolutions.Length; i++)
        {
            string option = $"{rawResolutions[i].width} x {rawResolutions[i].height}";
            if (!options.Contains(option))
            {
                options.Add(option);
                uniqueResolutions.Add(rawResolutions[i]);
                if (rawResolutions[i].width == savedWidth && rawResolutions[i].height == savedHeight)
                {
                    currentResIndex = uniqueResolutions.Count - 1;
                }
            }
        }
        resolutions = uniqueResolutions.ToArray();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(currentResIndex, 0, resolutions.Length - 1));
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex >= resolutions.Length) return;
        PlayClickSound();
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt(prefResWidth, resolution.width);
        PlayerPrefs.SetInt(prefResHeight, resolution.height);
    }

    private void WireMenuAudio()
    {
        if (musicSlider) { musicSlider.onValueChanged.AddListener(SetMusicVolume); musicSlider.onValueChanged.AddListener((val) => PlaySliderTickSound()); }
        if (sfxSlider) { sfxSlider.onValueChanged.AddListener(SetSFXVolume); sfxSlider.onValueChanged.AddListener((val) => PlaySliderTickSound()); }
        if (uiSlider) { uiSlider.onValueChanged.AddListener(SetUIVolume); uiSlider.onValueChanged.AddListener((val) => PlaySliderTickSound()); }
    }

    private void LoadAudioSettings()
    {
        float cachedMusic = PlayerPrefs.GetFloat(prefMusic, 0.75f);
        float cachedSfx = PlayerPrefs.GetFloat(prefSfx, 0.75f);
        float cachedUi = PlayerPrefs.GetFloat(prefUi, 0.75f);

        if (musicSlider) musicSlider.value = cachedMusic;
        if (sfxSlider) sfxSlider.value = cachedSfx;
        if (uiSlider) uiSlider.value = cachedUi;

        SetMusicVolume(cachedMusic);
        SetSFXVolume(cachedSfx);
        SetUIVolume(cachedUi);
    }

    public void SetMusicVolume(float val) => ApplyVolumeToMixer(mixerMusicParam, val);
    public void SetSFXVolume(float val) => ApplyVolumeToMixer(mixerSfxParam, val);
    public void SetUIVolume(float val) => ApplyVolumeToMixer(mixerUiParam, val);

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (audioMixer == null) return;
        float targetDb = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, targetDb);
    }

    private void SaveAudioSettingsToDisk()
    {
        if (musicSlider) PlayerPrefs.SetFloat(prefMusic, musicSlider.value);
        if (sfxSlider) PlayerPrefs.SetFloat(prefSfx, sfxSlider.value);
        if (uiSlider) PlayerPrefs.SetFloat(prefUi, uiSlider.value);
        PlayerPrefs.Save();
    }

    public void PlayClickSound()
    {
        if (uiAudioSource && clickSound) uiAudioSource.PlayOneShot(clickSound);
    }

    private void PlaySliderTickSound()
    {
        if (Time.unscaledTime >= nextSliderSoundTime && uiAudioSource && sliderTickSound)
        {
            uiAudioSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f; 
        }
    }
}