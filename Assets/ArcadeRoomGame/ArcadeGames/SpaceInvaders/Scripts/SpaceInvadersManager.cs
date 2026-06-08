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
    public AudioSource uiAndSfxSource; 
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
    private const string prefResolution = "Setting_ResolutionIndex";
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

    // helper property to fetch the best available camera safely
    private Transform MainCam => cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);

    private void Awake()
    {
        // singleton pattern initialization
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
            return;
        }

        // audio source safety check
        if (bgmAudioSource != null && uiAndSfxSource != null && bgmAudioSource == uiAndSfxSource)
        {
            Debug.LogError("CRITICAL: Your BGM and UI Audio Sources are assigned to the exact same component! This will crash Unity. Please add a second Audio Source component and separate them.");
            uiAndSfxSource = null; 
        }
    }

    private void Start()
    {
        // ensure time scale is normal on load
        Time.timeScale = 1f; 
        
        // hide specific ui elements on start
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (waveText != null) waveText.gameObject.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // set up pause menu defaults
        if (pauseMenuContainer != null) pauseMenuContainer.SetActive(true);
        if (pauseSettingsContainer != null) pauseSettingsContainer.SetActive(false);

        activeSlot = PlayerPrefs.GetInt(prefSlot, 1);
        UpdateUI();

        WirePauseMenuAudio(); 
        InitializeResolutionDropdown(); 
        LoadAudioSettings(); 

        // start background music
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
    }

    private void Update()
    {
        // listen for pause input
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused && pauseSettingsContainer.activeSelf)
            {
                ClosePauseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    // pause & settings navigation

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

    // video / resolution settings

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;
        int savedResIndex = PlayerPrefs.GetInt(prefResolution, -1);

        // populate dropdown with available screen resolutions
        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");

            if (savedResIndex == -1 && 
                resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        if (savedResIndex != -1) currentResIndex = savedResIndex;

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        PlayButtonClickSound();
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        PlayerPrefs.SetInt(prefResolution, resolutionIndex);
        PlayerPrefs.Save();
    }

    // -----------------------------------------------------------
    // audio settings & sound fx trigger methods
    // -----------------------------------------------------------

    private void WirePauseMenuAudio()
    {
        // attach listener events to ui sliders dynamically
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
        
        // convert linear slider value to logarithmic decibels
        float targetDb = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, targetDb);
    }

    private void SaveAudioSettingsToDisk()
    {
        if (musicSlider != null) PlayerPrefs.SetFloat(prefMusic, musicSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat(prefSfx, sfxSlider.value);
        if (uiSlider != null) PlayerPrefs.SetFloat(prefUi, uiSlider.value);
        PlayerPrefs.Save();
    }

    public void PlayButtonClickSound()
    {
        if (uiAndSfxSource != null && buttonClickSound != null)
        {
            uiAndSfxSource.PlayOneShot(buttonClickSound);
        }
    }

    private void PlaySliderTickSound()
    {
        // limit the rate of slider tick sounds to prevent audio clipping
        if (Time.unscaledTime >= nextSliderSoundTime && uiAndSfxSource != null && sliderTickSound != null)
        {
            uiAndSfxSource.PlayOneShot(sliderTickSound);
            nextSliderSoundTime = Time.unscaledTime + 0.06f; 
        }
    }

    // -----------------------------------------------------------
    // space invaders core logics & cinematics
    // -----------------------------------------------------------

    public void AnnounceNewWave(int waveNumber)
    {
        if (uiAndSfxSource != null && newWaveSound != null)
        {
            uiAndSfxSource.PlayOneShot(newWaveSound);
        }

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

        // brief pause before executing life deduction
        yield return new WaitForSeconds(0.15f);

        playerLives--;
        UpdateUI();

        if (playerLives <= 0)
        {
            if (uiAndSfxSource != null && playerExplosionSound != null)
            {
                uiAndSfxSource.PlayOneShot(playerExplosionSound);
            }

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

        // rapidly shift the camera position
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
        // cleanly halt camera shaking
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

        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }

        // freeze the game environment
        Time.timeScale = 0f; 
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (uiAndSfxSource != null && gameOverSound != null)
        {
            uiAndSfxSource.PlayOneShot(gameOverSound);
        }
        
        // calculate currency reward
        int creditsEarned = currentScore / pointsPerCredit;
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";
        currentCredits = PlayerPrefs.GetInt(creditsKey, 500);
        currentCredits += creditsEarned;
        
        PlayerPrefs.SetInt(creditsKey, currentCredits);
        PlayerPrefs.Save();

        if (finalScoreText != null) finalScoreText.text = $"Final Score: {currentScore}";
        if (creditsEarnedText != null) 
        {
            creditsEarnedText.text = $"Credits Earned: +{creditsEarned}\nNew Balance: {currentCredits}";
        }
        
        // execute smooth fade in using unscaled delta time
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            CanvasGroup goCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (goCanvasGroup == null)
            {
                goCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }
            
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
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";
        currentCredits = PlayerPrefs.GetInt(creditsKey, 500);

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