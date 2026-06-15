using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SpaceInvadersMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject scoreAdvancePanel;

    [Header("Economy Settings")]
    public TextMeshProUGUI creditsText;
    public string baseCreditsKey     = "PlayerCredits";
    public bool   debugGiveFreeCredits = false;

    [Header("Audio Settings")]
    public AudioMixerGroup uiMixerGroup; // routes the local UI AudioSource only
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;

    [Header("Scene Routing")]
    public string gameSceneName       = "SpaceInvadersLevel";
    public string arcadeRoomSceneName = "ArcadeRoom";

    [Header("Audio Feedback - Local")]
    public AudioSource uiAudioSource;
    public AudioClip   clickSound;
    public AudioClip   errorSound;
    public AudioClip   sliderTickSound;

    private const string SlotKey = "Global_LastPlayedSlot";
    private float  sliderSoundCooldown = 0.05f;
    private float  lastSliderSoundTime;
    private int    activeSlot;
    private int    currentCredits;
    private string creditsPrefsKey;

    private void Start()
    {
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        activeSlot      = PlayerPrefs.GetInt(SlotKey, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        if (debugGiveFreeCredits) { PlayerPrefs.SetInt(creditsPrefsKey, 500); PlayerPrefs.Save(); }

        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        scoreAdvancePanel.SetActive(false);

        WireMenuAudio();
        LoadSliders();
        RefreshCreditsUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf || scoreAdvancePanel.activeSelf)
            {
                ReturnToMainMenu();
                PlayClickSound();
            }
        }
    }

    public void AttemptStartGame() { PlayClickSound(); SceneManager.LoadScene(gameSceneName); }
    public void OpenScoreAdvanceTable() => TogglePanels(false, false, true);
    public void OpenSettings()           => TogglePanels(false, true, false);

    public void ReturnToMainMenu()
    {
        SettingsManager.Instance?.SaveAll();
        TogglePanels(true, false, false);
    }

    public void LeaveArcadeMachine()
    {
        SettingsManager.Instance?.SaveAll();
        SceneManager.LoadScene(arcadeRoomSceneName);
    }

    private void TogglePanels(bool main, bool settings, bool scoreAdvance)
    {
        if (mainPanel)         mainPanel.SetActive(main);
        if (settingsPanel)     settingsPanel.SetActive(settings);
        if (scoreAdvancePanel) scoreAdvancePanel.SetActive(scoreAdvance);
    }

    private void RefreshCreditsUI()
    {
        currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0);
        if (creditsText != null) creditsText.text = $"CREDITS: {currentCredits}";
    }

    private void LoadSliders()
    {
        if (!SettingsManager.Instance) return;
        if (musicSlider) musicSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedMusicVol);
        if (sfxSlider)   sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedSFXVol);
        if (uiSlider)    uiSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedUIVol);
    }

    private void WireMenuAudio()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake        = false;
            uiAudioSource.ignoreListenerPause = true;
        }
        if (uiMixerGroup != null) uiAudioSource.outputAudioMixerGroup = uiMixerGroup;

        ConfigureSlider(musicSlider, v => SettingsManager.Instance?.SetMusicVolume(v));
        ConfigureSlider(sfxSlider,   v => SettingsManager.Instance?.SetSFXVolume(v));
        ConfigureSlider(uiSlider,    v => SettingsManager.Instance?.SetUIVolume(v));

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn.gameObject.name == "PlayButton") continue;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    private void ConfigureSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
        slider.onValueChanged.AddListener(_ => PlaySliderTick());
    }

    public void PlayClickSound()
    {
        if (uiAudioSource != null && clickSound != null) uiAudioSource.PlayOneShot(clickSound);
    }

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
}