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

    // playerpref string keys cached to prevent typos and optimize lookups
    private const string MusicVolKey = "Setting_MusicVol";
    private const string SfxVolKey = "Setting_SFXVol";
    private const string UiVolKey = "Setting_UIVol";
    private const string SlotKey = "Global_LastPlayedSlot";

    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;
    private int activeSlot;
    private int currentCredits;
    private string creditsPrefsKey;

    private void Start()
    {
        // configure initial cursor state for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // resolve save slots and runtime credit keys
        activeSlot = PlayerPrefs.GetInt(SlotKey, 1);
        creditsPrefsKey = $"PlayerCredits_Slot{activeSlot}";
        
        // ensure warning layouts are hidden cleanly on bootup
        if (insufficientCreditsWarningText) insufficientCreditsWarningText.gameObject.SetActive(false);

        // establish baseline panel visibility state
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        scoreAdvancePanel.SetActive(false);

        // run system initializations
        WireMenuAudio(); 
        LoadSettings();  
        RefreshCreditsUI();
    }

    private void Update()
    {
        // process backward context escape navigation safely
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf || scoreAdvancePanel.activeSelf)
            {
                ReturnToMainMenu();
                PlayClickSound(); 
            }
        }
    }

    private void RefreshCreditsUI()
    {
        // load player balance based on calculated active key profile
        currentCredits = PlayerPrefs.GetInt(creditsPrefsKey, 500); 
        
        if (creditsText != null)
        {
            creditsText.text = $"CREDITS: {currentCredits}";
        }
    }

    public void AttemptStartGame()
    {
        // validate player transactional balance before switching scenes
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
            // fallback processing for broken/insufficient transaction attempts
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
        // modular utility handler designed to minimize boilerplate panel state toggles
        if (mainPanel) mainPanel.SetActive(main);
        if (settingsPanel) settingsPanel.SetActive(settings);
        if (scoreAdvancePanel) scoreAdvancePanel.SetActive(scoreAdvance);
    }

    private void LoadSettings()
    {
        // acquire saved volume float records seamlessly or leverage original defaults
        float targetMusic = PlayerPrefs.GetFloat(MusicVolKey, 0.75f);
        float targetSfx = PlayerPrefs.GetFloat(SfxVolKey, 0.75f);
        float targetUi = PlayerPrefs.GetFloat(UiVolKey, 0.75f);

        // push saved values cleanly out onto current inspector ui structures
        if (musicSlider) musicSlider.value = targetMusic;
        if (sfxSlider) sfxSlider.value = targetSfx;
        if (uiSlider) uiSlider.value = targetUi;

        // bind volume configuration settings directly out onto the active system mixer channel profiles
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
        
        // apply dynamic logarithmic conversions out onto matching targeted mixer parameter nodes
        float targetDb = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, targetDb);
    }

    private void SaveAudioSettingsToDisk()
    {
        // verify references exist safely before executing registry update requests
        if (musicSlider) PlayerPrefs.SetFloat(MusicVolKey, musicSlider.value);
        if (sfxSlider) PlayerPrefs.SetFloat(SfxVolKey, sfxSlider.value);
        if (uiSlider) PlayerPrefs.SetFloat(UiVolKey, uiSlider.value);
        PlayerPrefs.Save();
    }

    private void WireMenuAudio()
    {
        // lazy initialize target menu execution source dependencies safely when needed
        if (uiAudioSource == null) 
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.ignoreListenerPause = true; 
        }

        if (uiMixerGroup != null) uiAudioSource.outputAudioMixerGroup = uiMixerGroup;

        // programmatically bind unified change event pipelines to user slider layouts
        ConfigureSliderListener(musicSlider, SetMusicVolume);
        ConfigureSliderListener(sfxSlider, SetSFXVolume);
        ConfigureSliderListener(uiSlider, SetUIVolume);

        // intercept global children array components to cleanly route shared click audio responses
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
        // universal engine routing tool configured to avoid boilerplate event binding overhead
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
        // basic time check constraint used to keep sequential slider sounds tracking cleanly
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