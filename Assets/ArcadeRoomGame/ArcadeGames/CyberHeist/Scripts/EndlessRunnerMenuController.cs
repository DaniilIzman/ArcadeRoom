using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class EndlessRunnerMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Economy Settings")]
    public int costPerPlay = 10;
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI insufficientCreditsWarningText; 
    [Tooltip("Must perfectly match the string your Arcade Room uses to save money.")]
    public string baseCreditsKey = "PlayerCredits";

    [Header("Settings UI")]
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public TMP_Dropdown resolutionDropdown;

    [Header("Audio Feedback")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;
    public AudioClip sliderTickSound;

    [Header("Scene Routing")]
    public string gameSceneName = "EndlessRunnerLevel";
    public string arcadeRoomSceneName = "ArcadeRoom";

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
    private int currentCredits;
    private string creditsPrefsKey;

    private float nextSliderSoundTime = 0f;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        if (insufficientCreditsWarningText) insufficientCreditsWarningText.gameObject.SetActive(false);

        TogglePanels(true, false);

        ApplySavedResolution();
        InitializeResolutionDropdown();
        WireMenuAudio();
        LoadAudioSettings();
        RefreshCreditsUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
            {
                ReturnToMainMenu();
            }
        }
    }

    private void RefreshCreditsUI()
    {
        currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0); 
        if (creditsText != null) creditsText.text = $"CREDITS: {currentCredits}";
    }

    public void AttemptStartGame()
    {
        if (currentCredits >= costPerPlay)
        {
            currentCredits -= costPerPlay;
            PlayerPrefs.SetInt(creditsPrefsKey, currentCredits);
            PlayerPrefs.Save();
            
            PlayClickSound();
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            PlayClickSound();
            if (insufficientCreditsWarningText)
            {
                insufficientCreditsWarningText.text = $"INSERT COIN! ({costPerPlay} REQ)";
                insufficientCreditsWarningText.gameObject.SetActive(true);
            }
        }
    }

    public void OpenSettings()
    {
        PlayClickSound();
        TogglePanels(false, true);
    }

    public void ReturnToMainMenu()
    {
        PlayClickSound();
        TogglePanels(true, false);
        if (insufficientCreditsWarningText) insufficientCreditsWarningText.gameObject.SetActive(false);
        SaveAudioSettingsToDisk();
    }

    public void LeaveArcadeMachine()
    {
        PlayClickSound();
        SaveAudioSettingsToDisk();
        SceneManager.LoadScene(arcadeRoomSceneName);
    }

    private void TogglePanels(bool main, bool settings)
    {
        if (mainPanel) mainPanel.SetActive(main);
        if (settingsPanel) settingsPanel.SetActive(settings);
    }

    // resolution logic

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
        Canvas.ForceUpdateCanvases();

        PlayerPrefs.SetInt(prefResWidth, resolution.width);
        PlayerPrefs.SetInt(prefResHeight, resolution.height);
        PlayerPrefs.Save();
    }

    // audio logic

    private void WireMenuAudio()
    {
        if (musicSlider)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener((val) => PlaySliderTickSound());
        }
        if (sfxSlider)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener((val) => PlaySliderTickSound());
        }
        if (uiSlider)
        {
            uiSlider.onValueChanged.RemoveAllListeners();
            uiSlider.onValueChanged.AddListener(SetUIVolume);
            uiSlider.onValueChanged.AddListener((val) => PlaySliderTickSound());
        }
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
        if (uiAudioSource != null && clickSound != null)
        {
            uiAudioSource.PlayOneShot(clickSound);
        }
    }

    private void PlaySliderTickSound()
    {
        if (Time.unscaledTime >= nextSliderSoundTime && uiAudioSource != null && sliderTickSound != null)
        {
            uiAudioSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f; 
        }
    }
}