using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// singleton that owns all space invaders game state: score, lives, waves, pause, and game over
public class SpaceInvadersManager : MonoBehaviour
{
    public static SpaceInvadersManager Instance { get; private set; }

    // starting life count decremented on each hit
    [Header("Game Stats")]
    public int playerLives = 3;

    // score accumulates per kill; credits are awarded at a fixed conversion rate on game over
    [Header("Scoring & Economy")]
    public int currentScore = 0;
    public int pointsPerCredit = 50;
    public string baseCreditsKey = "PlayerCredits";

    // hud elements updated during gameplay
    [Header("UI - Mid Game")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;
    public Image brightnessOverlay;

    // panel shown at the end of the game displaying final score and credits earned
    [Header("UI - Game Over")]
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

    // settings sliders and dropdown inside the pause menu
    [Header("Pause Menu Settings Controls")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown resolutionDropdown;

    // three separate audiosources for music, sfx, and ui sounds routed through the mixer
    [Header("Audio Sources")]
    public AudioSource sfxAudioSource;
    public AudioSource uiAudioSource;
    public AudioSource bgmAudioSource;

    // clips assigned to each game event
    [Header("Audio Clips")]
    public AudioClip playerExplosionSound;
    public AudioClip gameOverSound;
    public AudioClip newWaveSound;
    public AudioClip buttonClickSound;
    public AudioClip sliderTickSound;

    // camera transform used for the shake effect; falls back to Camera.main if left unassigned
    [Header("Screen Effects Settings")]
    public Transform cameraTransform;
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 0.2f;

    // scene loaded when the player returns to the space invaders menu
    [Header("Scene Routing")]
    public string mainMenuSceneName = "SpaceInvadersMenu";

    // playerprefs key used to read the active save slot
    private const string prefSlot = "Global_LastPlayedSlot";

    private int activeSlot;
    private int currentCredits;

    // prevents game over from being triggered more than once
    public bool isGameOver { get; private set; } = false;

    // rate-limits slider tick sounds to prevent audio spam
    private float nextSliderSoundTime = 0f;

    private Coroutine shakeRoutine;

    // stores the camera's position before shaking so it can be snapped back afterward
    private Vector3 preShakeCamPos;
    private bool isShaking = false;

    // resolves to the assigned camera transform or falls back to the main camera
    private Transform MainCam => cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // prevent bgmAudioSource from being accidentally shared with sfx or ui sources
        if (bgmAudioSource != null && (bgmAudioSource == sfxAudioSource || bgmAudioSource == uiAudioSource))
        {
            sfxAudioSource = null;
            uiAudioSource = null;
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        // hide panels that should only appear at specific moments
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (waveText != null) waveText.gameObject.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // show the main pause view and hide the settings sub-panel on start
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(true);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);
        if (pauseHelpContainer != null) pauseHelpContainer.SetActive(false);

        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        updateui();

        RouteAudioSources();
        BindSettingsUI();

        // subscribe to brightness changes so the overlay stays in sync
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged += UpdateBrightnessOverlay;

        // start background music if it isn't already playing
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
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
            // escape backs out of a sub-panel first; only toggles pause from the main pause view
            if (isPaused && pauseHelpContainer != null && pauseHelpContainer.activeSelf) ClosePauseHelp();
            else if (isPaused && pauseSettingsContainer.activeSelf) ClosePauseSettings();
            else TogglePause();
        }
    }

    // toggles between paused and unpaused, syncing timescale, cursor, and bgm state
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pausePanel != null) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (pauseMenuContainer != null) pauseMenuContainer.SetActive(true);
            if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);
            if (pauseHelpContainer != null) pauseHelpContainer.SetActive(false);
            RefreshSlidersFromSettings();
            if (bgmAudioSource != null) bgmAudioSource.Pause();
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            SettingsManager.Instance?.SaveAll();
            if (bgmAudioSource != null) bgmAudioSource.UnPause();
        }
    }

    // switches from the pause menu view to the settings sub-panel
    public void OpenPauseSettings()
    {
        PlayButtonClickSound();
        SettingsManager.Instance?.SyncDropdown(resolutionDropdown);
        RefreshSlidersFromSettings();
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(false);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(true);
        if (pauseHelpContainer != null) pauseHelpContainer.SetActive(false);
    }

    // saves settings and returns from the settings sub-panel to the main pause menu view
    public void ClosePauseSettings()
    {
        PlayButtonClickSound();
        SettingsManager.Instance?.SaveAll();
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(true);
        if (pauseHelpContainer != null) pauseHelpContainer.SetActive(false);
    }

    // switches from the pause menu view to the help guide sub-panel
    public void OpenPauseHelp()
    {
        PlayButtonClickSound();
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(false);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);
        if (pauseHelpContainer != null) pauseHelpContainer.SetActive(true);
    }

    // returns from the help guide to the main pause menu view
    public void ClosePauseHelp()
    {
        PlayButtonClickSound();
        if (pauseHelpContainer != null) pauseHelpContainer.SetActive(false);
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(true);
    }

    // wires all settings controls and populates sliders with current saved values
    private void BindSettingsUI()
    {
        var sm = SettingsManager.Instance;

        if (sm != null && resolutionDropdown != null)
        {
            sm.PopulateDropdown(resolutionDropdown);
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(i => { sm.ApplyResolution(i); PlayButtonClickSound(); });
        }

        WireSlider(musicSlider,      v => SettingsManager.Instance?.SetMusicVolume(v));
        WireSlider(sfxSlider,        v => SettingsManager.Instance?.SetSFXVolume(v));
        WireSlider(uiSlider,         v => SettingsManager.Instance?.SetUIVolume(v));
        WireSlider(brightnessSlider, v => SettingsManager.Instance?.SetBrightness(v));

        RefreshSlidersFromSettings();

        // wire click sounds to every button found inside the pause panel
        if (pausePanel != null)
            foreach (Button btn in pausePanel.GetComponentsInChildren<Button>(true))
                btn.onClick.AddListener(PlayButtonClickSound);
    }

    // clears and re-wires a single slider with a value change handler and a tick sound
    private void WireSlider(Slider s, UnityEngine.Events.UnityAction<float> onChange)
    {
        if (s == null) return;
        s.onValueChanged.RemoveAllListeners();
        s.onValueChanged.AddListener(onChange);
        s.onValueChanged.AddListener(_ => PlaySliderTickSound());
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
        sm.Route(bgmAudioSource, SettingsManager.AudioCategory.Music);
        sm.Route(sfxAudioSource, SettingsManager.AudioCategory.SFX);
        sm.Route(uiAudioSource,  SettingsManager.AudioCategory.UI);
    }

    // plays the button click sound through the ui audiosource
    public void PlayButtonClickSound()
    {
        if (uiAudioSource != null && buttonClickSound != null) uiAudioSource.PlayOneShot(buttonClickSound);
    }

    // plays the slider tick sound rate-limited to prevent audio spam
    private void PlaySliderTickSound()
    {
        if (Time.unscaledTime >= nextSliderSoundTime && uiAudioSource != null && sliderTickSound != null)
        {
            uiAudioSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f;
        }
    }

    // called by invadercollision to play the explosion sound through a persistent audiosource
    public void PlayEnemyExplosionSound(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null) sfxAudioSource.PlayOneShot(clip);
    }

    // resumes the game from a button in the pause panel
    public void ResumeGame()
    {
        PlayButtonClickSound();
        if (isPaused) TogglePause();
    }

    // saves settings and loads the space invaders menu scene
    public void ReturnToMainMenu()
    {
        PlayButtonClickSound();
        Time.timeScale = 1f;
        SettingsManager.Instance?.SaveAll();
        LoadScene(mainMenuSceneName);
    }

    // plays the new wave sound and briefly shows the wave number on screen
    public void AnnounceNewWave(int waveNumber)
    {
        if (sfxAudioSource != null && newWaveSound != null) sfxAudioSource.PlayOneShot(newWaveSound);
        if (waveText != null)
        {
            waveText.text = $"WAVE {waveNumber}";
            StartCoroutine(flashwavetextroutine());
        }
    }

    // activates the wave text for two seconds then hides it again
    private IEnumerator flashwavetextroutine()
    {
        waveText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        waveText.gameObject.SetActive(false);
    }

    // adds points to the current score and refreshes the hud; ignored after game over
    public void AddScore(int points)
    {
        if (isGameOver) return;
        currentScore += points;
        updateui();
    }

    // starts the player death sequence; ignored after game over
    public void LoseLife()
    {
        if (isGameOver) return;
        StartCoroutine(playerdeathsequenceroutine());
    }

    // triggers a camera shake, decrements lives, then ends the game if lives reach zero
    private IEnumerator playerdeathsequenceroutine()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(camerashakeroutine());

        yield return new WaitForSeconds(0.15f);

        playerLives--;
        updateui();

        if (playerLives <= 0)
        {
            if (sfxAudioSource != null && playerExplosionSound != null) sfxAudioSource.PlayOneShot(playerExplosionSound);
            TriggerGameOver();
            yield break;
        }
    }

    // randomly offsets the camera position each frame over the shake duration with decreasing intensity
    private IEnumerator camerashakeroutine()
    {
        if (MainCam == null) yield break;

        isShaking = true;
        preShakeCamPos = MainCam.localPosition;
        float timePassed = 0f;

        while (timePassed < shakeDuration)
        {
            timePassed += Time.unscaledDeltaTime;

            // reduce magnitude linearly over the shake duration for a natural falloff
            float dampingFactor = 1.0f - Mathf.Clamp01(timePassed / shakeDuration);

            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude * dampingFactor;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude * dampingFactor;

            MainCam.localPosition = new Vector3(preShakeCamPos.x + offsetX, preShakeCamPos.y + offsetY, preShakeCamPos.z);
            yield return null;
        }

        // snap the camera back to exactly where it was before the shake
        MainCam.localPosition = preShakeCamPos;
        isShaking = false;
        shakeRoutine = null;
    }

    // entry point for ending the game; guards against duplicate calls
    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        StartCoroutine(safegameoverroutine());
    }

    // stops all gameplay, calculates and saves earned credits, then fades in the game over panel
    private IEnumerator safegameoverroutine()
    {
        // cancel any in-progress shake and restore the camera immediately
        if (shakeRoutine != null) { StopCoroutine(shakeRoutine); shakeRoutine = null; }
        if (isShaking && MainCam != null) { MainCam.localPosition = preShakeCamPos; isShaking = false; }

        // wait one frame to ensure all physics and particle callbacks have resolved
        yield return new WaitForEndOfFrame();

        if (bgmAudioSource != null) bgmAudioSource.Stop();

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (sfxAudioSource != null && gameOverSound != null) sfxAudioSource.PlayOneShot(gameOverSound);

        // convert score to credits and add them to the active slot's saved balance
        int creditsEarned = currentScore / pointsPerCredit;
        string creditsKey = $"{baseCreditsKey}_Slot{activeSlot}";
        currentCredits = PlayerPrefs.GetInt(creditsKey, 0);
        currentCredits += creditsEarned;

        PlayerPrefs.SetInt(creditsKey, currentCredits);
        PlayerPrefs.Save();

        if (finalScoreText != null) finalScoreText.text = $"Final Score: {currentScore}";
        if (creditsEarnedText != null) creditsEarnedText.text = $"Credits Earned: +{creditsEarned}\nNew Balance: {currentCredits}";

        // fade the game over panel in using a canvas group alpha tween with unscaled time
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            CanvasGroup goCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (goCanvasGroup == null) goCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();

            goCanvasGroup.alpha = 0f;
            float fadeDuration = 0.5f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                goCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            goCanvasGroup.alpha = 1f;
        }
    }

    // reloads the current scene to restart from the beginning
    public void TryAgain()
    {
        PlayButtonClickSound();
        Time.timeScale = 1f;
        LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // updates the lives and score hud text fields
    private void updateui()
    {
        if (livesText != null) livesText.text = $"LIVES: {playerLives}";
        if (scoreText != null) scoreText.text = $"SCORE: {currentScore}";
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
}