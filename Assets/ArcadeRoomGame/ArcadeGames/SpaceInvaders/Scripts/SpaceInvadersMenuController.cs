using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// controls the space invaders menu scene: panel navigation, settings, and credits display
public class SpaceInvadersMenuController : MonoBehaviour
{
    // the three panels toggled between main, settings, and the score advance table
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject scoreAdvancePanel;

    // fullscreen overlay image used to simulate brightness by darkening the screen
    public Image brightnessOverlay;

    // reads and displays the credit balance for the active save slot
    [Header("Economy Settings")]
    public TextMeshProUGUI creditsText;
    public string baseCreditsKey = "PlayerCredits";

    // when true, overwrites the slot's credits with 500 on start; for testing only
    public bool debugGiveFreeCredits = false;

    // settings controls wired to settingsmanager in bindSettingsUI
    [Header("Settings Controls")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown resolutionDropdown;

    // scene names loaded when starting a game or returning to the arcade room
    [Header("Scene Routing")]
    public string gameSceneName = "SpaceInvadersLevel";
    public string arcadeRoomSceneName = "ArcadeRoom";

    // audiosource and clips for button and slider sounds local to this menu
    [Header("Audio Feedback - Local UI")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;
    public AudioClip errorSound;
    public AudioClip sliderTickSound;

    // playerprefs key used to identify the active save slot
    private const string prefSlot = "Global_LastPlayedSlot";

    // minimum time between successive slider tick sounds
    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;

    private int activeSlot;
    private int currentCredits;

    // full playerprefs key for the active slot's credit balance
    private string creditsPrefsKey;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        // inject free credits for testing without affecting real save data flow
        if (debugGiveFreeCredits)
        {
            PlayerPrefs.SetInt(creditsPrefsKey, 500);
            PlayerPrefs.Save();
        }

        // show only the main panel on startup
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        scoreAdvancePanel.SetActive(false);

        SetupAudioSource();
        BindSettingsUI();
        RefreshCreditsUI();

        // subscribe to brightness changes so the overlay stays in sync
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged += UpdateBrightnessOverlay;
    }

    private void OnDestroy()
    {
        // unsubscribe to avoid stale references after this object is destroyed
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged -= UpdateBrightnessOverlay;
    }

    private void Update()
    {
        // pressing escape from any sub-panel returns to the main panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf || scoreAdvancePanel.activeSelf) ReturnToMainMenu();
        }
    }

    // reads and displays the current credit balance from playerprefs
    private void RefreshCreditsUI()
    {
        currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0);
        if (creditsText != null) creditsText.text = $"CREDITS: {currentCredits}";
    }

    // loads the game scene immediately
    public void AttemptStartGame() { PlayClickSound(); LoadScene(gameSceneName); }

    // opens the score advance reference table panel
    public void OpenScoreAdvanceTable() { PlayClickSound(); TogglePanels(false, false, true); }

    // opens the settings panel and syncs all controls to current saved values
    public void OpenSettings()
    {
        PlayClickSound();
        SettingsManager.Instance?.SyncDropdown(resolutionDropdown);
        RefreshSlidersFromSettings();
        TogglePanels(false, true, false);
    }

    // saves settings and returns to the main panel
    public void ReturnToMainMenu()
    {
        PlayClickSound();
        SettingsManager.Instance?.SaveAll();
        TogglePanels(true, false, false);
    }

    // saves settings and loads the arcade room scene to exit the mini-game
    public void LeaveArcadeMachine()
    {
        PlayClickSound();
        SettingsManager.Instance?.SaveAll();
        LoadScene(arcadeRoomSceneName);
    }

    // sets the active state of all three panels in a single call
    private void TogglePanels(bool main, bool settings, bool scoreAdvance)
    {
        if (mainPanel) mainPanel.SetActive(main);
        if (settingsPanel) settingsPanel.SetActive(settings);
        if (scoreAdvancePanel) scoreAdvancePanel.SetActive(scoreAdvance);
    }

    // loads a scene by name via the scene fader if available, otherwise loads directly
    private void LoadScene(string scene)
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(scene);
        else SceneManager.LoadScene(scene);
    }

    // populates all settings controls and wires their change listeners to settingsmanager
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

        // wire click sounds to every button in this gameobject's hierarchy
        foreach (Button btn in GetComponentsInChildren<Button>(true))
            btn.onClick.AddListener(PlayClickSound);
    }

    // clears and re-wires a single slider with a value change handler and a tick sound
    private void WireSlider(Slider s, UnityEngine.Events.UnityAction<float> onChange)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.onValueChanged.AddListener(onChange);
        s.onValueChanged.AddListener(_ => PlaySliderTick());
    }

    // reads current values from settingsmanager and updates all sliders without triggering their events
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

    // adjusts the alpha of the black overlay image to simulate a brightness change
    private void UpdateBrightnessOverlay(float value)
    {
        if (brightnessOverlay == null) return;
        float alpha = Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(value));
        brightnessOverlay.color = new Color(0f, 0f, 0f, alpha);
    }

    // public pass-through methods kept for inspector button wiring compatibility
    public void SetMusicVolume(float val) => SettingsManager.Instance?.SetMusicVolume(val);
    public void SetSFXVolume(float val)   => SettingsManager.Instance?.SetSFXVolume(val);
    public void SetUIVolume(float val)    => SettingsManager.Instance?.SetUIVolume(val);
    public void SetBrightness(float val)  => SettingsManager.Instance?.SetBrightness(val);
    public void SetResolution(int index)  => SettingsManager.Instance?.ApplyResolution(index);

    // creates a ui audiosource at runtime if none was assigned and routes it through the ui mixer group
    private void SetupAudioSource()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;

            // keep ui sounds playing even if the audio listener is paused
            uiAudioSource.ignoreListenerPause = true;
        }
        SettingsManager.Instance?.Route(uiAudioSource, SettingsManager.AudioCategory.UI);
    }

    // plays the click sound as a one-shot
    public void PlayClickSound()
    {
        if (uiAudioSource != null && clickSound != null) uiAudioSource.PlayOneShot(clickSound);
    }

    // plays the slider tick sound, rate-limited to prevent audio spam
    public void PlaySliderTick()
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
}