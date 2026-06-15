using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class FlappyGameManager : MonoBehaviour
{
    public static FlappyGameManager Instance { get; private set; }

    [Header("In-Game UI")]
    public TextMeshProUGUI scoreText;

    [Header("Game Over UI")]
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

    [Header("Economy Settings")]
    public int pipesPerCredit = 3;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource audioSource;
    public AudioSource uiSource;

    [Header("Gameplay Audio Clips")]
    public AudioClip scoreSound;
    public AudioClip gameOverSound;

    [Header("UI Audio Clips")]
    public AudioClip clickSound;
    public AudioClip sliderSound;

    private const string SlotKey = "Global_LastPlayedSlot";

    public int  currentScore { get; private set; } = 0;
    public bool isGameOver   { get; private set; } = false;

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
        if (pausePanel)    pausePanel.SetActive(false);
        TogglePauseUIContainers(true, false);

        UpdateScoreUI();
        WirePauseMenuAudio();
        InitResDropdown();
        LoadSliders();

        lastSliderSoundTime = Time.unscaledTime; // arm cooldown after init
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused && pauseSettingsContainer && pauseSettingsContainer.activeSelf)
            { PlayClickAudio(); ClosePauseSettings(); }
            else TogglePause();
        }
    }

    // ── Pause ─────────────────────────────────────────────────────────────────

    public void TogglePause()
    {
        PlayClickAudio();
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel) pausePanel.SetActive(isPaused);
        if (isPaused) { TogglePauseUIContainers(true, false); LoadSliders(); }
        else          { SettingsManager.Instance?.SaveAll(); }
    }

    public void OpenPauseSettings()  { PlayClickAudio(); TogglePauseUIContainers(false, true); }

    public void ClosePauseSettings() { PlayClickAudio(); SettingsManager.Instance?.SaveAll(); TogglePauseUIContainers(true, false); }

    private void TogglePauseUIContainers(bool menuActive, bool settingsActive)
    {
        if (pauseMenuContainer)     pauseMenuContainer.SetActive(menuActive);
        if (pauseSettingsContainer) pauseSettingsContainer.SetActive(settingsActive);
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    private void InitResDropdown()
    {
        if (resolutionDropdown == null || !SettingsManager.Instance) return;
        SettingsManager.Instance.PopulateDropdown(resolutionDropdown);
        resolutionDropdown.onValueChanged.AddListener(_ => PlayClickAudio());
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

    private void WirePauseMenuAudio()
    {
        ConfigureSlider(musicSlider, v => SettingsManager.Instance?.SetMusicVolume(v));
        ConfigureSlider(sfxSlider,   v => SettingsManager.Instance?.SetSFXVolume(v));
        ConfigureSlider(uiSlider,    v => SettingsManager.Instance?.SetUIVolume(v));

        if (pausePanel != null)
        {
            foreach (var btn in pausePanel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(PlayClickAudio); }
        }
    }

    private void ConfigureSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
        slider.onValueChanged.AddListener(_ => PlaySliderTickAudio());
    }

    public void SetMusicVolume(float val) => SettingsManager.Instance?.SetMusicVolume(val);
    public void SetSFXVolume(float val)   => SettingsManager.Instance?.SetSFXVolume(val);
    public void SetUIVolume(float val)    => SettingsManager.Instance?.SetUIVolume(val);

    // ── Gameplay (unchanged) ──────────────────────────────────────────────────

    public void AddScore()
    {
        if (isGameOver) return;
        currentScore++;
        UpdateScoreUI();
        if (audioSource && scoreSound) audioSource.PlayOneShot(scoreSound);
    }

    private void UpdateScoreUI() { if (scoreText) scoreText.text = currentScore.ToString(); }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (musicSource) musicSource.Stop();
        if (audioSource && gameOverSound) audioSource.PlayOneShot(gameOverSound);

        int earned = currentScore / pipesPerCredit;
        SaveFlightData(earned);

        if (gameOverPanel)     gameOverPanel.SetActive(true);
        if (finalScoreText)    finalScoreText.text    = $"FINAL SCORE: {currentScore}";
        if (creditsEarnedText) creditsEarnedText.text = $"EARNED: {earned} CREDITS";
    }

    private void SaveFlightData(int creditsToAdd)
    {
        int slot = PlayerPrefs.GetInt(SlotKey, 1);
        string creditsKey = $"PlayerCredits_Slot{slot}";
        PlayerPrefs.SetInt(creditsKey, PlayerPrefs.GetInt(creditsKey, 0) + creditsToAdd);

        string prefsKey = $"FlappyHistory_Slot{slot}";
        FlappyLeaderboard board = new FlappyLeaderboard();
        string json = PlayerPrefs.GetString(prefsKey, "");
        if (!string.IsNullOrEmpty(json)) board = JsonUtility.FromJson<FlappyLeaderboard>(json);

        board.entries.Add(new FlappyScoreEntry
        {
            attemptNumber = board.entries.Count + 1,
            date          = System.DateTime.Now.ToString("MM/dd/yy HH:mm"),
            score         = currentScore
        });
        PlayerPrefs.SetString(prefsKey, JsonUtility.ToJson(board));
        PlayerPrefs.Save();
    }

    public void ResumeGame()   { PlayClickAudio(); if (isPaused) TogglePause(); }
    public void TryAgain()     { PlayClickAudio(); Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void ReturnToMenu() { PlayClickAudio(); Time.timeScale = 1f; SettingsManager.Instance?.SaveAll(); SceneManager.LoadScene("FlappyMenu"); }

    public void PlayClickAudio() { if (uiSource && clickSound) uiSource.PlayOneShot(clickSound); }

    private void PlaySliderTickAudio()
    {
        if (lastSliderSoundTime < 0f) return;
        if (Time.unscaledTime - lastSliderSoundTime >= sliderSoundCooldown)
        {
            if (uiSource && sliderSound)
            {
                uiSource.pitch = Random.Range(0.95f, 1.05f);
                uiSource.PlayOneShot(sliderSound);
                uiSource.pitch = 1f;
                lastSliderSoundTime = Time.unscaledTime;
            }
        }
    }
}