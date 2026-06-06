using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// --- DATA CONTAINERS FOR OUR LEADERBOARD ---
[System.Serializable]
public class FlappyScoreEntry
{
    public int attemptNumber;
    public string date;
    public int score;
}

[System.Serializable]
public class FlappyLeaderboard
{
    public List<FlappyScoreEntry> entries = new List<FlappyScoreEntry>();
}
// -------------------------------------------

public class FlappyMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject personalBestPanel;
    public GameObject settingsPanel;

    [Header("Personal Best UI")]
    public TextMeshProUGUI leaderboardText;

    [Header("Audio Settings & Routing")]
    public AudioMixer audioMixer;
    public AudioMixerGroup uiMixerGroup; 
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider; 

    [Header("Scene Routing")]
    public string gameSceneName = "FlappyLevel";
    public string arcadeRoomSceneName = "ArcadeRoom"; 

    [Header("Audio Feedback - Local")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;
    public AudioClip sliderTickSound;
    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;

    private int activeSlot;

    private void Start()
    {
        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        mainPanel.SetActive(true);
        personalBestPanel.SetActive(false);
        settingsPanel.SetActive(false);

        WireMenuAudio(); // set up the dynamic runtime listeners
        LoadSettings();  // load the values and push them through those listeners
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf || personalBestPanel.activeSelf)
            {
                ReturnToMainMenu();
                PlayClickSound(); 
            }
            else if (mainPanel.activeSelf)
            {
                LeaveArcadeMachine();
            }
        }
    }

    // main navigation

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenPersonalBest()
    {
        mainPanel.SetActive(false);
        personalBestPanel.SetActive(true);

        LoadAndDisplayLeaderboard();
    }

    public void OpenSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        personalBestPanel.SetActive(false);
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        PlayerPrefs.Save(); 
    }

    public void LeaveArcadeMachine()
    {
        SceneManager.LoadScene(arcadeRoomSceneName);
    }

    // leaderboard logic

    private void LoadAndDisplayLeaderboard()
    {
        string json = PlayerPrefs.GetString($"FlappyHistory_Slot{activeSlot}", "");

        if (string.IsNullOrEmpty(json))
        {
            leaderboardText.text = "NO FLIGHT DATA FOUND.\n\nINSERT COIN TO PLAY!";
            return;
        }

        FlappyLeaderboard board = JsonUtility.FromJson<FlappyLeaderboard>(json);
        string displayText = "FLIGHT LOG\n<size=70%>Try | Date | Score</size>\n\n";

        for (int i = board.entries.Count - 1; i >= 0; i--)
        {
            FlappyScoreEntry entry = board.entries[i];
            displayText += $"#{entry.attemptNumber} - {entry.date} - <color=#FFD700>{entry.score} PTS</color>\n";
        }

        if (leaderboardText != null)
        {
            leaderboardText.text = displayText;
        }
    }
    // settings: music, ui, sfx

    private void LoadSettings()
    {
        // pull down the global values seamlessly
        if (musicSlider) musicSlider.value = PlayerPrefs.GetFloat("Setting_MusicVol", 0.75f);
        if (sfxSlider) sfxSlider.value = PlayerPrefs.GetFloat("Setting_SFXVol", 0.75f);
        if (uiSlider) uiSlider.value = PlayerPrefs.GetFloat("Setting_UIVol", 0.75f);

        // force an evaluation to make sure the mixer is perfectly matched up on startup
        SetMusicVolume(musicSlider ? musicSlider.value : 0.75f);
        SetSFXVolume(sfxSlider ? sfxSlider.value : 0.75f);
        SetUIVolume(uiSlider ? uiSlider.value : 0.75f);
    }

    public void SetMusicVolume(float val)
    {
        ApplyVolumeToMixer("MusicVol", val);
        PlayerPrefs.SetFloat("Setting_MusicVol", val);
    }

    public void SetSFXVolume(float val)
    {
        ApplyVolumeToMixer("SFXVol", val);
        PlayerPrefs.SetFloat("Setting_SFXVol", val);
    }

    public void SetUIVolume(float val)
    {
        ApplyVolumeToMixer("UIVol", val);
        PlayerPrefs.SetFloat("Setting_UIVol", val);
    }

    private void ApplyVolumeToMixer(string parameterName, float sliderValue)
    {
        if (audioMixer == null) return;
        
        if (sliderValue <= 0.0001f) 
        {
            audioMixer.SetFloat(parameterName, -80f); 
        }
        else 
        {
            audioMixer.SetFloat(parameterName, Mathf.Log10(sliderValue) * 20f);
        }
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

        if (uiMixerGroup != null)
        {
            uiAudioSource.outputAudioMixerGroup = uiMixerGroup;
        }

        if (musicSlider)
        {
            musicSlider.onValueChanged.RemoveAllListeners(); // clear any editor runtime pollution
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

        // auto-wire local click sounds to all canvas buttons
        Button[] menuButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in menuButtons)
        {
            btn.onClick.RemoveAllListeners(); // safe clean baseline
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    public void PlayClickSound() 
    { 
        if (uiAudioSource != null && clickSound != null) 
        {
            uiAudioSource.PlayOneShot(clickSound); 
        }
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