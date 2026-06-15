using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    private enum SlotMenuMode { NewGame, Continue }
    private SlotMenuMode currentSlotMode;

    [Header("Scene Routing")]
    public string gameSceneName  = "ArcadeRoom";
    public float  sceneLoadDelay = 1.0f;

    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject slotSelectionPanel;

    [Header("Main Menu Buttons")]
    public Button       continueButton;
    public CanvasGroup  continueButtonCanvasGroup;

    [Header("Slot Selection UI")]
    public TextMeshProUGUI[] slotInfoTexts;
    public GameObject[]      slotFolderButtons;
    public TextMeshProUGUI   slotPanelTitle;

    [Header("Save Management")]
    [Tooltip("Add the exact names of your arcade games here so their unlocks wipe correctly.")]
    public string[] knownArcadeGames = { "Space Invaders" };

    [Header("Settings Controls")]
    public TMP_Dropdown    resolutionDropdown;
    public Slider          musicVolumeSlider;
    public Slider          sfxVolumeSlider;
    public Slider          uiVolumeSlider;
    public Slider          sensitivitySlider;
    public TextMeshProUGUI settingsBackButtonText;

    // AudioMixer field removed — SettingsManager owns the mixer now.
    // Remove the old mainMixer assignment from the Inspector too.

    [Header("Audio Feedback - Menu Sounds")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip   clickSound;
    [SerializeField] private AudioClip   sliderTickSound;
    [SerializeField] private float       sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;

    private bool unsavedChangesExist = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        if (uiAudioSource == null) uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake        = false;
        uiAudioSource.ignoreListenerPause = true;

        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        slotSelectionPanel.SetActive(false);

        UpdateContinueButtonInteractivity();

        // SettingsManager.Start() already called ApplySavedResolution and ApplySavedAudio.
        // We just populate the dropdown and sync sliders to what it loaded.
        if (resolutionDropdown != null && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.PopulateDropdown(resolutionDropdown);
            // Add dirty flag on top; ApplyResolution is already wired by PopulateDropdown
            resolutionDropdown.onValueChanged.AddListener(_ => NotifySettingChanged());
        }

        LoadSliders();
        ResetSettingsBackButtonText();

        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider)   sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiVolumeSlider)    uiVolumeSlider.onValueChanged.AddListener(SetUIVolume);
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        WireMenuAudio();
    }

    // ── Main Navigation ───────────────────────────────────────────────────────

    public void OpenNewGameMenu()
    {
        currentSlotMode    = SlotMenuMode.NewGame;
        slotPanelTitle.text = "New Game: Select a Save Slot";
        RefreshSlotUI();
        mainPanel.SetActive(false);
        slotSelectionPanel.SetActive(true);
    }

    public void OpenContinueMenu()
    {
        currentSlotMode    = SlotMenuMode.Continue;
        slotPanelTitle.text = "Continue: Select your Save";
        RefreshSlotUI();
        mainPanel.SetActive(false);
        slotSelectionPanel.SetActive(true);
    }

    public void CloseSlotMenu()
    {
        slotSelectionPanel.SetActive(false);
        mainPanel.SetActive(true);
        UpdateContinueButtonInteractivity();
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    private void UpdateContinueButtonInteractivity()
    {
        bool anySaveExists = PlayerPrefs.GetInt("Slot_1_HasData", 0) == 1 ||
                             PlayerPrefs.GetInt("Slot_2_HasData", 0) == 1 ||
                             PlayerPrefs.GetInt("Slot_3_HasData", 0) == 1;

        if (continueButton != null)
            continueButton.interactable = anySaveExists;

        if (continueButtonCanvasGroup != null)
        {
            continueButtonCanvasGroup.alpha          = anySaveExists ? 1.0f : 0.5f;
            continueButtonCanvasGroup.blocksRaycasts = anySaveExists;
        }
    }

    // ── Save Slot Logic ───────────────────────────────────────────────────────

    private void RefreshSlotUI()
    {
        for (int i = 0; i < 3; i++)
        {
            int  slotNumber = i + 1;
            bool hasData    = PlayerPrefs.GetInt($"Slot_{slotNumber}_HasData", 0) == 1;

            if (slotInfoTexts.Length > i && slotInfoTexts[i] != null)
            {
                slotInfoTexts[i].text = hasData
                    ? $"Slot {slotNumber}\n<size=80%>Last Played: {PlayerPrefs.GetString($"Slot_{slotNumber}_Timestamp", "Unknown Date")}</size>"
                    : $"Slot {slotNumber}\n<color=#888888><size=80%>Empty</size></color>";
            }

            if (slotFolderButtons.Length > i && slotFolderButtons[i] != null)
                slotFolderButtons[i].SetActive(hasData);
        }
    }

    public void SelectSlot(int slotNumber)
    {
        bool hasData = PlayerPrefs.GetInt($"Slot_{slotNumber}_HasData", 0) == 1;

        if (currentSlotMode == SlotMenuMode.NewGame)
        {
            WipeSlotData(slotNumber);
            PlayerPrefs.SetInt($"Slot_{slotNumber}_HasData", 1);
            PlayerPrefs.SetString($"Slot_{slotNumber}_Timestamp", DateTime.Now.ToString("g"));
            PlayerPrefs.SetInt("Global_LastPlayedSlot", slotNumber);
            PlayerPrefs.Save();
            StartCoroutine(DelayedLoadRoutine());
        }
        else if (currentSlotMode == SlotMenuMode.Continue)
        {
            if (!hasData) { Debug.LogWarning($"Cannot continue. Slot {slotNumber} is empty!"); return; }
            PlayerPrefs.SetString($"Slot_{slotNumber}_Timestamp", DateTime.Now.ToString("g"));
            PlayerPrefs.SetInt("Global_LastPlayedSlot", slotNumber);
            PlayerPrefs.Save();
            StartCoroutine(DelayedLoadRoutine());
        }
    }

    public void DeleteSlot(int slotNumber)
    {
        WipeSlotData(slotNumber);
        RefreshSlotUI();
        UpdateContinueButtonInteractivity();
        Debug.Log($"Slot {slotNumber} successfully deleted.");
    }

    private void WipeSlotData(int slotNumber)
    {
        PlayerPrefs.DeleteKey($"Slot_{slotNumber}_HasData");
        PlayerPrefs.DeleteKey($"Slot_{slotNumber}_Timestamp");
        PlayerPrefs.DeleteKey($"PlayerCredits_Slot{slotNumber}");

        // 1 - Wipe the master arcade unlock string used by ArcadeMachine.cs
        PlayerPrefs.DeleteKey($"ArcadeUnlocks_Slot{slotNumber}");

        // 2 - Legacy per-game unlock keys (kept for cleanup of old saves)
        foreach (string game in knownArcadeGames)
            PlayerPrefs.DeleteKey($"Unlock_{game.Replace(" ", "")}_Slot{slotNumber}");

        string jsonPath = Application.persistentDataPath + $"/shopProgress_Slot{slotNumber}.json";
        if (System.IO.File.Exists(jsonPath)) System.IO.File.Delete(jsonPath);

        PlayerPrefs.Save();
    }

    public void OpenSaveDirectory(int slotNumber)
    {
        string path = Application.persistentDataPath;
        Application.OpenURL("file://" + path);
        Debug.Log($"Opening save folder for Slot {slotNumber}. Path: {path}");
    }

    private IEnumerator DelayedLoadRoutine()
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    // ── Settings Panel Navigation ─────────────────────────────────────────────

    public void OpenSettings()
    {
        ResetSettingsBackButtonText();
        LoadSliders(); // re-sync in case another scene changed saved values
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettingsAndSave()
    {
        if (unsavedChangesExist)
        {
            // Sensitivity is not in SettingsManager, save it separately
            if (sensitivitySlider != null)
                PlayerPrefs.SetFloat("Setting_MouseSensitivity", sensitivitySlider.value);

            SettingsManager.Instance?.SaveAll(); // persists music/sfx/ui vol + resolution
            unsavedChangesExist = false;
        }

        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        ResetSettingsBackButtonText();
    }

    private void NotifySettingChanged()
    {
        if (unsavedChangesExist) return;
        unsavedChangesExist = true;
        if (settingsBackButtonText != null) settingsBackButtonText.text = "Confirm & Save";
    }

    private void ResetSettingsBackButtonText()
    {
        unsavedChangesExist = false;
        if (settingsBackButtonText != null) settingsBackButtonText.text = "Back";
    }

    // ── Settings Handlers ─────────────────────────────────────────────────────

    private void LoadSliders()
    {
        if (!SettingsManager.Instance) return;

        // Use SetValueWithoutNotify so loading saved values doesn't fire the
        // dirty-flag listeners or play slider tick sounds
        if (musicVolumeSlider) musicVolumeSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedMusicVol);
        if (sfxVolumeSlider)   sfxVolumeSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedSFXVol);
        if (uiVolumeSlider)    uiVolumeSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedUIVol);

        float sensitivity = PlayerPrefs.GetFloat("Setting_MouseSensitivity", 2.0f);
        if (sensitivitySlider) sensitivitySlider.SetValueWithoutNotify(sensitivity);

        // Push saved values to the mixer immediately so audio is correct on open
        SettingsManager.Instance.ApplySavedAudio();
    }

    // These are kept as named methods so they can still be wired in the Inspector if needed
    public void SetResolution(int index)     { SettingsManager.Instance?.ApplyResolution(index);  NotifySettingChanged(); }
    public void SetMusicVolume(float value)  { SettingsManager.Instance?.SetMusicVolume(value);   NotifySettingChanged(); }
    public void SetSFXVolume(float value)    { SettingsManager.Instance?.SetSFXVolume(value);     NotifySettingChanged(); }
    public void SetUIVolume(float value)     { SettingsManager.Instance?.SetUIVolume(value);      NotifySettingChanged(); }
    public void SetSensitivity(float value)  { PlayerPrefs.SetFloat("Setting_MouseSensitivity", value); NotifySettingChanged(); }

    // ── Audio ─────────────────────────────────────────────────────────────────

    private void WireMenuAudio()
    {
        if (musicVolumeSlider)  musicVolumeSlider.onValueChanged.AddListener(_  => PlaySliderTick());
        if (sfxVolumeSlider)    sfxVolumeSlider.onValueChanged.AddListener(_    => PlaySliderTick());
        if (uiVolumeSlider)     uiVolumeSlider.onValueChanged.AddListener(_     => PlaySliderTick());
        if (sensitivitySlider)  sensitivitySlider.onValueChanged.AddListener(_  => PlaySliderTick());
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(_ => PlayClickSound());

        Button[] menuButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in menuButtons) btn.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (uiAudioSource && clickSound) uiAudioSource.PlayOneShot(clickSound);
    }

    private void PlaySliderTick()
    {
        if (Time.unscaledTime - lastSliderSoundTime >= sliderSoundCooldown)
        {
            if (uiAudioSource && sliderTickSound)
            {
                uiAudioSource.PlayOneShot(sliderTickSound);
                lastSliderSoundTime = Time.unscaledTime;
            }
        }
    }
}