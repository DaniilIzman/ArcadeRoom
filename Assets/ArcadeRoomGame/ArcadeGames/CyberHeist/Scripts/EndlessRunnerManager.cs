using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndlessRunnerManager : MonoBehaviour
{
    public static EndlessRunnerManager Instance { get; private set; }

    [Header("Gameplay Stats")]
    public int  score    = 0;
    public int  distance = 0;
    public bool isGameOver { get; private set; } = false;
    public bool isPaused   { get; private set; } = false;

    [Header("Economy Sync")]
    public int    scoreDivider      = 100;
    public int    distanceDivider   = 10;
    public string baseCreditsKey    = "PlayerCredits";
    public string mainMenuSceneName = "EndlessRunnerMenu";

    [Header("Mid-Game UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI distanceText;

    [Header("Power-Up UI")]
    public GameObject      speedBoostUIPanel;
    public TextMeshProUGUI speedBoostTimerText;
    public GameObject      jumpBoostUIPanel;
    public TextMeshProUGUI jumpBoostTimerText;

    [Header("Game Over UI")]
    public GameObject      gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalDistanceText;
    public TextMeshProUGUI creditsEarnedText;

    [Header("Pause Menu UI")]
    public GameObject pausePanel;
    public GameObject pauseSettingsContainer;
    public GameObject pauseMainContainer;

    [Header("Settings UI")]
    public Slider       musicSlider;
    public Slider       sfxSlider;
    public Slider       uiSlider;
    public TMP_Dropdown resolutionDropdown;

    [Header("Audio Sources")]
    public AudioSource sfxAudioSource;
    public AudioSource uiAudioSource;
    public AudioClip   clickSound;
    public AudioClip   sliderTickSound;
    public AudioClip   coinPickupSound;
    public AudioClip   crashSound;

    private const string prefSlot = "Global_LastPlayedSlot";

    private int    activeSlot;
    private string creditsPrefsKey;
    private float  nextSliderSoundTime = 0f;
    private float  speedBoostTimeLeft  = 0f;
    private float  jumpBoostTimeLeft   = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        activeSlot      = PlayerPrefs.GetInt(prefSlot, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel)    pausePanel.SetActive(false);
        if (speedBoostUIPanel) speedBoostUIPanel.SetActive(false);
        if (jumpBoostUIPanel)  jumpBoostUIPanel.SetActive(false);

        UpdateGameplayUI();
        InitResDropdown();
        WireMenuAudio();
        LoadSliders();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused && pauseSettingsContainer != null && pauseSettingsContainer.activeSelf)
                ClosePauseSettings();
            else
                TogglePause();
        }
        if (!isPaused && !isGameOver) HandlePowerUpTimers();
    }

    // ── Gameplay ──────────────────────────────────────────────────────────────

    private void HandlePowerUpTimers()
    {
        if (speedBoostTimeLeft > 0)
        {
            speedBoostTimeLeft -= Time.deltaTime;
            if (speedBoostTimerText) speedBoostTimerText.text = speedBoostTimeLeft.ToString("F1") + "s";
            if (speedBoostTimeLeft <= 0 && speedBoostUIPanel) speedBoostUIPanel.SetActive(false);
        }
        if (jumpBoostTimeLeft > 0)
        {
            jumpBoostTimeLeft -= Time.deltaTime;
            if (jumpBoostTimerText) jumpBoostTimerText.text = jumpBoostTimeLeft.ToString("F1") + "s";
            if (jumpBoostTimeLeft <= 0 && jumpBoostUIPanel) jumpBoostUIPanel.SetActive(false);
        }
    }

    public void ActivateSpeedBoostUI(float duration) { speedBoostTimeLeft = duration; if (speedBoostUIPanel) speedBoostUIPanel.SetActive(true); }
    public void ActivateJumpBoostUI(float duration)  { jumpBoostTimeLeft  = duration; if (jumpBoostUIPanel)  jumpBoostUIPanel.SetActive(true); }

    public void AddScore(int amount)    { if (isGameOver) return; score    += amount; UpdateGameplayUI(); }
    public void AddDistance(int amount) { if (isGameOver) return; distance += amount; UpdateGameplayUI(); }

    private void UpdateGameplayUI()
    {
        if (scoreText)    scoreText.text    = $"SCORE: {score}";
        if (distanceText) distanceText.text = $"DISTANCE: {distance}m";
    }

    public void PlayerCrashed()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (sfxAudioSource && crashSound) sfxAudioSource.PlayOneShot(crashSound);
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(0.5f);
        Time.timeScale   = 0f;
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        int currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0);
        int creditsEarned  = Mathf.Max(0, (score / scoreDivider) + (distance / distanceDivider));
        currentCredits    += creditsEarned;
        PlayerPrefs.SetInt(creditsPrefsKey, currentCredits);
        PlayerPrefs.Save();

        if (gameOverPanel)     gameOverPanel.SetActive(true);
        if (finalScoreText)    finalScoreText.text    = $"FINAL SCORE: {score}";
        if (finalDistanceText) finalDistanceText.text = $"DISTANCE: {distance}m";
        if (creditsEarnedText) creditsEarnedText.text = $"CREDITS EARNED: +{creditsEarned}\nNEW BALANCE: {currentCredits}";
    }

    public void PlayCoinPickupSound() { if (sfxAudioSource && coinPickupSound) sfxAudioSource.PlayOneShot(coinPickupSound); }

    // ── Menu Routing ──────────────────────────────────────────────────────────

    public void TryAgain()        { PlayClickSound(); Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void ReturnToMainMenu(){ PlayClickSound(); Time.timeScale = 1f; SettingsManager.Instance?.SaveAll(); SceneManager.LoadScene(mainMenuSceneName); }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
            if (pauseMainContainer)     pauseMainContainer.SetActive(true);
            if (pauseSettingsContainer) pauseSettingsContainer.SetActive(false);
        }
        else
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
            SettingsManager.Instance?.SaveAll();
        }
    }

    public void ResumeGame()       { PlayClickSound(); if (isPaused) TogglePause(); }

    public void OpenPauseSettings()
    {
        PlayClickSound();
        if (pauseMainContainer)     pauseMainContainer.SetActive(false);
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(true);
    }

    public void ClosePauseSettings()
    {
        PlayClickSound();
        SettingsManager.Instance?.SaveAll();
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(false);
        if (pauseMainContainer)     pauseMainContainer.SetActive(true);
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    private void InitResDropdown()
    {
        if (resolutionDropdown == null || !SettingsManager.Instance) return;
        SettingsManager.Instance.PopulateDropdown(resolutionDropdown);
        resolutionDropdown.onValueChanged.AddListener(_ => PlayClickSound());
    }

    public void SetResolution(int index) => SettingsManager.Instance?.ApplyResolution(index);

    // ── Audio & Settings ─────────────────────────────────────────────────────

    private void LoadSliders()
    {
        if (!SettingsManager.Instance) return;
        if (musicSlider) musicSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedMusicVol);
        if (sfxSlider)   sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedSFXVol);
        if (uiSlider)    uiSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedUIVol);
        SettingsManager.Instance.ApplySavedAudio();
    }

    private void WireMenuAudio()
    {
        if (musicSlider) { musicSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetMusicVolume(v)); musicSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
        if (sfxSlider)   { sfxSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetSFXVolume(v));   sfxSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
        if (uiSlider)    { uiSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetUIVolume(v));    uiSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
    }

    public void SetMusicVolume(float val) => SettingsManager.Instance?.SetMusicVolume(val);
    public void SetSFXVolume(float val)   => SettingsManager.Instance?.SetSFXVolume(val);
    public void SetUIVolume(float val)    => SettingsManager.Instance?.SetUIVolume(val);

    public void PlayClickSound()
    {
        if (uiAudioSource && clickSound) uiAudioSource.PlayOneShot(clickSound);
    }

    private void PlaySliderTickSound()
    {
        if (Time.unscaledTime >= nextSliderSoundTime && uiAudioSource && sliderTickSound)
        {
            uiAudioSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f;
        }
    }
}