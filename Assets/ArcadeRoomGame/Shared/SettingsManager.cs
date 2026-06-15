using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using TMPro;
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // playerPrefs keys — shared across all scripts
    public const string KeyResW  = "Setting_ResWidth";
    public const string KeyResH  = "Setting_ResHeight";
    public const string KeyMusic = "Setting_MusicVol";
    public const string KeySFX   = "Setting_SFXVol";
    public const string KeyUI    = "Setting_UIVol";

    // resolutions below this will be hidden from the dropdown to prevent UI overflow
    public const int MinWidth  = 1024;
    public const int MinHeight = 576;

    [Header("Assign your shared AudioMixer asset here (replaces per-script mixer fields)")]
    public AudioMixer mainMixer;
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam   = "SFXVol";
    [SerializeField] private string uiParam    = "UIVol";

    private Resolution[] _resolutions;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildResolutionCache();
    }

    private void Start()
    {
        ApplySavedResolution();
        ApplySavedAudio();
    }

    // resolution

    private void BuildResolutionCache()
    {
        var seen = new HashSet<string>();
        var list = new List<Resolution>();
        foreach (var r in Screen.resolutions)
        {
            if (r.width < MinWidth || r.height < MinHeight) continue; // block sub-minimum
            if (seen.Add($"{r.width}x{r.height}")) list.Add(r);       // collapse refresh-rate dupes
        }
        _resolutions = list.ToArray();
    }

    /// Fills a TMP_Dropdown with valid resolutions, selects the saved one,
    /// and wires ApplyResolution to onValueChanged automatically.
    /// 
    public void PopulateDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null || _resolutions == null) return;

        // Use Screen.width/height (the actual window)
        int savedW = PlayerPrefs.GetInt(KeyResW, Screen.width);
        int savedH = PlayerPrefs.GetInt(KeyResH, Screen.height);
        int match  = 0;

        var labels = new List<string>();
        for (int i = 0; i < _resolutions.Length; i++)
        {
            labels.Add($"{_resolutions[i].width} x {_resolutions[i].height}");
            if (_resolutions[i].width == savedW && _resolutions[i].height == savedH)
                match = i;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(labels);
        dropdown.SetValueWithoutNotify(match);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(ApplyResolution);
    }

    /// Applies and persists the resolution at the given index.
    public void ApplyResolution(int index)
    {
        if (_resolutions == null || index < 0 || index >= _resolutions.Length) return;
        var r = _resolutions[index];
        // FullScreenWindow (borderless) keeps the Windows compositor active, which prevents
        // the brightness/gamma shift that ExclusiveFullScreen causes on some GPU+driver combos.
        Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
        Canvas.ForceUpdateCanvases();
        PlayerPrefs.SetInt(KeyResW, r.width);
        PlayerPrefs.SetInt(KeyResH, r.height);
        PlayerPrefs.Save();
    }

    /// restores the last saved resolution (clamped to minimum)
    public void ApplySavedResolution()
    {
        int w = Mathf.Max(PlayerPrefs.GetInt(KeyResW, Screen.width),  MinWidth);
        int h = Mathf.Max(PlayerPrefs.GetInt(KeyResH, Screen.height), MinHeight);
        Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
    }

    // ── Audio ─────────────────────────────────────────────────────────────────

    public float SavedMusicVol => PlayerPrefs.GetFloat(KeyMusic, 0.75f);
    public float SavedSFXVol   => PlayerPrefs.GetFloat(KeySFX,   0.75f);
    public float SavedUIVol    => PlayerPrefs.GetFloat(KeyUI,    0.75f);

    public void SetMusicVolume(float val) { ApplyVolume(musicParam, val); PlayerPrefs.SetFloat(KeyMusic, val); }
    public void SetSFXVolume(float val)   { ApplyVolume(sfxParam,   val); PlayerPrefs.SetFloat(KeySFX,   val); }
    public void SetUIVolume(float val)    { ApplyVolume(uiParam,    val); PlayerPrefs.SetFloat(KeyUI,    val); }

    /// Re-pushes saved volumes to the mixer (useful when a scene first loads).
    public void ApplySavedAudio()
    {
        ApplyVolume(musicParam, SavedMusicVol);
        ApplyVolume(sfxParam,   SavedSFXVol);
        ApplyVolume(uiParam,    SavedUIVol);
    }

    private void ApplyVolume(string param, float linear)
    {
        if (mainMixer == null) return;
        mainMixer.SetFloat(param, linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f);
    }

    public void SaveAll() => PlayerPrefs.Save();
}