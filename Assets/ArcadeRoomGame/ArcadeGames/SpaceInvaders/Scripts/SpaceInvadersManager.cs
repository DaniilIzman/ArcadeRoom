using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SpaceInvadersManager : MonoBehaviour
{
    public static SpaceInvadersManager Instance { get; private set; }

    [Header("Game Stats")]
    public int playerLives = 3;
    public int costPerPlay = 10;
    
    [Header("Scoring & Economy")]
    public int currentScore = 0;
    public int pointsPerCredit = 50; 
    [Tooltip("Must perfectly match the string your Arcade Room uses to save money.")]
    public string baseCreditsKey = "PlayerCredits";
    
    [Header("UI - Mid Game")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText; 
    public TextMeshProUGUI waveText; 
    
    [Header("UI - Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;     
    public TextMeshProUGUI creditsEarnedText;  

    [Header("Pause Menu UI (Escape)")]
    public GameObject pausePanel; 
    public GameObject pauseMenuContainer; 
    public GameObject pauseSettingsContainer; 
    public bool isPaused { get; private set; } = false;

    [Header("Pause Menu Settings")]
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
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

    [Header("Screen Effects Settings")]
    public Transform cameraTransform; 
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 0.2f;

    [Header("Scene Routing")]
    public string mainMenuSceneName = "SpaceInvadersMenu";

    // string constants to protect against typos
    private const string mixerMusicParam = "MusicVol";
    private const string mixerSfxParam = "SFXVol";
    private const string mixerUiParam = "UIVol";
    private const string prefMusic = "Setting_MusicVol";
    private const string prefSfx = "Setting_SFXVol";
    private const string prefUi = "Setting_UIVol";
    
    // CHANGED: Saving absolute width/height instead of unreliable array indices
    private const string prefResWidth = "Setting_ResWidth";
    private const string prefResHeight = "Setting_ResHeight";
    private const string prefSlot = "Global_LastPlayedSlot";

    private Resolution[] resolutions; 
    private int activeSlot;
    private int currentCredits;
    public bool isGameOver { get; private set; } = false;
    private float nextSliderSoundTime = 0f; 

    // camera shake safety tracking
    private Coroutine shakeRoutine;
    private Vector3 preShakeCamPos;
    private bool isShaking = false;

    private Transform MainCam => cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
            return;
        }

        if (bgmAudioSource != null && (bgmAudioSource == sfxAudioSource || bgmAudioSource == uiAudioSource))
        {
            sfxAudioSource = null; 
            uiAudioSource = null;
        }
    }

    private void Start()
    {
        Time.timeScale = 1f; 
        
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (waveText != null) waveText.gameObject.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(true);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);

        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        UpdateUI();

        // CHANGED: Explicitly apply resolution on scene load
        ApplySavedResolution();

        WirePauseMenuAudio(); 
        InitializeResolutionDropdown(); 
        LoadAudioSettings(); 

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

    private string GetActiveCreditsKey()
    {
        return $"{baseCreditsKey}_Slot{activeSlot}";
    }

    // --- RESOLUTION LOGIC ---

    private void ApplySavedResolution()
    {
        int w = PlayerPrefs.GetInt(prefResWidth, Screen.currentResolution.width);
        int h = PlayerPrefs.GetInt(prefResHeight, Screen.currentResolution.height);
        Screen.SetResolution(w, h, Screen.fullScreen);
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        Resolution[] rawResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        List<Resolution> uniqueResolutions = new List<Resolution>();
        
        int savedWidth = PlayerPrefs.GetInt(prefResWidth, Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt(prefResHeight, Screen.currentResolution.height);
        int currentResIndex = 0;

        for (int i = 0; i < rawResolutions.Length; i++)
        {
            string option = $"{rawResolutions[i].width} x {rawResolutions[i].height}";
            if (!options.Contains(option))
            {
                options.Add(option);
                uniqueResolutions.Add(rawResolutions[i]);

                // Check if this unique resolution matches our saved width/height
                if (rawResolutions[i].width == savedWidth && rawResolutions[i].height == savedHeight)
                {
                    currentResIndex = uniqueResolutions.Count - 1;
                }
            }
        }

        resolutions = uniqueResolutions.ToArray();
        resolutionDropdown.AddOptions(options);
        
        // Set without notify to prevent it from triggering the event immediately on start
        resolutionDropdown.SetValueWithoutNotify(Mathf.Clamp(currentResIndex, 0, resolutions.Length - 1));
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex >= resolutions.Length) return;

        PlayButtonClickSound();
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        Canvas.ForceUpdateCanvases();

        // Save explicit width and height
        PlayerPrefs.SetInt(prefResWidth, resolution.width);
        PlayerPrefs.SetInt(prefResHeight, resolution.height);
        PlayerPrefs.Save();
    }

    // --- PAUSE & MENU LOGIC ---

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
            LoadAudioSettings();
            if (bgmAudioSource != null) bgmAudioSource.Pause();
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            SaveAudioSettingsToDisk();
            if (bgmAudioSource != null) bgmAudioSource.UnPause();
        }
    }

    public void OpenPauseSettings()
    {
        PlayButtonClickSound();
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(false);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(true);
    }

    public void ClosePauseSettings()
    {
        PlayButtonClickSound();
        SaveAudioSettingsToDisk(); 
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(true);
    }
    
    private void SaveAudioSettingsToDisk()
    {
        if (musicSlider != null) PlayerPrefs.SetFloat(prefMusic, musicSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat(prefSfx, sfxSlider.value);
        if (uiSlider != null) PlayerPrefs.SetFloat(prefUi, uiSlider.value);
        PlayerPrefs.Save();
    }

    public void ResumeGame()
    {
        PlayButtonClickSound();
        if (isPaused) TogglePause(); 
    }

    public void ReturnToMainMenu()
    {
        PlayButtonClickSound();
        Time.timeScale = 1f; 
        SaveAudioSettingsToDisk();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // --- AUDIO LOGIC ---

    private void WirePauseMenuAudio()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners(); 
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener((val) => PlaySliderTickSound());
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener((val) => PlaySliderTickSound());
        }
        if (uiSlider != null)
        {
            uiSlider.onValueChanged.RemoveAllListeners();
            uiSlider.onValueChanged.AddListener(SetUIVolume);
            uiSlider.onValueChanged.AddListener((val) => PlaySliderTickSound());
        }
    }

    public void PlayEnemyExplosionSound(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    private void LoadAudioSettings()
    {
        float cachedMusic = PlayerPrefs.GetFloat(prefMusic, 0.75f);
        float cachedSfx = PlayerPrefs.GetFloat(prefSfx, 0.75f);
        float cachedUi = PlayerPrefs.GetFloat(prefUi, 0.75f);

        if (musicSlider != null) musicSlider.value = cachedMusic;
        if (sfxSlider != null) sfxSlider.value = cachedSfx;
        if (uiSlider != null) uiSlider.value = cachedUi;

        SetMusicVolume(cachedMusic);
        SetSFXVolume(cachedSfx);
        SetUIVolume(cachedUi);
    }

    public void SetMusicVolume(float val) => ApplyVolumeToMixer(mixerMusicParam, val);
    public void SetSFXVolume(float val) => ApplyVolumeToMixer(mixerSfxParam, val);
    public void SetUIVolume(float val) => ApplyVolumeToMixer(mixerUiParam, val);

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (audioMixer == null) return;
        float targetDb = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, targetDb);
    }

    public void PlayButtonClickSound()
    {
        if (uiAudioSource != null && buttonClickSound != null)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }
    }

    private void PlaySliderTickSound()
    {
        if (Time.unscaledTime >= nextSliderSoundTime && uiAudioSource != null && sliderTickSound != null)
        {
            uiAudioSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f; 
        }
    }

    // --- CORE GAMEPLAY LOGIC ---

    public void AnnounceNewWave(int waveNumber)
    {
        if (sfxAudioSource != null && newWaveSound != null) sfxAudioSource.PlayOneShot(newWaveSound);
        if (waveText != null)
        {
            waveText.text = $"WAVE {waveNumber}";
            StartCoroutine(FlashWaveTextRoutine());
        }
    }

    private IEnumerator FlashWaveTextRoutine()
    {
        waveText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        waveText.gameObject.SetActive(false);
    }

    public void AddScore(int points)
    {
        if (isGameOver) return;
        currentScore += points;
        UpdateUI();
    }

    public void LoseLife()
    {
        if (isGameOver) return;
        StartCoroutine(PlayerDeathSequenceRoutine());
    }

    private IEnumerator PlayerDeathSequenceRoutine()
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(CameraShakeRoutine());

        yield return new WaitForSeconds(0.15f);

        playerLives--;
        UpdateUI();

        if (playerLives <= 0)
        {
            if (sfxAudioSource != null && playerExplosionSound != null) sfxAudioSource.PlayOneShot(playerExplosionSound);
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
            float dampingFactor = 1.0f - Mathf.Clamp01(timePassed / shakeDuration);
            
            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude * dampingFactor;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude * dampingFactor;

            MainCam.localPosition = new Vector3(preShakeCamPos.x + offsetX, preShakeCamPos.y + offsetY, preShakeCamPos.z);
            yield return null;
        }

        MainCam.localPosition = preShakeCamPos; 
        isShaking = false;
        shakeRoutine = null;
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        StartCoroutine(SafeGameOverRoutine());
    }

    private IEnumerator SafeGameOverRoutine()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        
        if (isShaking && MainCam != null)
        {
            MainCam.localPosition = preShakeCamPos;
            isShaking = false;
        }

        yield return new WaitForEndOfFrame();

        if (bgmAudioSource != null) bgmAudioSource.Stop();

        Time.timeScale = 0f; 
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (sfxAudioSource != null && gameOverSound != null) sfxAudioSource.PlayOneShot(gameOverSound);
        
        int creditsEarned = currentScore / pointsPerCredit;
        string creditsKey = GetActiveCreditsKey();
        currentCredits = PlayerPrefs.GetInt(creditsKey, 0);
        currentCredits += creditsEarned;
        
        PlayerPrefs.SetInt(creditsKey, currentCredits);
        PlayerPrefs.Save();

        if (finalScoreText != null) finalScoreText.text = $"Final Score: {currentScore}";
        if (creditsEarnedText != null) 
        {
            creditsEarnedText.text = $"Credits Earned: +{creditsEarned}\nNew Balance: {currentCredits}";
        }
        
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

    public void TryAgain()
    {
        PlayButtonClickSound();
        string creditsKey = GetActiveCreditsKey();
        currentCredits = PlayerPrefs.GetInt(creditsKey, 0);

        if (currentCredits >= costPerPlay)
        {
            currentCredits -= costPerPlay;
            PlayerPrefs.SetInt(creditsKey, currentCredits);
            PlayerPrefs.Save();

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void UpdateUI()
    {
        if (livesText != null) livesText.text = $"LIVES: {playerLives}";
        if (scoreText != null) scoreText.text = $"SCORE: {currentScore}";
    }
}