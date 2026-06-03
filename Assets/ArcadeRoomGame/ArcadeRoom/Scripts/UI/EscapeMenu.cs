using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 
using System.Collections.Generic;
using TMPro; // CRITICAL: Adds support for modern TextMeshPro UI elements

public class EscapeMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Settings Controls")]
    public TMP_Dropdown resolutionDropdown; // UPDATED: Changed from Dropdown to TMP_Dropdown
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider sensitivitySlider;

    [Header("Scene Routing")]
    [Tooltip("The exact name of your Main Menu scene.")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private Resolution[] availableResolutions;
    
    private PlayerCamera cachedCamera;
    private PlayerMovement cachedMovement;

    private void Start()
    {
        cachedCamera = Object.FindFirstObjectByType<PlayerCamera>();
        cachedMovement = Object.FindFirstObjectByType<PlayerMovement>();

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        InitializeResolutionOptions();
        LoadAndApplySettings();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePauseState();
            }
        }
    }

    public void TogglePauseState()
    {
        if (isPaused)
        {
            isPaused = false;
        }
        else
        {
            isPaused = true;
        }

        if (isPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }

        pausePanel.SetActive(isPaused);
        UpdatePlayerConstraints(isPaused);
        ManageCursorState(isPaused);
    }

    public void OpenSettings()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
        PlayerPrefs.Save(); 
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("[ESCAPE MENU] Main Menu scene name is empty!");
        }
    }

    #region Settings Logic

    private void InitializeResolutionOptions()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = availableResolutions[i].width + " x " + availableResolutions[i].height;
            options.Add(option);

            if (availableResolutions[i].width == Screen.currentResolution.width)
            {
                if (availableResolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }
        }

        resolutionDropdown.AddOptions(options);

        int savedRes = PlayerPrefs.GetInt("Setting_Resolution", currentResolutionIndex);
        resolutionDropdown.value = savedRes;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (availableResolutions == null) return;
        if (resolutionIndex >= availableResolutions.Length) return;

        Resolution res = availableResolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        
        PlayerPrefs.SetInt("Setting_Resolution", resolutionIndex);
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("Setting_MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("Setting_SFXVol", value);
    }

    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat("Setting_MouseSensitivity", value);
        ApplySensitivityToCamera(value);
    }

    private void LoadAndApplySettings()
    {
        float musicVol = PlayerPrefs.GetFloat("Setting_MusicVol", 0.75f);
        float sfxVol = PlayerPrefs.GetFloat("Setting_SFXVol", 0.75f);
        float sensitivity = PlayerPrefs.GetFloat("Setting_MouseSensitivity", 2.0f);

        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (sensitivitySlider != null) sensitivitySlider.value = sensitivity;

        ApplySensitivityToCamera(sensitivity);
    }

    private void ApplySensitivityToCamera(float sensitivityValue)
    {
        if (cachedCamera != null)
        {
            // Update camera sensitivity reference if applicable
        }
    }

    private void UpdatePlayerConstraints(bool shouldFreeze)
    {
        if (cachedCamera != null)
        {
            cachedCamera.isFrozen = shouldFreeze;
        }

        if (cachedMovement != null)
        {
            cachedMovement.isFrozen = shouldFreeze;
        }
    }

    private void ManageCursorState(bool visible)
    {
        if (visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    #endregion
}