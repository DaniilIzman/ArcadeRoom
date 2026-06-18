using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// controls the endless runner's pre-game menu; handles navigation between the main panel
// and settings panel, credit display, settings wiring, and audio feedback
public class EndlessRunnerMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;      // the root panel containing play, settings, and leave buttons
    public GameObject settingsPanel;  // the settings sub-panel shown when the player opens settings
    public Image brightnessOverlay;   // black overlay whose alpha represents the current brightness level

    [Header("Economy Settings")]
    public TextMeshProUGUI creditsText;    // displays the player's current credit balance
    public string baseCreditsKey = "PlayerCredits"; // playerprefs key prefix shared with the arcade room

    [Header("Settings Controls")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown resolutionDropdown;

    [Header("Audio Feedback - Local UI")]
    public AudioSource uiAudioSource;  // plays button click and slider tick sounds
    public AudioClip clickSound;
    public AudioClip sliderTickSound;

    [Header("Scene Routing")]
    public string gameSceneName = "EndlessRunnerLevel"; // scene loaded when the player hits play
    public string arcadeRoomSceneName = "ArcadeRoom";   // scene loaded when the player leaves the machine

    // playerprefs key that stores which save slot is currently active
    private const string prefSlot = "Global_LastPlayedSlot";

    // throttle the slider tick sound so it doesn't fire on every tiny slider movement
    private float nextSliderSoundTime = 0f;

    private int activeSlot;         // the save slot number read from playerprefs on start
    private int currentCredits;     // the player's credit balance for the active slot
    private string creditsPrefsKey; // full playerprefs key for credits on the active slot

    private void Start()
    {
        // show the cursor since this is a menu scene
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // build the credits key for the active slot so the right balance is displayed
        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        // start on the main panel with the settings panel hidden
        TogglePanels(true, false);

        SetupAudioSource();
        BindSettingsUI();
        RefreshCreditsUI();

        // subscribe so the overlay updates whenever brightness is changed
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged += UpdateBrightnessOverlay;
    }

    private void OnDestroy()
    {
        // unsubscribe to avoid a dangling delegate after the scene unloads
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged -= UpdateBrightnessOverlay;
    }

    private void Update()
    {
        // pressing escape while on the settings panel acts as a back button
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf) ReturnToMainMenu();
        }
    }

    // reads the credit balance from playerprefs and updates the credits label
    private void RefreshCreditsUI()
    {
        currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0);
        if (creditsText != null) creditsText.text = $"CREDITS: {currentCredits}";
    }

    // ── navigation ───────────────────────────────────────────────────────────

    // plays the click sound and loads the game scene
    public void AttemptStartGame() { PlayClickSound(); LoadScene(gameSceneName); }

    // opens the settings panel, syncing the dropdown and sliders to current values first
    public void OpenSettings()
    {
        PlayClickSound();
        SettingsManager.Instance?.SyncDropdown(resolutionDropdown);
        RefreshSlidersFromSettings();
        TogglePanels(false, true);
    }

    // saves settings and returns to the main panel from the settings panel
    public void ReturnToMainMenu()
    {
        PlayClickSound();
        SettingsManager.Instance?.SaveAll();
        TogglePanels(true, false);
    }

    // saves settings and loads the arcade room scene when the player leaves the machine
    public void LeaveArcadeMachine()
    {
        PlayClickSound();
        SettingsManager.Instance?.SaveAll();
        LoadScene(arcadeRoomSceneName);
    }

    // shows or hides the main and settings panels according to the provided flags
    private void TogglePanels(bool main, bool settings)
    {
        if (mainPanel) mainPanel.SetActive(main);
        if (settingsPanel) settingsPanel.SetActive(settings);
    }

    // loads a scene by name, routing through scenefader if one is present in the scene
    private void LoadScene(string scene)
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(scene);
        else SceneManager.LoadScene(scene);
    }

    // ── settings: delegate to settingsmanager ────────────────────────────────

    // wires up the resolution dropdown and all volume/brightness sliders to the settings manager
    private void BindSettingsUI()
    {
        var sm = SettingsManager.Instance;

        if (sm != null && resolutionDropdown != null)
        {
            sm.PopulateDropdown(resolutionDropdown);
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(i => { sm.ApplyResolution(i); PlayClickSound(); });
        }

        WireSlider(musicSlider,      v => SettingsManager.Instance?.SetMusicVolume(v));
        WireSlider(sfxSlider,        v => SettingsManager.Instance?.SetSFXVolume(v));
        WireSlider(uiSlider,         v => SettingsManager.Instance?.SetUIVolume(v));
        WireSlider(brightnessSlider, v => SettingsManager.Instance?.SetBrightness(v));

        RefreshSlidersFromSettings();

        // attach the click sound to every button in the hierarchy so none are missed
        foreach (Button btn in GetComponentsInChildren<Button>(true))
            btn.onClick.AddListener(PlayClickSound);
    }

    // attaches the settings callback and the tick sound to a slider, clearing old listeners first
    private void WireSlider(Slider s, UnityEngine.Events.UnityAction<float> onChange)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.onValueChanged.AddListener(onChange);
        s.onValueChanged.AddListener(_ => PlaySliderTickSound());
    }

    // pulls the current values from the settings manager and sets all sliders silently
    private void RefreshSlidersFromSettings()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;
        if (musicSlider)      musicSlider.SetValueWithoutNotify(sm.MusicVolume);
        if (sfxSlider)        sfxSlider.SetValueWithoutNotify(sm.SFXVolume);
        if (uiSlider)         uiSlider.SetValueWithoutNotify(sm.UIVolume);
        if (brightnessSlider) brightnessSlider.SetValueWithoutNotify(sm.Brightness);
        UpdateBrightnessOverlay(sm.Brightness);
        sm.ApplyAllAudio();
    }

    // adjusts the black overlay's alpha so that 0 brightness = nearly opaque, 1 = fully transparent
    private void UpdateBrightnessOverlay(float value)
    {
        if (brightnessOverlay == null) return;
        float alpha = Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(value));
        brightnessOverlay.color = new Color(0f, 0f, 0f, alpha);
    }

    // public pass-throughs so unity ui buttons can call these directly from the inspector
    public void SetMusicVolume(float val) => SettingsManager.Instance?.SetMusicVolume(val);
    public void SetSFXVolume(float val)   => SettingsManager.Instance?.SetSFXVolume(val);
    public void SetUIVolume(float val)    => SettingsManager.Instance?.SetUIVolume(val);
    public void SetBrightness(float val)  => SettingsManager.Instance?.SetBrightness(val);
    public void SetResolution(int index)  => SettingsManager.Instance?.ApplyResolution(index);

    // ── local ui audio ────────────────────────────────────────────────────────

    // ensures the ui audio source exists and registers it with the settings manager
    private void SetupAudioSource()
    {
        if (uiAudioSource == null)
        {
            // add an audio source component at runtime if none was assigned in the inspector
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;

            // ignore the listener pause so ui sounds still play when time is frozen
            uiAudioSource.ignoreListenerPause = true;
        }
        SettingsManager.Instance?.Route(uiAudioSource, SettingsManager.AudioCategory.UI);
    }

    // plays the click sound as a one-shot through the ui audio source
    public void PlayClickSound()
    {
        if (uiAudioSource != null && clickSound != null) uiAudioSource.PlayOneShot(clickSound);
    }

    // plays the slider tick sound at most once per 60ms to avoid rapid-fire audio spam
    private void PlaySliderTickSound()
    {
        if (Time.unscaledTime >= nextSliderSoundTime && uiAudioSource != null && sliderTickSound != null)
        {
            uiAudioSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f;
        }
    }
}