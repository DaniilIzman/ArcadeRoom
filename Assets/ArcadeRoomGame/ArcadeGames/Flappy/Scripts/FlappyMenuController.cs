using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class FlappyScoreEntry   { public int attemptNumber; public string date; public int score; }
[System.Serializable]
public class FlappyLeaderboard  { public List<FlappyScoreEntry> entries = new List<FlappyScoreEntry>(); }

public class FlappyMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject personalBestPanel;
    public GameObject settingsPanel;

    [Header("Personal Best UI")]
    public TextMeshProUGUI leaderboardText;

    [Header("Audio Settings")]
    public AudioMixerGroup uiMixerGroup; // routes local UI AudioSource only
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider uiSlider;

    [Header("Scene Routing")]
    public string gameSceneName       = "FlappyLevel";
    public string arcadeRoomSceneName = "ArcadeRoom";

    [Header("Audio Feedback - Local")]
    public AudioSource uiAudioSource;
    public AudioClip   clickSound;
    public AudioClip   sliderTickSound;

    private const string SlotKey = "Global_LastPlayedSlot";
    private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;
    private int   activeSlot;

    private void Start()
    {
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
        activeSlot = PlayerPrefs.GetInt(SlotKey, 1);

        ToggleMenuPanels(true, false, false);
        WireMenuAudio();
        LoadSliders();
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

    public void StartGame()    => SceneManager.LoadScene(gameSceneName);
    public void OpenSettings() => ToggleMenuPanels(false, false, true);

    public void OpenPersonalBest() { ToggleMenuPanels(false, true, false); LoadAndDisplayLeaderboard(); }

    public void ReturnToMainMenu() { SettingsManager.Instance?.SaveAll(); ToggleMenuPanels(true, false, false); }

    public void LeaveArcadeMachine() { SettingsManager.Instance?.SaveAll(); SceneManager.LoadScene(arcadeRoomSceneName); }

    private void ToggleMenuPanels(bool main, bool pb, bool settings)
    {
        if (mainPanel)         mainPanel.SetActive(main);
        if (personalBestPanel) personalBestPanel.SetActive(pb);
        if (settingsPanel)     settingsPanel.SetActive(settings);
    }

    private void LoadAndDisplayLeaderboard()
    {
        string json = PlayerPrefs.GetString($"FlappyHistory_Slot{activeSlot}", "");
        if (string.IsNullOrEmpty(json)) { if (leaderboardText) leaderboardText.text = "NO FLIGHT DATA FOUND.\n\nINSERT COIN TO PLAY!"; return; }

        FlappyLeaderboard board = JsonUtility.FromJson<FlappyLeaderboard>(json);
        string display = "";
        for (int i = board.entries.Count - 1; i >= 0; i--)
        {
            var e = board.entries[i];
            display += $"#{e.attemptNumber} - {e.date} - <color=#FFD700>{e.score} PTS</color>\n";
        }
        if (leaderboardText != null) leaderboardText.text = display;
    }

    public void ClearFlightLog()
    {
        PlayerPrefs.DeleteKey($"FlappyHistory_Slot{activeSlot}");
        PlayerPrefs.Save();
        LoadAndDisplayLeaderboard();
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

        ConfigureMenuSlider(musicSlider, v => SettingsManager.Instance?.SetMusicVolume(v));
        ConfigureMenuSlider(sfxSlider,   v => SettingsManager.Instance?.SetSFXVolume(v));
        ConfigureMenuSlider(uiSlider,    v => SettingsManager.Instance?.SetUIVolume(v));

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(PlayClickSound); }
    }

    private void ConfigureMenuSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
        slider.onValueChanged.AddListener(_ => PlaySliderTick());
    }

    public void PlayClickSound() { if (uiAudioSource && clickSound) uiAudioSource.PlayOneShot(clickSound); }

    public void PlaySliderTick()
    {
        if (Time.unscaledTime - lastSliderSoundTime >= sliderSoundCooldown)
        {
            if (uiAudioSource && sliderTickSound) { uiAudioSource.PlayOneShot(sliderTickSound); lastSliderSoundTime = Time.unscaledTime; }
        }
    }
}