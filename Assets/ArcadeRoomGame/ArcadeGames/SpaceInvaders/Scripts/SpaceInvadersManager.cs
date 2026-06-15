using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SpaceInvadersManager : MonoBehaviour
{
    public static SpaceInvadersManager Instance { get; private set; }

    [Header("Game Stats")]
    public int playerLives = 3;

    [Header("Scoring & Economy")]
    public int    currentScore   = 0;
    public int    pointsPerCredit = 50;
    public string baseCreditsKey = "PlayerCredits";

    [Header("UI - Mid Game")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;

    [Header("UI - Game Over")]
    public GameObject      gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI creditsEarnedText;

    [Header("Pause Menu UI (Escape)")]
    public GameObject pausePanel;
    public GameObject pauseMenuContainer;
    public GameObject pauseSettingsContainer;
    public bool isPaused { get; private set; } = false;

    [Header("Pause Menu Settings")]
    public Slider       musicSlider;
    public Slider       sfxSlider;
    public Slider       uiSlider;
    public TMP_Dropdown resolutionDropdown;

    [Header("Audio Sources")]
    public AudioSource sfxAudioSource;
    public AudioSource uiAudioSource;
    public AudioSource bgmAudioSource;

    [Header("Audio Clips - Gameplay")]
    public AudioClip playerExplosionSound;
    public AudioClip gameOverSound;
    public AudioClip newWaveSound;

    [Header("Audio Clips - UI")]
    public AudioClip buttonClickSound;
    public AudioClip sliderTickSound;

    [Header("Screen Effects")]
    public Transform cameraTransform;
    public float     shakeDuration  = 0.4f;
    public float     shakeMagnitude = 0.2f;

    [Header("Scene Routing")]
    public string mainMenuSceneName = "SpaceInvadersMenu";

    private const string prefSlot = "Global_LastPlayedSlot";

    private int   activeSlot;
    private int   currentCredits;
    public  bool  isGameOver { get; private set; } = false;
    private float nextSliderSoundTime = 0f;

    private Coroutine shakeRoutine;
    private Vector3   preShakeCamPos;
    private bool      isShaking = false;

    private Transform MainCam => cameraTransform != null ? cameraTransform
                                : (Camera.main != null ? Camera.main.transform : null);

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (bgmAudioSource != null && (bgmAudioSource == sfxAudioSource || bgmAudioSource == uiAudioSource))
        {
            sfxAudioSource = null;
            uiAudioSource  = null;
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (gameOverPanel != null)          gameOverPanel.SetActive(false);
        if (waveText != null)               waveText.gameObject.SetActive(false);
        if (pausePanel != null)             pausePanel.SetActive(false);
        if (pauseMenuContainer != null)     pauseMenuContainer.SetActive(true);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);

        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        UpdateUI();

        InitResDropdown();
        WirePauseMenuAudio();
        LoadSliders();

        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused && pauseSettingsContainer.activeSelf) ClosePauseSettings();
            else TogglePause();
        }
    }

    private string GetActiveCreditsKey() => $"{baseCreditsKey}_Slot{activeSlot}";

    // ── Resolution ────────────────────────────────────────────────────────────

    private void InitResDropdown()
    {
        if (resolutionDropdown == null || !SettingsManager.Instance) return;
        SettingsManager.Instance.PopulateDropdown(resolutionDropdown);
        resolutionDropdown.onValueChanged.AddListener(_ => PlayButtonClickSound());
    }

    public void SetResolution(int index) => SettingsManager.Instance?.ApplyResolution(index);

    // ── Pause ─────────────────────────────────────────────────────────────────

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
            if (pauseMenuContainer != null)     pauseMenuContainer.SetActive(true);
            if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);
            LoadSliders();
            if (bgmAudioSource != null) bgmAudioSource.Pause();
        }
        else
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
            SettingsManager.Instance?.SaveAll();
            if (bgmAudioSource != null) bgmAudioSource.UnPause();
        }
    }

    public void OpenPauseSettings()
    {
        PlayButtonClickSound();
        if (pauseMenuContainer != null)     pauseMenuContainer.SetActive(false);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(true);
    }

    public void ClosePauseSettings()
    {
        PlayButtonClickSound();
        SettingsManager.Instance?.SaveAll();
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);
        if (pauseMenuContainer != null)     pauseMenuContainer.SetActive(true);
    }

    public void ResumeGame()   { PlayButtonClickSound(); if (isPaused) TogglePause(); }

    public void ReturnToMainMenu()
    {
        PlayButtonClickSound();
        Time.timeScale = 1f;
        SettingsManager.Instance?.SaveAll();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Audio & Settings ─────────────────────────────────────────────────────

    private void LoadSliders()
    {
        if (!SettingsManager.Instance) return;
        if (musicSlider) musicSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedMusicVol);
        if (sfxSlider)   sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedSFXVol);
        if (uiSlider)    uiSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedUIVol);
        SettingsManager.Instance.ApplySavedAudio();
    }

    private void WirePauseMenuAudio()
    {
        if (musicSlider != null) { musicSlider.onValueChanged.RemoveAllListeners(); musicSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetMusicVolume(v)); musicSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
        if (sfxSlider   != null) { sfxSlider.onValueChanged.RemoveAllListeners();   sfxSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetSFXVolume(v));   sfxSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
        if (uiSlider    != null) { uiSlider.onValueChanged.RemoveAllListeners();    uiSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetUIVolume(v));    uiSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
    }

    public void SetMusicVolume(float val) => SettingsManager.Instance?.SetMusicVolume(val);
    public void SetSFXVolume(float val)   => SettingsManager.Instance?.SetSFXVolume(val);
    public void SetUIVolume(float val)    => SettingsManager.Instance?.SetUIVolume(val);

    public void PlayEnemyExplosionSound(AudioClip clip) { if (sfxAudioSource && clip) sfxAudioSource.PlayOneShot(clip); }

    public void PlayButtonClickSound() { if (uiAudioSource && buttonClickSound) uiAudioSource.PlayOneShot(buttonClickSound); }

    private void PlaySliderTickSound()
    {
        if (Time.unscaledTime >= nextSliderSoundTime && uiAudioSource && sliderTickSound)
        {
            uiAudioSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f;
        }
    }

    // ── Core Gameplay (unchanged) ─────────────────────────────────────────────

    public void AnnounceNewWave(int waveNumber)
    {
        if (sfxAudioSource != null && newWaveSound != null) sfxAudioSource.PlayOneShot(newWaveSound);
        if (waveText != null) { waveText.text = $"WAVE {waveNumber}"; StartCoroutine(FlashWaveTextRoutine()); }
    }

    private IEnumerator FlashWaveTextRoutine()
    {
        waveText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        waveText.gameObject.SetActive(false);
    }

    public void AddScore(int points) { if (isGameOver) return; currentScore += points; UpdateUI(); }
    public void LoseLife()           { if (isGameOver) return; StartCoroutine(PlayerDeathSequenceRoutine()); }

    private IEnumerator PlayerDeathSequenceRoutine()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(CameraShakeRoutine());
        yield return new WaitForSeconds(0.15f);
        playerLives--;
        UpdateUI();
        if (playerLives <= 0)
        {
            if (sfxAudioSource && playerExplosionSound) sfxAudioSource.PlayOneShot(playerExplosionSound);
            TriggerGameOver();
            yield break;
        }
    }

    private IEnumerator CameraShakeRoutine()
    {
        if (MainCam == null) yield break;
        isShaking = true;
        preShakeCamPos = MainCam.localPosition;
        float timePassed = 0f;
        while (timePassed < shakeDuration)
        {
            timePassed += Time.unscaledDeltaTime;
            float damp = 1.0f - Mathf.Clamp01(timePassed / shakeDuration);
            MainCam.localPosition = new Vector3(
                preShakeCamPos.x + Random.Range(-1f, 1f) * shakeMagnitude * damp,
                preShakeCamPos.y + Random.Range(-1f, 1f) * shakeMagnitude * damp,
                preShakeCamPos.z);
            yield return null;
        }
        MainCam.localPosition = preShakeCamPos;
        isShaking = false; shakeRoutine = null;
    }

    public void TriggerGameOver() { if (isGameOver) return; isGameOver = true; StartCoroutine(SafeGameOverRoutine()); }

    private IEnumerator SafeGameOverRoutine()
    {
        if (shakeRoutine != null) { StopCoroutine(shakeRoutine); shakeRoutine = null; }
        if (isShaking && MainCam != null) { MainCam.localPosition = preShakeCamPos; isShaking = false; }

        yield return new WaitForEndOfFrame();

        if (bgmAudioSource != null) bgmAudioSource.Stop();
        Time.timeScale   = 0f;
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        if (sfxAudioSource && gameOverSound) sfxAudioSource.PlayOneShot(gameOverSound);

        int creditsEarned = currentScore / pointsPerCredit;
        string key = GetActiveCreditsKey();
        currentCredits = PlayerPrefs.GetInt(key, 0) + creditsEarned;
        PlayerPrefs.SetInt(key, currentCredits);
        PlayerPrefs.Save();

        if (finalScoreText   != null) finalScoreText.text   = $"Final Score: {currentScore}";
        if (creditsEarnedText != null) creditsEarnedText.text = $"Credits Earned: +{creditsEarned}\nNew Balance: {currentCredits}";

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>() ?? gameOverPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < 0.5f) { elapsed += Time.unscaledDeltaTime; cg.alpha = Mathf.Clamp01(elapsed / 0.5f); yield return null; }
            cg.alpha = 1f;
        }
    }

    public void TryAgain() { PlayButtonClickSound(); Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

    private void UpdateUI()
    {
        if (livesText != null) livesText.text = $"LIVES: {playerLives}";
        if (scoreText != null) scoreText.text = $"SCORE: {currentScore}";
    }
}