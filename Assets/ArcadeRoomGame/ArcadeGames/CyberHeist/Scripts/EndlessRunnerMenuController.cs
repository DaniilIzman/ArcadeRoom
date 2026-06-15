using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndlessRunnerMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Economy Settings")]
    public TextMeshProUGUI creditsText;
    public string baseCreditsKey = "PlayerCredits";

    [Header("Settings UI")]
    public Slider       musicSlider;
    public Slider       sfxSlider;
    public Slider       uiSlider;
    public TMP_Dropdown resolutionDropdown;

    [Header("Audio Feedback")]
    public AudioSource uiAudioSource;
    public AudioClip   clickSound;
    public AudioClip   sliderTickSound;

    [Header("Scene Routing")]
    public string gameSceneName       = "EndlessRunnerLevel";
    public string arcadeRoomSceneName = "ArcadeRoom";

    private const string prefSlot = "Global_LastPlayedSlot";

    private int    activeSlot;
    private int    currentCredits;
    private string creditsPrefsKey;
    private float  nextSliderSoundTime = 0f;

    private void Start()
    {
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        activeSlot      = PlayerPrefs.GetInt(prefSlot, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        TogglePanels(true, false);
        InitResDropdown();
        WireMenuAudio();
        LoadSliders();
        RefreshCreditsUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel.activeSelf) ReturnToMainMenu();
    }

    private void RefreshCreditsUI()
    {
        currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0);
        if (creditsText != null) creditsText.text = $"CREDITS: {currentCredits}";
    }

    public void AttemptStartGame() { PlayClickSound(); SceneManager.LoadScene(gameSceneName); }

    public void OpenSettings()     { PlayClickSound(); TogglePanels(false, true); }

    public void ReturnToMainMenu() { PlayClickSound(); SettingsManager.Instance?.SaveAll(); TogglePanels(true, false); }

    public void LeaveArcadeMachine() { PlayClickSound(); SettingsManager.Instance?.SaveAll(); SceneManager.LoadScene(arcadeRoomSceneName); }

    private void TogglePanels(bool main, bool settings)
    {
        if (mainPanel)     mainPanel.SetActive(main);
        if (settingsPanel) settingsPanel.SetActive(settings);
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
    }

    private void WireMenuAudio()
    {
        if (musicSlider) { musicSlider.onValueChanged.RemoveAllListeners(); musicSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetMusicVolume(v)); musicSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
        if (sfxSlider)   { sfxSlider.onValueChanged.RemoveAllListeners();   sfxSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetSFXVolume(v));   sfxSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
        if (uiSlider)    { uiSlider.onValueChanged.RemoveAllListeners();    uiSlider.onValueChanged.AddListener(v => SettingsManager.Instance?.SetUIVolume(v));    uiSlider.onValueChanged.AddListener(_ => PlaySliderTickSound()); }
    }

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