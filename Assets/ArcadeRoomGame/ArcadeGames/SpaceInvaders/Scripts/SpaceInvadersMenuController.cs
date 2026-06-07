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
    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;

    private int activeSlot;
    private int currentCredits;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        
        // hide warnings on start
        if (insufficientCreditsWarningText) insufficientCreditsWarningText.gameObject.SetActive(false);

        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        scoreAdvancePanel.SetActive(false);

        WireMenuAudio(); 
        LoadSettings();  
        RefreshCreditsUI();
    }

    private void Update()
    {
        // safe backward navigation using Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf || scoreAdvancePanel.activeSelf)
            {
                ReturnToMainMenu();
                PlayClickSound(); 
            }
        }
    }

    // main navigation & economy

    private void RefreshCreditsUI()
    {
        string creditsKey = $"PlayerCredits_Slot{activeSlot}";
        currentCredits = PlayerPrefs.GetInt(creditsKey, 500); // defaults to 500
        
        if (creditsText != null)
        {
            creditsText.text = $"CREDITS: {currentCredits}";
        }
    }

    public void AttemptStartGame()
    {
        if (currentCredits >= costPerPlay)
        {
            // deduct the cost and save it to the global wallet!
            currentCredits -= costPerPlay;
            PlayerPrefs.SetInt($"PlayerCredits_Slot{activeSlot}", currentCredits);
            PlayerPrefs.Save();
            
            // start the game
            PlayClickSound();
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // not enough money
            if (uiAudioSource && errorSound) uiAudioSource.PlayOneShot(errorSound);
            
            if (insufficientCreditsWarningText)
            {
                insufficientCreditsWarningText.text = $"INSERT COIN! ({costPerPlay} REQ)";
                insufficientCreditsWarningText.gameObject.SetActive(true);
            }
        }
    }

    public void OpenScoreAdvanceTable()
    {
        mainPanel.SetActive(false);
        scoreAdvancePanel.SetActive(true);
    }

    public void OpenSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        scoreAdvancePanel.SetActive(false);
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        
        if (insufficientCreditsWarningText) insufficientCreditsWarningText.gameObject.SetActive(false);
        SaveAudioSettingsToDisk(); 
    }

    public void LeaveArcadeMachine()
    {
        SaveAudioSettingsToDisk(); 
        SceneManager.LoadScene(arcadeRoomSceneName);
    }

    // audio settings (Recycled from Flappy)

    private void LoadSettings()
    {
        if (musicSlider) musicSlider.value = PlayerPrefs.GetFloat("Setting_MusicVol", 0.75f);
        if (sfxSlider) sfxSlider.value = PlayerPrefs.GetFloat("Setting_SFXVol", 0.75f);
        if (uiSlider) uiSlider.value = PlayerPrefs.GetFloat("Setting_UIVol", 0.75f);

        SetMusicVolume(musicSlider ? musicSlider.value : 0.75f);
        SetSFXVolume(sfxSlider ? sfxSlider.value : 0.75f);
        SetUIVolume(uiSlider ? uiSlider.value : 0.75f);
    }

    public void SetMusicVolume(float val) => ApplyVolumeToMixer("MusicVol", val);
    public void SetSFXVolume(float val) => ApplyVolumeToMixer("SFXVol", val);
    public void SetUIVolume(float val) => ApplyVolumeToMixer("UIVol", val);

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (audioMixer == null) return;
        
        if (sliderValue <= 0.0001f) audioMixer.SetFloat(parameterName, -80f); 
        else audioMixer.SetFloat(parameterName, Mathf.Log10(sliderValue) * 20f);
    }

    private void SaveAudioSettingsToDisk()
    {
        if (musicSlider) PlayerPrefs.SetFloat("Setting_MusicVol", musicSlider.value);
        if (sfxSlider) PlayerPrefs.SetFloat("Setting_SFXVol", sfxSlider.value);
        if (uiSlider) PlayerPrefs.SetFloat("Setting_UIVol", uiSlider.value);
        PlayerPrefs.Save();
    }

    // local audio feedback
    private void WireMenuAudio()
    {
        if (uiAudioSource == null) 
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.ignoreListenerPause = true; 
        }

        if (uiMixerGroup != null) uiAudioSource.outputAudioMixerGroup = uiMixerGroup;

        if (musicSlider)
        {
            musicSlider.onValueChanged.RemoveAllListeners(); 
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener((val) => PlaySliderTick());
        }
        if (sfxSlider)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener((val) => PlaySliderTick());
        }
        if (uiSlider)
        {
            uiSlider.onValueChanged.RemoveAllListeners();
            uiSlider.onValueChanged.AddListener(SetUIVolume);
            uiSlider.onValueChanged.AddListener((val) => PlaySliderTick());
        }

        Button[] menuButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in menuButtons)
        {
            if (btn.gameObject.name == "PlayButton") continue; 
            btn.onClick.RemoveAllListeners(); 
            btn.onClick.AddListener(PlayClickSound);
        }
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