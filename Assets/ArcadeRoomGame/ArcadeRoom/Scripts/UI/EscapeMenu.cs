using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 
using UnityEngine.Audio; 
using System.Collections.Generic;
using System.Collections; 
using TMPro; 

public class EscapeMenu : MonoBehaviour
{
    public static EscapeMenu Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Settings Controls")]
    public TMP_Dropdown resolutionDropdown;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider sensitivitySlider;
    
    [Header("Dynamic Button Settings")]
    public TextMeshProUGUI backButtonText;

    [Header("Audio Mixer Routing")]
    public AudioMixer mainMixer;
    public string musicParamName = "MusicVol";
    public string sfxParamName = "SFXVol";

    [Header("Scene Routing")]
    public string mainMenuSceneName = "MainMenu";
    public float mainMenuLoadDelay = 1.0f;

    private bool isPaused = false;
    private bool hasChanges = false; 
    public bool canPause = true; // ssed to lock menu during load transitions
    private Resolution[] availableResolutions;
    
    private PlayerCamera cachedCamera;
    private PlayerMovement cachedMovement;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        cachedCamera = Object.FindFirstObjectByType<PlayerCamera>();
        cachedMovement = Object.FindFirstObjectByType<PlayerMovement>();

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        InitializeResolutionOptions();
        LoadAndApplySettings();
        ResetBackButtonText();

        // automatic slider wiring (Fixes non-functional sliders)
        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && canPause)
        {
            if (settingsPanel.activeSelf) CloseSettingsAndSave();
            else TogglePauseState();
        }
    }

    public void TogglePauseState()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        // ambient Music fix
        if (AmbientAudio.Instance != null)
        {
            if (isPaused) AmbientAudio.Instance.PauseMusic();
            else AmbientAudio.Instance.ResumeMusic();
        }

        pausePanel.SetActive(isPaused);
        UpdatePlayerConstraints(isPaused);
        ManageCursorState(isPaused);
    }

    // called by arcadeMachine to prevent overlaps
    public void ForceCloseAndLock()
    {
        canPause = false;
        if (isPaused) TogglePauseState();
    }

    public void UnlockMenu() => canPause = true;

    public void OpenSettings()
    {
        hasChanges = false;
        ResetBackButtonText();
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettingsAndSave()
    {
        if (hasChanges)
        {
            PlayerPrefs.SetFloat("Setting_MusicVol", musicVolumeSlider.value);
            PlayerPrefs.SetFloat("Setting_SFXVol", sfxVolumeSlider.value);
            PlayerPrefs.SetFloat("Setting_MouseSensitivity", sensitivitySlider.value);
            PlayerPrefs.SetInt("Setting_Resolution", resolutionDropdown.value);
            
            PlayerPrefs.Save();
            hasChanges = false;
        }

        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
        ResetBackButtonText();
    }

    public void LoadMainMenu() => StartCoroutine(DelayedMenuLoadRoutine());

    private IEnumerator DelayedMenuLoadRoutine()
    {
        yield return new WaitForSecondsRealtime(mainMenuLoadDelay);
        Time.timeScale = 1f; 
        if (!string.IsNullOrEmpty(mainMenuSceneName)) SceneManager.LoadScene(mainMenuSceneName);
    }

    #region Settings Logic

    private void ResetBackButtonText()
    {
        if (backButtonText != null) backButtonText.text = "Back";
    }

    private void MarkSettingsAsDirty()
    {
        if (!hasChanges)
        {
            hasChanges = true;
            if (backButtonText != null) backButtonText.text = "Confirm & Save";
        }
    }

    private void InitializeResolutionOptions()
    {
        if (resolutionDropdown == null) return;
        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = availableResolutions[i].width + " x " + availableResolutions[i].height;
            options.Add(option);

            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        int savedRes = PlayerPrefs.GetInt("Setting_Resolution", currentResolutionIndex);
        resolutionDropdown.value = savedRes;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (availableResolutions == null || resolutionIndex >= availableResolutions.Length) return;
        Resolution res = availableResolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        MarkSettingsAsDirty();
    }

    public void SetMusicVolume(float value)
    {
        ApplyVolumeToMixer(musicParamName, value);
        MarkSettingsAsDirty();
    }

    public void SetSFXVolume(float value)
    {
        ApplyVolumeToMixer(sfxParamName, value);
        MarkSettingsAsDirty();
    }

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (mainMixer == null) return;
        if (sliderValue <= 0.0001f) mainMixer.SetFloat(parameterName, -80f); 
        else mainMixer.SetFloat(parameterName, Mathf.Log10(sliderValue) * 20f);
    }

    public void SetSensitivity(float value)
    {
        ApplySensitivityToCamera(value);
        MarkSettingsAsDirty();
    }

    private void LoadAndApplySettings()
    {
        float musicVol = PlayerPrefs.GetFloat("Setting_MusicVol", 0.75f);
        float sfxVol = PlayerPrefs.GetFloat("Setting_SFXVol", 0.75f);
        float sensitivity = PlayerPrefs.GetFloat("Setting_MouseSensitivity", 2.0f);

        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (sensitivitySlider != null) sensitivitySlider.value = sensitivity;

        ApplyVolumeToMixer(musicParamName, musicVol);
        ApplyVolumeToMixer(sfxParamName, sfxVol);
        ApplySensitivityToCamera(sensitivity);
    }

    private void ApplySensitivityToCamera(float sensitivityValue)
    {
        if (cachedCamera != null) cachedCamera.mouseSensitivity = sensitivityValue;
    }

    private void UpdatePlayerConstraints(bool shouldFreeze)
    {
        // menu only targets the pause flag
        if (cachedCamera != null) cachedCamera.isPausedByMenu = shouldFreeze;
        if (cachedMovement != null) cachedMovement.isPausedByMenu = shouldFreeze;
    }

    private void ManageCursorState(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    #endregion

    private void OnDestroy() => Time.timeScale = 1f;
}