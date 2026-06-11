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
    public int costPerPlay = 10;
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI insufficientCreditsWarningText; 
    [Tooltip("Must perfectly match the string your Arcade Room uses to save money.")]
    public string baseCreditsKey = "PlayerCredits";
    [Tooltip("Enable this to give yourself 500 free credits purely for testing the Menu logic.")]
    public bool debugGiveFreeCredits = false;

    [Header("Audio Settings & Routing")]
    public AudioMixer audioMixer;
    public AudioMixerGroup uiMixerGroup; 
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider; 

    [Header("Scene Routing")]
    public string gameSceneName = "SpaceInvadersLevel";
    public string arcadeRoomSceneName = "ArcadeRoom"; 

    [Header("Audio Feedback - Local")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;
    public AudioClip errorSound;
    public AudioClip sliderTickSound;

    private const string MusicVolKey = "Setting_MusicVol";
    private const string SfxVolKey = "Setting_SFXVol";
    private const string UiVolKey = "Setting_UIVol";
    private const string SlotKey = "Global_LastPlayedSlot";
    
    // Changing how Menu forces screen limits
    private const string prefResWidth = "Setting_ResWidth";
    private const string prefResHeight = "Setting_ResHeight";

    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;
    private int activeSlot;
    private int currentCredits;
    private string creditsPrefsKey;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        activeSlot = PlayerPrefs.GetInt(SlotKey, 1);
        creditsPrefsKey = $"{baseCreditsKey}_Slot{activeSlot}";

        // Developer override so you can test without playing Arcade Room first
        if (debugGiveFreeCredits)
        {
            PlayerPrefs.SetInt(creditsPrefsKey, 500);
            PlayerPrefs.Save();
        }
        
        if (insufficientCreditsWarningText) insufficientCreditsWarningText.gameObject.SetActive(false);

        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        scoreAdvancePanel.SetActive(false);

        ApplySavedResolution();
        WireMenuAudio(); 
        LoadSettings();  
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

    private void ApplySavedResolution()
    {
        // Enforce the layout resolution saved by the GameManager
        int w = PlayerPrefs.GetInt(prefResWidth, Screen.currentResolution.width);
        int h = PlayerPrefs.GetInt(prefResHeight, Screen.currentResolution.height);
        Screen.SetResolution(w, h, Screen.fullScreen);
    }

    private void RefreshCreditsUI()
    {
        currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 0); 
        if (creditsText != null) creditsText.text = $"CREDITS: {currentCredits}";
    }

    public void AttemptStartGame()
    {
        if (currentCredits >= costPerPlay)
        {
            currentCredits -= costPerPlay;
            PlayerPrefs.SetInt(creditsPrefsKey, currentCredits);
            PlayerPrefs.Save();
            
            PlayClickSound();
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            if (uiAudioSource && errorSound) uiAudioSource.PlayOneShot(errorSound);
            
            if (insufficientCreditsWarningText)
            {
                insufficientCreditsWarningText.text = $"INSERT COIN! ({costPerPlay} REQ)";
                insufficientCreditsWarningText.gameObject.SetActive(true);
            }
        }
    }

    public void OpenScoreAdvanceTable() => TogglePanels(false, false, true);
    public void OpenSettings() => TogglePanels(false, true, false);

    public void ReturnToMainMenu()
    {
        TogglePanels(true, false, false);
        if (insufficientCreditsWarningText) insufficientCreditsWarningText.gameObject.SetActive(false);
        SaveAudioSettingsToDisk(); 
    }

    public void LeaveArcadeMachine()
    {
        SaveAudioSettingsToDisk(); 
        SceneManager.LoadScene(arcadeRoomSceneName);
    }

    private void TogglePanels(bool main, bool settings, bool scoreAdvance)
    {
        if (mainPanel) mainPanel.SetActive(main);
        if (settingsPanel) settingsPanel.SetActive(settings);
        if (scoreAdvancePanel) scoreAdvancePanel.SetActive(scoreAdvance);
    }

    private void LoadSettings()
    {
        float targetMusic = PlayerPrefs.GetFloat(MusicVolKey, 0.75f);
        float targetSfx = PlayerPrefs.GetFloat(SfxVolKey, 0.75f);
        float targetUi = PlayerPrefs.GetFloat(UiVolKey, 0.75f);

        if (musicSlider) musicSlider.value = targetMusic;
        if (sfxSlider) sfxSlider.value = targetSfx;
        if (uiSlider) uiSlider.value = targetUi;

        SetMusicVolume(targetMusic);
        SetSFXVolume(targetSfx);
        SetUIVolume(targetUi);
    }

    public void SetMusicVolume(float val) => ApplyVolumeToMixer("MusicVol", val);
    public void SetSFXVolume(float val) => ApplyVolumeToMixer("SFXVol", val);
    public void SetUIVolume(float val) => ApplyVolumeToMixer("UIVol", val);

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (audioMixer == null) return;
        float targetDb = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, targetDb);
    }

    private void SaveAudioSettingsToDisk()
    {
        if (musicSlider) PlayerPrefs.SetFloat(MusicVolKey, musicSlider.value);
        if (sfxSlider) PlayerPrefs.SetFloat(SfxVolKey, sfxSlider.value);
        if (uiSlider) PlayerPrefs.SetFloat(UiVolKey, uiSlider.value);
        PlayerPrefs.Save();
    }

    private void WireMenuAudio()
    {
        if (uiAudioSource == null) 
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.ignoreListenerPause = true; 
        }

        if (uiMixerGroup != null) uiAudioSource.outputAudioMixerGroup = uiMixerGroup;

        ConfigureSliderListener(musicSlider, SetMusicVolume);
        ConfigureSliderListener(sfxSlider, SetSFXVolume);
        ConfigureSliderListener(uiSlider, SetUIVolume);

        Button[] menuButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in menuButtons)
        {
            if (btn.gameObject.name == "PlayButton") continue; 
            btn.onClick.RemoveAllListeners(); 
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    private void ConfigureSliderListener(Slider slider, UnityEngine.Events.UnityAction<float> volumeAction)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(volumeAction);
        slider.onValueChanged.AddListener((val) => PlaySliderTick());
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