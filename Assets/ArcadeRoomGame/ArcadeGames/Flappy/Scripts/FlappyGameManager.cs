using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// singleton that owns all flappy bird game state: score, game over, pause, settings, and economy
public class FlappyGameManager : MonoBehaviour
{
    public static FlappyGameManager Instance { get; private set; }

    // live score display updated with each point gained
    [Header("In-Game UI")]
    public TextMeshProUGUI scoreText;

    // panel shown at the end of the run with final score and credits earned
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI creditsEarnedText;

    // pause menu root and its two sub-containers for the menu and settings views
    [Header("Pause Menu UI (Escape)")]
    public GameObject pausePanel;
    public GameObject pauseMenuContainer;
    public GameObject pauseSettingsContainer;
    // help guide sub-container shown from the pause menu
    public GameObject pauseHelpContainer;
    public bool isPaused { get; private set; } = false;

    // settings controls inside the pause menu
    [Header("Pause Menu Settings Controls")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown resolutionDropdown;

    // fullscreen overlay image used to simulate brightness by darkening the screen
    public Image brightnessOverlay;

    // number of pipes the player must pass to earn one credit
    [Header("Economy Settings")]
    public int pipesPerCredit = 3;

    // three audiosources routed through separate mixer groups for music, sfx, and ui
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource audioSource;
    public AudioSource uiSource;

    // sounds played on scoring a point and on game over
    [Header("Gameplay Audio Clips")]
    public AudioClip scoreSound;
    public AudioClip gameOverSound;

    // sounds played for button clicks and slider movement in the pause menu
    [Header("UI Audio Clips")]
    public AudioClip clickSound;
    public AudioClip sliderSound;

    // playerprefs key used to identify the active save slot
    private const string prefSlot = "Global_LastPlayedSlot";

    public int currentScore { get; private set; } = 0;

    // prevents game over logic from running more than once
    public bool isGameOver { get; private set; } = false;

    // rate-limits slider tick sounds to prevent audio spam
    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        TogglePauseUIContainers(true, false);

        UpdateScoreUI();
        RouteAudioSources();
        BindSettingsUI();

        // initialise the slider sound timestamp so the first tick is never blocked
        lastSliderSoundTime = Time.unscaledTime;

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
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            // pressing escape while in the settings sub-panel closes settings; otherwise toggles pause
            if (isPaused && pauseHelpContainer && pauseHelpContainer.activeSelf)
            {
                PlayClickAudio();
                ClosePauseHelp();
            }
            else if (isPaused && pauseSettingsContainer && pauseSettingsContainer.activeSelf)
            {
                PlayClickAudio();
                ClosePauseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    // toggles between paused and unpaused, syncing timescale and saving settings on resume
    public void TogglePause()
    {
        PlayClickAudio();
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pausePanel) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            TogglePauseUIContainers(true, false);
            RefreshSlidersFromSettings();
        }
        else
        {
            SettingsManager.Instance?.SaveAll();
        }
    }

    // switches to the settings sub-panel and syncs all controls to current saved values
    public void OpenPauseSettings()
    {
        PlayClickAudio();
        SettingsManager.Instance?.SyncDropdown(resolutionDropdown);
        RefreshSlidersFromSettings();
        TogglePauseUIContainers(false, true);
    }

    // saves settings and returns to the main pause menu view
    public void ClosePauseSettings()
    {
        PlayClickAudio();
        SettingsManager.Instance?.SaveAll();
        TogglePauseUIContainers(true, false);
    }

    // sets the active state of the two pause sub-containers
    private void TogglePauseUIContainers(bool menuActive, bool settingsActive, bool helpActive = false)
    {
        if (pauseMenuContainer) pauseMenuContainer.SetActive(menuActive);
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(settingsActive);
        if (pauseHelpContainer) pauseHelpContainer.SetActive(helpActive);
    }

    // switches to the help guide sub-panel
    public void OpenPauseHelp()
    {
        PlayClickAudio();
        TogglePauseUIContainers(false, false, true);
    }

    // returns from the help guide to the main pause menu view
    public void ClosePauseHelp()
    {
        PlayClickAudio();
        TogglePauseUIContainers(true, false, false);
    }

    // wires all settings controls and populates sliders with current saved values
    private void BindSettingsUI()
    {
        var sm = SettingsManager.Instance;

        if (sm != null && resolutionDropdown != null)
        {
            sm.PopulateDropdown(resolutionDropdown);
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(i => { sm.ApplyResolution(i); PlayClickAudio(); });
        }

        WireSlider(musicSlider,      v => SettingsManager.Instance?.SetMusicVolume(v));
        WireSlider(sfxSlider,        v => SettingsManager.Instance?.SetSFXVolume(v));
        WireSlider(uiSlider,         v => SettingsManager.Instance?.SetUIVolume(v));
        WireSlider(brightnessSlider, v => SettingsManager.Instance?.SetBrightness(v));

        RefreshSlidersFromSettings();

        // wire click sounds to every button found inside the pause panel
        if (pausePanel != null)
            foreach (Button btn in pausePanel.GetComponentsInChildren<Button>(true))
                btn.onClick.AddListener(PlayClickAudio);
    }

    // clears and re-wires a single slider with a value change handler and a tick sound
    private void WireSlider(Slider s, UnityEngine.Events.UnityAction<float> onChange)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.onValueChanged.AddListener(onChange);
        s.onValueChanged.AddListener(_ => PlaySliderTickAudio());
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

    // routes each audiosource through the appropriate mixer group
    private void RouteAudioSources()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;
        sm.Route(musicSource, SettingsManager.AudioCategory.Music);
        sm.Route(audioSource, SettingsManager.AudioCategory.SFX);
        sm.Route(uiSource,    SettingsManager.AudioCategory.UI);
    }

    // increments the score, updates the hud, and plays the score sound; ignored after game over
    public void AddScore()
    {
        if (isGameOver) return;
        currentScore++;
        UpdateScoreUI();

        if (audioSource != null && scoreSound != null) audioSource.PlayOneShot(scoreSound);
    }

    // updates the score text with the current score value
    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = currentScore.ToString();
    }

    // stops music, converts score to credits, saves flight history, and shows the game over panel
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (musicSource != null) musicSource.Stop();
        if (audioSource != null && gameOverSound != null) audioSource.PlayOneShot(gameOverSound);

        int earnedCredits = currentScore / pipesPerCredit;
        SaveFlightData(earnedCredits);

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {currentScore}";
        if (creditsEarnedText != null) creditsEarnedText.text = $"EARNED: {earnedCredits} CREDITS";
    }

    // adds earned credits to the active slot's balance and appends a score entry to the flight log
    private void SaveFlightData(int creditsToAdd)
    {
        int activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";

        int currentCredits = PlayerPrefs.GetInt(creditsKey, 0);
        PlayerPrefs.SetInt(creditsKey, currentCredits + creditsToAdd);

        // load existing leaderboard data or create a new one if none exists
        string prefsKey = $"FlappyHistory_Slot{activeSlot}";
        FlappyLeaderboard board = new FlappyLeaderboard();
        string json = PlayerPrefs.GetString(prefsKey, "");

        if (!string.IsNullOrEmpty(json)) board = JsonUtility.FromJson<FlappyLeaderboard>(json);

        FlappyScoreEntry newEntry = new FlappyScoreEntry
        {
            attemptNumber = board.entries.Count + 1,
            date = System.DateTime.Now.ToString("MM/dd/yy HH:mm"),
            score = currentScore
        };

        board.entries.Add(newEntry);
        PlayerPrefs.SetString(prefsKey, JsonUtility.ToJson(board));
        PlayerPrefs.Save();
    }

    // resumes from the pause menu button
    public void ResumeGame()
    {
        PlayClickAudio();
        if (isPaused) TogglePause();
    }

    // reloads the current scene to start a fresh run
    public void TryAgain()
    {
        PlayClickAudio();
        Time.timeScale = 1f;
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // saves settings and loads the flappy bird menu scene
    public void ReturnToMenu()
    {
        PlayClickAudio();
        Time.timeScale = 1f;
        SettingsManager.Instance?.SaveAll();
        LoadScene("FlappyMenu");
    }

    // loads a scene by name via the scene fader if available, otherwise loads directly
    private void LoadScene(string scene)
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(scene);
        else SceneManager.LoadScene(scene);
    }

    // overload that loads a scene by build index
    private void LoadScene(int buildIndex)
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(buildIndex);
        else SceneManager.LoadScene(buildIndex);
    }

    // plays the click sound through the ui audiosource
    public void PlayClickAudio()
    {
        if (uiSource != null && clickSound != null) uiSource.PlayOneShot(clickSound);
    }

    // plays the slider tick sound with a slight random pitch, rate-limited to prevent audio spam
    private void PlaySliderTickAudio()
    {
        if (lastSliderSoundTime < 0f) return;

        if (Time.unscaledTime - lastSliderSoundTime >= sliderSoundCooldown)
        {
            if (uiSource != null && sliderSound != null)
            {
                // slight pitch variation makes repeated ticks feel less robotic
                uiSource.pitch = Random.Range(0.95f, 1.05f);
                uiSource.PlayOneShot(sliderSound);
                uiSource.pitch = 1f;
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