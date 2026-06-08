using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private const string MusicVolKey = "Setting_MusicVol";
    private const string SfxVolKey = "Setting_SFXVol";
    private const string UiVolKey = "Setting_UIVol";
    private const string SlotKey = "Global_LastPlayedSlot";

    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;
    private int activeSlot;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        activeSlot = PlayerPrefs.GetInt(SlotKey, 1);

        ToggleMenuPanels(true, false, false);
        WireMenuAudio(); 
        LoadSettings();  
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
        }
    }

    public void StartGame() => SceneManager.LoadScene(gameSceneName);

    public void OpenPersonalBest()
    {
        ToggleMenuPanels(false, true, false);
        LoadAndDisplayLeaderboard();
    }

    public void OpenSettings() => ToggleMenuPanels(false, false, true);

    public void ReturnToMainMenu()
    {
        ToggleMenuPanels(true, false, false);
        SaveAudioSettingsToDisk(); 
    }

    public void LeaveArcadeMachine()
    {
        SaveAudioSettingsToDisk(); 
        SceneManager.LoadScene(arcadeRoomSceneName);
    }

    private void ToggleMenuPanels(bool main, bool pb, bool settings)
    {
        if (mainPanel) mainPanel.SetActive(main);
        if (personalBestPanel) personalBestPanel.SetActive(pb);
        if (settingsPanel) settingsPanel.SetActive(settings);
    }

    private void LoadAndDisplayLeaderboard()
    {
        string json = PlayerPrefs.GetString($"FlappyHistory_Slot{activeSlot}", "");

        if (string.IsNullOrEmpty(json))
        {
            if (leaderboardText) leaderboardText.text = "NO FLIGHT DATA FOUND.\n\nINSERT COIN TO PLAY!";
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

    public void ClearFlightLog()
    {
        PlayerPrefs.DeleteKey($"FlappyHistory_Slot{activeSlot}");
        PlayerPrefs.Save();
        LoadAndDisplayLeaderboard();
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

        ConfigureMenuSlider(musicSlider, SetMusicVolume);
        ConfigureMenuSlider(sfxSlider, SetSFXVolume);
        ConfigureMenuSlider(uiSlider, SetUIVolume);

        Button[] menuButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in menuButtons)
        {
            btn.onClick.RemoveAllListeners(); 
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    private void ConfigureMenuSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
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