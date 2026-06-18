using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// controls the flappy bird menu scene: panel navigation, settings, leaderboard display, and ui audio
public class FlappyMenuController : MonoBehaviour
{
    // the three panels toggled between main, personal best, and settings views
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject personalBestPanel;
    public GameObject settingsPanel;

    // fullscreen overlay image used to simulate brightness by darkening the screen
    public Image brightnessOverlay;

    // text field that displays the flight log entries when the personal best panel is open
    [Header("Personal Best UI")]
    public TextMeshProUGUI leaderboardText;

    // settings controls wired to settingsmanager in bindSettingsUI
    [Header("Settings Controls")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown resolutionDropdown;

    // scene names loaded when starting a game or returning to the arcade room
    [Header("Scene Routing")]
    public string gameSceneName = "FlappyLevel";
    public string arcadeRoomSceneName = "ArcadeRoom";

    // audiosource and clips for button and slider sounds local to this menu
    [Header("Audio Feedback - Local UI")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;
    public AudioClip sliderTickSound;

    // playerprefs key used to identify the active save slot
    private const string prefSlot = "Global_LastPlayedSlot";

    // minimum time between successive slider tick sounds
    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;
    private int activeSlot;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);

        // show only the main panel on startup
        TogglePanels(true, false, false);

        SetupAudioSource();
        BindSettingsUI();

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
            if (settingsPanel.activeSelf || personalBestPanel.activeSelf)
            {
                ReturnToMainMenu();
                PlayClickSound();
            }
        }
    }

    // loads the game scene immediately
    public void StartGame() { PlayClickSound(); LoadScene(gameSceneName); }

    // opens the personal best panel and populates the leaderboard from saved data
    public void OpenPersonalBest()
    {
        PlayClickSound();
        TogglePanels(false, true, false);
        LoadAndDisplayLeaderboard();
    }

    // opens the settings panel and syncs all controls to current saved values
    public void OpenSettings()
    {
        PlayClickSound();
        SettingsManager.Instance?.SyncDropdown(resolutionDropdown);
        RefreshSlidersFromSettings();
        TogglePanels(false, false, true);
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
    private void TogglePanels(bool main, bool pb, bool settings)
    {
        if (mainPanel) mainPanel.SetActive(main);
        if (personalBestPanel) personalBestPanel.SetActive(pb);
        if (settingsPanel) settingsPanel.SetActive(settings);
    }

    // loads a scene by name via the scene fader if available, otherwise loads directly
    private void LoadScene(string scene)
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(scene);
        else SceneManager.LoadScene(scene);
    }

    // reads the saved flight log and displays entries in reverse chronological order
    private void LoadAndDisplayLeaderboard()
    {
        string json = PlayerPrefs.GetString($"FlappyHistory_Slot{activeSlot}", "");

        if (string.IsNullOrEmpty(json))
        {
            if (leaderboardText) leaderboardText.text = "NO FLIGHT DATA FOUND.\n\nINSERT COIN TO PLAY!";
            return;
        }

        FlappyLeaderboard board = JsonUtility.FromJson<FlappyLeaderboard>(json);
        string displayText = "";

        // iterate in reverse so the most recent run appears at the top
        for (int i = board.entries.Count - 1; i >= 0; i--)
        {
            FlappyScoreEntry entry = board.entries[i];
            displayText += $"#{entry.attemptNumber} - {entry.date} - <color=#FFD700>{entry.score} PTS</color>\n";
        }

        if (leaderboardText != null) leaderboardText.text = displayText;
    }

    // wipes the flight log for the active slot and refreshes the leaderboard display
    public void ClearFlightLog()
    {
        PlayerPrefs.DeleteKey($"FlappyHistory_Slot{activeSlot}");
        PlayerPrefs.Save();
        LoadAndDisplayLeaderboard();
    }

    // wires all settings controls and populates sliders with current saved values
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

    // serialisable entry storing one run's attempt number, date, and score
    [System.Serializable]
    public class FlappyScoreEntry
    {
        public int attemptNumber;
        public string date;
        public int score;
    }

    // serialisable wrapper that holds the full list of score entries for json persistence
    [System.Serializable]
    public class FlappyLeaderboard
    {
        public List<FlappyScoreEntry> entries = new List<FlappyScoreEntry>();
    }
}