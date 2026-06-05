using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    private enum SlotMenuMode { NewGame, Continue }
    private SlotMenuMode currentSlotMode;

    [Header("Scene Routing")]
    public string gameSceneName = "ArcadeRoom";
    public float sceneLoadDelay = 1.0f;

    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject slotSelectionPanel;

    [Header("Main Menu Buttons")]
    public Button continueButton;
    public CanvasGroup continueButtonCanvasGroup;

    [Header("Slot Selection UI")]
    public TextMeshProUGUI[] slotInfoTexts; 
    public TextMeshProUGUI slotPanelTitle;  

    [Header("Settings Controls")]
    public TMP_Dropdown resolutionDropdown;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider uiVolumeSlider;
    public Slider sensitivitySlider;
    public TextMeshProUGUI settingsBackButtonText;

    [Header("Audio Mixer Routing")]
    public AudioMixer mainMixer;
    public string musicParamName = "MusicVol";
    public string sfxParamName = "SFXVol";
    public string uiParamName = "UIVol";

    [Header("Audio Feedback - Menu Sounds")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip sliderTickSound;
    [SerializeField] private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;

    private bool unsavedChangesExist = false;
    private Resolution[] availableResolutions;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        if (uiAudioSource == null) uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.ignoreListenerPause = true;

        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        slotSelectionPanel.SetActive(false);

        // check if any saves exist globally to lock/unlock the main Continue button
        UpdateContinueButtonInteractivity();

        InitializeResolutionOptions();
        LoadAndApplySettings();
        ResetSettingsBackButtonText();

        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiVolumeSlider) uiVolumeSlider.onValueChanged.AddListener(SetUIVolume);
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);

        WireMenuAudio();
    }

    #region Main Navigation Flows

    public void OpenNewGameMenu()
    {
        currentSlotMode = SlotMenuMode.NewGame;
        slotPanelTitle.text = "New Game: Select a Save Slot";
        RefreshSlotUI();
        
        mainPanel.SetActive(false);
        slotSelectionPanel.SetActive(true);
    }

    public void OpenContinueMenu()
    {
        currentSlotMode = SlotMenuMode.Continue;
        slotPanelTitle.text = "Continue: Select your Save";
        RefreshSlotUI();

        mainPanel.SetActive(false);
        slotSelectionPanel.SetActive(true);
    }

    public void CloseSlotMenu()
    {
        slotSelectionPanel.SetActive(false);
        mainPanel.SetActive(true);
        UpdateContinueButtonInteractivity(); // recalculate in case they went back
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    private void UpdateContinueButtonInteractivity()
    {
        // checks if at least one slot contains data
        bool anySaveExists = PlayerPrefs.GetInt("Slot_1_HasData", 0) == 1 ||
                             PlayerPrefs.GetInt("Slot_2_HasData", 0) == 1 ||
                             PlayerPrefs.GetInt("Slot_3_HasData", 0) == 1;

        if (continueButton != null)
        {
            continueButton.interactable = anySaveExists;
        }

        // handles the half-transparency cleanly
        if (continueButtonCanvasGroup != null)
        {
            continueButtonCanvasGroup.alpha = anySaveExists ? 1.0f : 0.5f;
            continueButtonCanvasGroup.blocksRaycasts = anySaveExists; 
        }
    }

    #endregion

    #region Save Slot Logic

    private void RefreshSlotUI()
    {
        for (int i = 0; i < 3; i++)
        {
            int slotNumber = i + 1;
            bool hasData = PlayerPrefs.GetInt($"Slot_{slotNumber}_HasData", 0) == 1;

            if (slotInfoTexts.Length > i && slotInfoTexts[i] != null)
            {
                if (hasData)
                {
                    string lastSaved = PlayerPrefs.GetString($"Slot_{slotNumber}_Timestamp", "Unknown Date");
                    slotInfoTexts[i].text = $"Slot {slotNumber}\n<size=80%>Last Played: {lastSaved}</size>";
                }
                else
                {
                    slotInfoTexts[i].text = $"Slot {slotNumber}\n<color=#888888><size=80%>Empty</size></color>";
                }
            }
        }
    }

    public void SelectSlot(int slotNumber)
    {
        bool hasData = PlayerPrefs.GetInt($"Slot_{slotNumber}_HasData", 0) == 1;

        if (currentSlotMode == SlotMenuMode.NewGame)
        {
            // wipe old files belonging specifically to this chosen slot number
            WipeSlotData(slotNumber);
            
            // write completely fresh validation trackers
            PlayerPrefs.SetInt($"Slot_{slotNumber}_HasData", 1);
            PlayerPrefs.SetString($"Slot_{slotNumber}_Timestamp", DateTime.Now.ToString("g")); // e.g. 6/5/2026 3:30 PM
            
            // inform the gameplay scene exactly which profile index it must load
            PlayerPrefs.SetInt("Global_LastPlayedSlot", slotNumber);
            PlayerPrefs.Save();

            StartCoroutine(DelayedLoadRoutine());
        }
        else if (currentSlotMode == SlotMenuMode.Continue)
        {
            if (!hasData)
            {
                Debug.LogWarning($"Cannot continue. Slot {slotNumber} is empty!");
                return; 
            }

            // update timestamp to make it the most recent save file
            PlayerPrefs.SetString($"Slot_{slotNumber}_Timestamp", DateTime.Now.ToString("g"));
            PlayerPrefs.SetInt("Global_LastPlayedSlot", slotNumber);
            PlayerPrefs.Save();

            StartCoroutine(DelayedLoadRoutine());
        }
    }

    // delete slot function
    public void DeleteSlot(int slotNumber)
    {
        // delete all the data for this slot
        WipeSlotData(slotNumber);

        // update the UI texts so the slot reads "Empty" again
        RefreshSlotUI();

        // immediately check if all slots are now empty. If so, this disables the Continue button
        UpdateContinueButtonInteractivity();
        
        Debug.Log($"Slot {slotNumber} successfully deleted.");
    }

    private void WipeSlotData(int slotNumber)
    {
        // delete slot configuration flags
        PlayerPrefs.DeleteKey($"Slot_{slotNumber}_HasData");
        PlayerPrefs.DeleteKey($"Slot_{slotNumber}_Timestamp");

        // delete economy save tied to this slot
        PlayerPrefs.DeleteKey($"PlayerCredits_Slot{slotNumber}");
        
        // delete json progress maps tied to this slot
        string jsonPath = Application.persistentDataPath + $"/shopProgress_Slot{slotNumber}.json";
        if (System.IO.File.Exists(jsonPath))
        {
            System.IO.File.Delete(jsonPath);
        }
        
        PlayerPrefs.Save();
    }

    private IEnumerator DelayedLoadRoutine()
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    #endregion

    #region Settings Menu Tracking Logic

    public void OpenSettings()
    {
        ResetSettingsBackButtonText();
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettingsAndSave()
    {
        if (unsavedChangesExist)
        {
            PlayerPrefs.SetFloat("Setting_MusicVol", musicVolumeSlider.value);
            PlayerPrefs.SetFloat("Setting_SFXVol", sfxVolumeSlider.value);
            PlayerPrefs.SetFloat("Setting_UIVol", uiVolumeSlider.value);
            PlayerPrefs.SetFloat("Setting_MouseSensitivity", sensitivitySlider.value);
            PlayerPrefs.SetInt("Setting_Resolution", resolutionDropdown.value);
            
            PlayerPrefs.Save();
        }

        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        ResetSettingsBackButtonText();
    }

    private void NotifySettingChanged()
    {
        if (unsavedChangesExist) return;

        unsavedChangesExist = true;
        if (settingsBackButtonText != null) 
        {
            settingsBackButtonText.text = "Confirm & Save";
        }
    }

    private void ResetSettingsBackButtonText()
    {
        unsavedChangesExist = false;
        if (settingsBackButtonText != null) 
        {
            settingsBackButtonText.text = "Back";
        }
    }

    #endregion

    #region Resolution and Volume Handlers

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
        NotifySettingChanged();
    }

    public void SetMusicVolume(float value) { ApplyVolumeToMixer(musicParamName, value); NotifySettingChanged(); }
    public void SetSFXVolume(float value) { ApplyVolumeToMixer(sfxParamName, value); NotifySettingChanged(); }
    public void SetUIVolume(float value) { ApplyVolumeToMixer(uiParamName, value); NotifySettingChanged(); }
    
    public void SetSensitivity(float value)
    {
        NotifySettingChanged();
    }

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (mainMixer == null) return;
        if (sliderValue <= 0.0001f) mainMixer.SetFloat(parameterName, -80f); 
        else mainMixer.SetFloat(parameterName, Mathf.Log10(sliderValue) * 20f);
    }

    private void LoadAndApplySettings()
    {
        float musicVol = PlayerPrefs.GetFloat("Setting_MusicVol", 0.75f);
        float sfxVol = PlayerPrefs.GetFloat("Setting_SFXVol", 0.75f);
        float uiVol = PlayerPrefs.GetFloat("Setting_UIVol", 0.75f);
        float sensitivity = PlayerPrefs.GetFloat("Setting_MouseSensitivity", 2.0f);

        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (uiVolumeSlider != null) uiVolumeSlider.value = uiVol;
        if (sensitivitySlider != null) sensitivitySlider.value = sensitivity;

        ApplyVolumeToMixer(musicParamName, musicVol);
        ApplyVolumeToMixer(sfxParamName, sfxVol);
        ApplyVolumeToMixer(uiParamName, uiVol);
    }

    #endregion

    #region Self-Contained Audio Handlers

    private void WireMenuAudio()
    {
        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener((val) => PlaySliderTick());
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener((val) => PlaySliderTick());
        if (uiVolumeSlider) uiVolumeSlider.onValueChanged.AddListener((val) => PlaySliderTick()); 
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener((val) => PlaySliderTick());
        
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener((val) => PlayClickSound());

        Button[] menuButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in menuButtons)
        {
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    private void PlayClickSound() 
    { 
        if (uiAudioSource && clickSound) uiAudioSource.PlayOneShot(clickSound); 
    }

    private void PlaySliderTick()
    {
        if (Time.unscaledTime - lastSliderSoundTime >= sliderSoundCooldown)
        {
            if (uiAudioSource != null && sliderTickSound != null)
            {
                uiAudioSource.PlayOneShot(sliderTickSound);
                lastSliderSoundTime = Time.unscaledTime;
            }
        }
    }

    #endregion
}