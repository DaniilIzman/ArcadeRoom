using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using TMPro;

// controls all main menu behaviour: panel navigation, save slot management, settings, and ui audio
public class MainMenuController : MonoBehaviour
{
    // tracks whether the slot panel was opened for a new game or a continue
    private enum SlotMenuMode { NewGame, Continue }
    private SlotMenuMode _currentSlotMode;

    // name of the scene to load when a slot is selected and the game begins
    [Header("Scene Routing")]
    public string gameSceneName  = "ArcadeRoom";

    // references to the three top-level ui panels
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject slotSelectionPanel;

    // fullscreen image overlay used to simulate brightness by darkening the screen
    public Image      brightnessOverlay;

    // the continue button and its canvas group, used to grey it out when no saves exist
    [Header("Main Menu Buttons")]
    public Button      continueButton;
    public CanvasGroup continueButtonCanvasGroup;

    // per-slot text labels, folder buttons, and the title shown above the slot list
    [Header("Slot Selection UI")]
    public TextMeshProUGUI[] slotInfoTexts;
    public GameObject[]      slotFolderButtons;
    public TextMeshProUGUI   slotPanelTitle;

    // list of all game names whose unlock keys should be wiped when a slot is deleted
    [Header("Save Management")]
    public string[] knownArcadeGames = { "Space Invaders", "Flappy Bird", "Endless Runner" };

    // optional icon shown on a slot when the player has completed the game in that slot
    [Tooltip("Optional: one icon GameObject per slot, shown when that slot has finished the game.")]
    public GameObject[] slotCompletedIcons;

    // references to all settings ui controls wired up in start
    [Header("Settings Controls")]
    public TMP_Dropdown    resolutionDropdown;
    public Slider          brightnessSlider;
    public Slider          musicSlider;
    public Slider          sfxSlider;
    public Slider          uiSlider;
    public Slider          sensitivitySlider;

    // text label on the settings back button, changes to "confirm & save" when there are unsaved changes
    public TextMeshProUGUI settingsBackButtonText;

    // audiosource and clips used for button click and slider tick sounds in the menu
    [Header("Audio Feedback")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip   clickSound;
    [SerializeField] private AudioClip   sliderTickSound;

    // minimum time between successive slider tick sounds to avoid rapid-fire audio spam
    [SerializeField] private float       sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;

    // tracks whether the player has changed a setting without saving it yet
    private bool _unsavedChangesExist = false;

    private void Start()
    {
        // ensure the cursor is visible and unlocked in the main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        // create an audiosource if none was assigned in the inspector
        if (uiAudioSource == null) uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake         = false;

        // keep ui sounds playing even if the listener is paused
        uiAudioSource.ignoreListenerPause = true;
        SettingsManager.Instance?.Route(uiAudioSource, SettingsManager.AudioCategory.UI);

        // show only the main panel on startup
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        slotSelectionPanel.SetActive(false);

        UpdateContinueButtonInteractivity();

        // populate and wire the resolution dropdown if both references are available
        if (resolutionDropdown != null && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.PopulateDropdown(resolutionDropdown);
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
            resolutionDropdown.onValueChanged.AddListener(_ => NotifySettingChanged());
        }

        // wire each settings slider to its corresponding handler
        if (musicSlider)       musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider)         sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiSlider)          uiSlider.onValueChanged.AddListener(SetUIVolume);
        if (brightnessSlider)  brightnessSlider.onValueChanged.AddListener(SetBrightness);
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        // subscribe to the brightness event so the overlay updates whenever brightness changes
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged += UpdateBrightnessOverlay;

        LoadSliders();
        ResetSettingsBackButtonText();
        WireMenuAudio();
    }

    // unsubscribe from the brightness event when this object is destroyed to prevent stale references
    private void OnDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged -= UpdateBrightnessOverlay;
    }

    // ── navigation ──

    // opens the slot selection panel in new game mode
    public void OpenNewGameMenu()
    {
        _currentSlotMode    = SlotMenuMode.NewGame;
        slotPanelTitle.text = "New Game: Select a Save Slot";
        RefreshSlotUI();
        mainPanel.SetActive(false);
        slotSelectionPanel.SetActive(true);
    }

    // opens the slot selection panel in continue mode
    public void OpenContinueMenu()
    {
        _currentSlotMode    = SlotMenuMode.Continue;
        slotPanelTitle.text = "Continue: Select your Save";
        RefreshSlotUI();
        mainPanel.SetActive(false);
        slotSelectionPanel.SetActive(true);
    }

    // dismisses the slot panel and returns to the main panel
    public void CloseSlotMenu()
    {
        slotSelectionPanel.SetActive(false);
        mainPanel.SetActive(true);
        UpdateContinueButtonInteractivity();
    }

    public void QuitGame() { Application.Quit(); }

    // enables the continue button only if at least one slot contains save data
    private void UpdateContinueButtonInteractivity()
    {
        bool any = PlayerPrefs.GetInt("Slot_1_HasData", 0) == 1 ||
                   PlayerPrefs.GetInt("Slot_2_HasData", 0) == 1 ||
                   PlayerPrefs.GetInt("Slot_3_HasData", 0) == 1;

        if (continueButton != null) continueButton.interactable = any;
        if (continueButtonCanvasGroup != null)
        {
            // visually dim the button and block its raycasts when no saves exist
            continueButtonCanvasGroup.alpha          = any ? 1f : 0.5f;
            continueButtonCanvasGroup.blocksRaycasts = any;
        }
    }

    // ── save slots ──

    // updates each slot's label, folder button visibility, and completed icon based on saved data
    private void RefreshSlotUI()
    {
        for (int i = 0; i < 3; i++)
        {
            int  slot    = i + 1;
            bool hasData = PlayerPrefs.GetInt($"Slot_{slot}_HasData", 0) == 1;

            // show the last-played timestamp for occupied slots or an "empty" label otherwise
            if (slotInfoTexts.Length > i && slotInfoTexts[i] != null)
                slotInfoTexts[i].text = hasData
                    ? $"Slot {slot}\n<size=80%>Last Played: {PlayerPrefs.GetString($"Slot_{slot}_Timestamp", "Unknown Date")}</size>"
                    : $"Slot {slot}\n<color=#888888><size=80%>Empty</size></color>";

            // only show the folder open button for slots that have data
            if (slotFolderButtons.Length > i && slotFolderButtons[i] != null)
                slotFolderButtons[i].SetActive(hasData);

            // show the completion icon if the player finished the game in this slot
            if (slotCompletedIcons != null && slotCompletedIcons.Length > i && slotCompletedIcons[i] != null)
                slotCompletedIcons[i].SetActive(PlayerPrefs.GetInt($"Slot_{slot}_Completed", 0) == 1);
        }
    }

    // handles a slot being chosen; behaviour differs depending on whether it's a new game or continue
    public void SelectSlot(int slot)
    {
        bool hasData = PlayerPrefs.GetInt($"Slot_{slot}_HasData", 0) == 1;

        if (_currentSlotMode == SlotMenuMode.NewGame)
        {
            // wipe any existing data, then initialise the slot and launch the game
            WipeSlotData(slot);
            PlayerPrefs.SetInt($"Slot_{slot}_HasData", 1);
            PlayerPrefs.SetString($"Slot_{slot}_Timestamp", DateTime.Now.ToString("g"));
            PlayerPrefs.SetInt("Global_LastPlayedSlot", slot);
            PlayerPrefs.Save();
            LoadGameScene();
        }
        else if (_currentSlotMode == SlotMenuMode.Continue)
        {
            // do nothing if the chosen slot has no save data
            if (!hasData) return;
            PlayerPrefs.SetString($"Slot_{slot}_Timestamp", DateTime.Now.ToString("g"));
            PlayerPrefs.SetInt("Global_LastPlayedSlot", slot);
            PlayerPrefs.Save();
            LoadGameScene();
        }
    }

    // clears a slot's data and refreshes the ui to reflect the change
    public void DeleteSlot(int slot) { WipeSlotData(slot); RefreshSlotUI(); UpdateContinueButtonInteractivity(); }

    // removes all playerprefs keys and the save file associated with the given slot
    private void WipeSlotData(int slot)
    {
        PlayerPrefs.DeleteKey($"Slot_{slot}_HasData");
        PlayerPrefs.DeleteKey($"Slot_{slot}_Timestamp");
        PlayerPrefs.DeleteKey($"Slot_{slot}_Completed");
        PlayerPrefs.DeleteKey($"PlayerCredits_Slot{slot}");
        PlayerPrefs.DeleteKey($"ArcadeUnlocks_Slot{slot}");

        // delete individual game unlock keys for every known arcade game
        foreach (string game in knownArcadeGames)
            PlayerPrefs.DeleteKey($"Unlock_{game.Replace(" ", "")}_Slot{slot}");

        // also delete the json save file from persistent storage if it exists
        string path = Application.persistentDataPath + $"/shopProgress_Slot{slot}.json";
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        PlayerPrefs.Save();
    }

    // opens the persistent data folder in the system file explorer
    public void OpenSaveDirectory(int slot) => Application.OpenURL("file://" + Application.persistentDataPath);

    // loads the game scene using the scene fader if available, otherwise loads directly
    private void LoadGameScene()
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(gameSceneName);
        else SceneManager.LoadScene(gameSceneName);
    }

    // ── settings panel ────────────────────────────────────────────────────────

    // opens the settings panel and syncs all controls to the currently saved values
    public void OpenSettings()
    {
        ResetSettingsBackButtonText();

        if (resolutionDropdown != null && SettingsManager.Instance != null)
            SettingsManager.Instance.SyncDropdown(resolutionDropdown);

        LoadSliders();
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // saves any pending changes then closes the settings panel and returns to the main menu
    public void CloseSettingsAndSave()
    {
        if (_unsavedChangesExist)
        {
            SettingsManager.Instance?.SaveAll();
            _unsavedChangesExist = false;
        }
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        ResetSettingsBackButtonText();
    }

    // marks that an unsaved change exists and updates the back button label to prompt the player to save
    private void NotifySettingChanged()
    {
        if (_unsavedChangesExist) return;
        _unsavedChangesExist = true;
        if (settingsBackButtonText != null) settingsBackButtonText.text = "Confirm & Save";
    }

    // resets the back button label to "back" and clears the unsaved changes flag
    private void ResetSettingsBackButtonText()
    {
        _unsavedChangesExist = false;
        if (settingsBackButtonText != null) settingsBackButtonText.text = "Back";
    }

    // ── settings handlers ─────────────────────────────────────────────────────

    // reads current values from settingsmanager and updates all sliders without triggering their events
    private void LoadSliders()
    {
        if (!SettingsManager.Instance) return;

        if (musicSlider)      musicSlider.SetValueWithoutNotify(SettingsManager.Instance.MusicVolume);
        if (sfxSlider)        sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.SFXVolume);
        if (uiSlider)         uiSlider.SetValueWithoutNotify(SettingsManager.Instance.UIVolume);
        if (brightnessSlider)
        {
            brightnessSlider.SetValueWithoutNotify(SettingsManager.Instance.Brightness);
            UpdateBrightnessOverlay(SettingsManager.Instance.Brightness);
        }
        if (sensitivitySlider) sensitivitySlider.SetValueWithoutNotify(SettingsManager.Instance.Sensitivity);

        // re-apply all audio volumes to the mixer after loading
        SettingsManager.Instance.ApplyAllAudio();
    }

    // each method passes the new value to settingsmanager and flags the change as unsaved
    public void SetResolution(int index)   { SettingsManager.Instance?.ApplyResolution(index); NotifySettingChanged(); }
    public void SetMusicVolume(float v)    { SettingsManager.Instance?.SetMusicVolume(v);      NotifySettingChanged(); }
    public void SetSFXVolume(float v)      { SettingsManager.Instance?.SetSFXVolume(v);        NotifySettingChanged(); }
    public void SetUIVolume(float v)       { SettingsManager.Instance?.SetUIVolume(v);         NotifySettingChanged(); }
    public void SetSensitivity(float v)    { SettingsManager.Instance?.SetSensitivity(v);      NotifySettingChanged(); }

    public void SetBrightness(float v)
    {
        // setting brightness fires the onbrightnesschanged event which calls updatebrightnessoverlay
        SettingsManager.Instance?.SetBrightness(v);
        NotifySettingChanged();
    }

    // adjusts the alpha of the black overlay image to simulate a brightness change
    private void UpdateBrightnessOverlay(float value)
    {
        if (brightnessOverlay == null) return;

        // at brightness 0 the overlay is nearly opaque; at brightness 1 it is fully transparent
        float alpha = Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(value));
        brightnessOverlay.color = new Color(0f, 0f, 0f, alpha);
    }

    // ── audio ─────────────────────────────────────────────────────────────────

    // attaches sound playback callbacks to all sliders, the dropdown, and every button in the menu
    private void WireMenuAudio()
    {
        if (musicSlider)       musicSlider.onValueChanged.AddListener(_       => PlaySliderTick());
        if (sfxSlider)         sfxSlider.onValueChanged.AddListener(_         => PlaySliderTick());
        if (uiSlider)          uiSlider.onValueChanged.AddListener(_          => PlaySliderTick());
        if (brightnessSlider)  brightnessSlider.onValueChanged.AddListener(_  => PlaySliderTick());
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(_ => PlaySliderTick());
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(_ => PlayClickSound());

        // wire every button found in the hierarchy, including inactive ones
        foreach (var btn in GetComponentsInChildren<Button>(true))
            btn.onClick.AddListener(PlayClickSound);
    }

    // plays the click sound as a one-shot so overlapping clicks don't cut each other off
    public void PlayClickSound() { if (uiAudioSource && clickSound) uiAudioSource.PlayOneShot(clickSound); }

    // plays the slider tick sound, rate-limited by the cooldown to prevent audio spam
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