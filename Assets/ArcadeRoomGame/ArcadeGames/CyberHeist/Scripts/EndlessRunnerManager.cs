using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// central game manager for the endless runner; handles score, distance, credits,
// game over, pausing, powerup timers, settings ui, and audio routing
public class EndlessRunnerManager : MonoBehaviour
{
    public static EndlessRunnerManager Instance { get; private set; }

    [Header("Gameplay Stats")]
    public int score = 0;
    public int distance = 0;
    public bool isGameOver { get; private set; } = false;
    public bool isPaused { get; private set; } = false;

    [Header("Economy Sync")]
    [Tooltip("How much score equals 1 credit")]
    public int scoreDivider = 100;    // credits from score = score / scoreDivider
    [Tooltip("How much distance equals 1 credit")]
    public int distanceDivider = 10;  // credits from distance = distance / distanceDivider
    public string baseCreditsKey = "PlayerCredits";   // playerprefs key prefix shared with the arcade room
    public string mainMenuSceneName = "EndlessRunnerMenu";

    [Header("Mid-Game UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI distanceText;

    [Header("Power-Up UI")]
    public GameObject speedBoostUIPanel;      // shown while a speed boost is active
    public TextMeshProUGUI speedBoostTimerText;
    public GameObject jumpBoostUIPanel;       // shown while jump boots are active
    public TextMeshProUGUI jumpBoostTimerText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalDistanceText;
    public TextMeshProUGUI creditsEarnedText;

    [Header("Pause Menu UI")]
    public GameObject pausePanel;
    public GameObject pauseSettingsContainer;  // the settings sub-page within the pause menu
    public GameObject pauseMainContainer;      // the main pause menu page with resume/settings/quit buttons

    [Header("Settings Controls")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown resolutionDropdown;
    public Image brightnessOverlay; // black overlay whose alpha represents the current brightness

    [Header("Audio Sources")]
    public AudioSource sfxAudioSource;    // gameplay one-shots: crashes, coins, powerups
    public AudioSource uiAudioSource;     // ui one-shots: button clicks, slider ticks
    public AudioSource musicAudioSource;  // background music track
    public AudioClip clickSound;
    public AudioClip sliderTickSound;
    public AudioClip coinPickupSound;
    public AudioClip crashSound;

    // playerprefs key that stores which save slot is currently active
    private const string prefSlot = "Global_LastPlayedSlot";

    private int activeSlot;          // the save slot number read from playerprefs on start
    private string creditsPrefsKey;  // the full key used to read/write credits for the active slot

    // throttle the slider tick sound so it doesn't fire on every tiny movement
    private float nextSliderSoundTime = 0f;

    // remaining seconds for each active powerup; counted down in update
    private float speedBoostTimeLeft = 0f;
    private float jumpBoostTimeLeft = 0f;

    private void Awake()
    {
        // enforce singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // make sure time is running normally in case we returned from a paused state
        Time.timeScale = 1f;

        // build the credits key for the active slot so credits are saved to the right entry
        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        // hide all overlay panels at the start of a run
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (speedBoostUIPanel) speedBoostUIPanel.SetActive(false);
        if (jumpBoostUIPanel) jumpBoostUIPanel.SetActive(false);

        UpdateGameplayUI();
        RouteAudioSources();
        BindSettingsUI();

        // subscribe to brightness changes so the overlay updates whenever the setting changes
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged += UpdateBrightnessOverlay;
    }

    private void OnDestroy()
    {
        // unsubscribe to prevent a dangling delegate after the scene unloads
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged -= UpdateBrightnessOverlay;
    }

    private void Update()
    {
        // escape either closes the settings sub-page or toggles the whole pause menu
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused && pauseSettingsContainer != null && pauseSettingsContainer.activeSelf)
            {
                ClosePauseSettings();
            }
            else
            {
                TogglePause();
            }
        }

        // powerup timers only tick when the game is actually running
        if (!isPaused && !isGameOver)
        {
            HandlePowerUpTimers();
        }
    }

    // counts down the active powerup timers and hides their ui panels when they expire
    private void HandlePowerUpTimers()
    {
        if (speedBoostTimeLeft > 0)
        {
            speedBoostTimeLeft -= Time.deltaTime;
            if (speedBoostTimerText) speedBoostTimerText.text = speedBoostTimeLeft.ToString("F1") + "s";

            // hide the panel the frame the timer reaches zero
            if (speedBoostTimeLeft <= 0 && speedBoostUIPanel) speedBoostUIPanel.SetActive(false);
        }

        if (jumpBoostTimeLeft > 0)
        {
            jumpBoostTimeLeft -= Time.deltaTime;
            if (jumpBoostTimerText) jumpBoostTimerText.text = jumpBoostTimeLeft.ToString("F1") + "s";

            if (jumpBoostTimeLeft <= 0 && jumpBoostUIPanel) jumpBoostUIPanel.SetActive(false);
        }
    }

    // called by the player when they collect a speed boost pickup; starts the countdown and shows the ui
    public void ActivateSpeedBoostUI(float duration)
    {
        speedBoostTimeLeft = duration;
        if (speedBoostUIPanel) speedBoostUIPanel.SetActive(true);
    }

    // called by the player when they collect jump boots; starts the countdown and shows the ui
    public void ActivateJumpBoostUI(float duration)
    {
        jumpBoostTimeLeft = duration;
        if (jumpBoostUIPanel) jumpBoostUIPanel.SetActive(true);
    }

    // adds to the score and refreshes the hud; ignores calls made after game over
    public void AddScore(int amount)
    {
        if (isGameOver) return;
        score += amount;
        UpdateGameplayUI();
    }

    // adds to the distance counter and refreshes the hud; ignores calls made after game over
    public void AddDistance(int amount)
    {
        if (isGameOver) return;
        distance += amount;
        UpdateGameplayUI();
    }

    // refreshes the score and distance labels on the hud
    private void UpdateGameplayUI()
    {
        if (scoreText) scoreText.text = $"SCORE: {score}";
        if (distanceText) distanceText.text = $"DISTANCE: {distance}m";
    }

    // called by the player controller when the player hits an obstacle; triggers the game over flow
    public void PlayerCrashed()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (sfxAudioSource && crashSound) sfxAudioSource.PlayOneShot(crashSound);

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // short delay gives the crash effect time to play before freezing time
        yield return new WaitForSeconds(0.5f);
        Time.timeScale = 0f;

        // unlock the cursor so the player can interact with the game over buttons
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // calculate how many credits the player earned from this run
        int currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0);
        int creditsEarned = (score / scoreDivider) + (distance / distanceDivider);
        creditsEarned = Mathf.Max(0, creditsEarned);

        // add the earned credits to the running total and save immediately
        currentCredits += creditsEarned;
        PlayerPrefs.SetInt(creditsPrefsKey, currentCredits);
        PlayerPrefs.Save();

        // show the game over panel and populate it with the run's results
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = $"FINAL SCORE: {score}";
        if (finalDistanceText) finalDistanceText.text = $"DISTANCE: {distance}m";
        if (creditsEarnedText)
        {
            creditsEarnedText.text = $"CREDITS EARNED: +{creditsEarned}\nNEW BALANCE: {currentCredits}";
        }
    }

    // plays the coin pickup sound; called by the player controller on coin trigger
    public void PlayCoinPickupSound()
    {
        if (sfxAudioSource && coinPickupSound) sfxAudioSource.PlayOneShot(coinPickupSound);
    }

    // reloads the current scene to restart the run
    public void TryAgain()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // saves settings and returns to the endless runner menu scene
    public void ReturnToMainMenu()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        SettingsManager.Instance?.SaveAll();
        LoadScene(mainMenuSceneName);
    }

    // toggles the pause state; locks or unlocks the cursor and saves settings on unpause
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // always open to the main pause page, not the settings sub-page
            if (pauseMainContainer) pauseMainContainer.SetActive(true);
            if (pauseSettingsContainer) pauseSettingsContainer.SetActive(false);

            // sync sliders to whatever the settings manager currently holds
            RefreshSlidersFromSettings();
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // save any changes made while paused
            SettingsManager.Instance?.SaveAll();
        }
    }

    // button handler that plays a click and resumes if the game is paused
    public void ResumeGame()
    {
        PlayClickSound();
        if (isPaused)
        {
            TogglePause();
        }
    }

    // switches the pause menu to the settings sub-page
    public void OpenPauseSettings()
    {
        PlayClickSound();
        SettingsManager.Instance?.SyncDropdown(resolutionDropdown);
        RefreshSlidersFromSettings();
        if (pauseMainContainer) pauseMainContainer.SetActive(false);
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(true);
    }

    // saves settings and switches back to the main pause page
    public void ClosePauseSettings()
    {
        PlayClickSound();
        SettingsManager.Instance?.SaveAll();
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(false);
        if (pauseMainContainer) pauseMainContainer.SetActive(true);
    }

    // loads a scene by name, routing through scenefader if one exists in the scene
    private void LoadScene(string scene)
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(scene);
        else SceneManager.LoadScene(scene);
    }

    // loads a scene by build index, routing through scenefader if one exists in the scene
    private void LoadScene(int buildIndex)
    {
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(buildIndex);
        else SceneManager.LoadScene(buildIndex);
    }

    // wires up all settings sliders and the resolution dropdown to the settings manager
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

    // registers the scene's audio sources with the settings manager so volume changes apply to them
    private void RouteAudioSources()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;
        sm.Route(musicAudioSource, SettingsManager.AudioCategory.Music);
        sm.Route(sfxAudioSource,   SettingsManager.AudioCategory.SFX);
        sm.Route(uiAudioSource,    SettingsManager.AudioCategory.UI);
    }

    // plays the ui click sound through the ui audio source
    public void PlayClickSound()
    {
        if (uiAudioSource && clickSound) uiAudioSource.PlayOneShot(clickSound);
    }

    // plays the slider tick sound at most once per 60ms to avoid rapid-fire audio spam
    private void PlaySliderTickSound()
    {
        if (Time.unscaledTime >= nextSliderSoundTime && uiAudioSource && sliderTickSound)
        {
            uiAudioSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f;
        }
    }
}