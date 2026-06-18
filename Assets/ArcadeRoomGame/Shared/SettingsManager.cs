using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using TMPro;

// singleton that owns all game settings and persists them across scene loads
// auto-spawns from resources so it exists regardless of which scene is loaded first
// all other scripts should read and write settings through this class only
[DisallowMultipleComponent]
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // categories used to route audiosources to the correct mixer group
    public enum AudioCategory { Music, SFX, UI }

    // playerprefs keys used to save and load each setting
    private const string KeyResIndex    = "Settings_ResolutionIndex";
    private const string KeyMusic        = "Settings_MusicVolume";
    private const string KeySFX          = "Settings_SFXVolume";
    private const string KeyUI           = "Settings_UIVolume";
    private const string KeyBrightness   = "Settings_Brightness";

    // intentionally uses the legacy key name to stay compatible with existing playercamera code
    private const string KeySensitivity  = "Setting_MouseSensitivity";

    // the main mixer asset that all audio categories route through
    [Header("Audio Mixer")]
    public AudioMixer mainMixer;

    // individual mixer groups for each audio category
    [Header("Mixer Groups")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup uiGroup;

    // names of the exposed parameters in the mixer that correspond to each volume
    [Header("Exposed mixer parameter names")]
    [SerializeField] private string musicParam = "MusicVolume";
    [SerializeField] private string sfxParam   = "SFXVolume";
    [SerializeField] private string uiParam    = "UIVolume";

    // fullscreen mode applied when changing resolution
    [Header("Display")]
    [Tooltip("ExclusiveFullScreen avoids the borderless-window brightness shift at native resolution " +
             "and lets resolution changes truly switch the display mode. Switch to FullScreenWindow " +
             "if exclusive flickers or alt-tabs poorly on your hardware.")]
    public FullScreenMode fullScreenMode = FullScreenMode.ExclusiveFullScreen;

    // publicly readable current values for each setting, set only through the setter methods
    public float MusicVolume     { get; private set; } = 0.75f;
    public float SFXVolume       { get; private set; } = 0.75f;
    public float UIVolume        { get; private set; } = 0.75f;
    public float Brightness      { get; private set; } = 1f;
    public float Sensitivity     { get; private set; } = 2f;
    public int   ResolutionIndex { get; private set; }

    // event broadcast to any overlay or ui that needs to react when brightness changes
    public event System.Action<float> OnBrightnessChanged;

    // the fixed list of supported resolutions built at startup
    private Resolution[] _resolutions;
    public IReadOnlyList<Resolution> Resolutions => _resolutions;

    // creates the settings manager before any scene loads if it doesn't already exist
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<SettingsManager>("SettingsManager");
        if (prefab == null)
        {
            Debug.LogWarning("[SettingsManager] No prefab found at Resources/SettingsManager. " +
                             "Create one (with the mixer + groups assigned) so settings work " +
                             "when starting directly in any scene.");
            return;
        }
        Instantiate(prefab);
    }

    private void Awake()
    {
        // enforce singleton and persist across scene loads
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildResolutionList();
        Load();
    }

    private void Start()
    {
        // push loaded values to the mixer and display immediately on first frame
        ApplyAllAudio();
        ApplyResolution(ResolutionIndex, persist: false);
        OnBrightnessChanged?.Invoke(Brightness);
    }

    // reads all saved settings from playerprefs, falling back to defaults if missing
    private void Load()
    {
        MusicVolume     = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic,      0.75f));
        SFXVolume       = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySFX,        0.75f));
        UIVolume        = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyUI,         0.75f));
        Brightness      = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyBrightness, 1f));
        Sensitivity     = PlayerPrefs.GetFloat(KeySensitivity, 2f);
        ResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyResIndex, _resolutions.Length - 1),
                                      0, _resolutions.Length - 1);
    }

    // writes all current setting values to playerprefs and flushes to disk
    public void SaveAll()
    {
        PlayerPrefs.SetFloat(KeyMusic,       MusicVolume);
        PlayerPrefs.SetFloat(KeySFX,         SFXVolume);
        PlayerPrefs.SetFloat(KeyUI,          UIVolume);
        PlayerPrefs.SetFloat(KeyBrightness,  Brightness);
        PlayerPrefs.SetFloat(KeySensitivity, Sensitivity);
        PlayerPrefs.SetInt(KeyResIndex,      ResolutionIndex);
        PlayerPrefs.Save();
    }

    // individual setters that update the cached value and immediately apply it to the mixer
    public void SetMusicVolume(float v) { MusicVolume = Mathf.Clamp01(v); Apply(musicParam, MusicVolume); }
    public void SetSFXVolume(float v)   { SFXVolume   = Mathf.Clamp01(v); Apply(sfxParam,   SFXVolume);   }
    public void SetUIVolume(float v)    { UIVolume    = Mathf.Clamp01(v); Apply(uiParam,    UIVolume);    }

    // pushes all three volume values to the mixer; safe to call on every scene load
    public void ApplyAllAudio()
    {
        Apply(musicParam, MusicVolume);
        Apply(sfxParam,   SFXVolume);
        Apply(uiParam,    UIVolume);
    }

    // converts a 0-1 linear volume value to decibels and sets it on the mixer parameter
    private void Apply(string param, float linear)
    {
        if (mainMixer == null)
        {
            Debug.LogError("[SettingsManager] mainMixer is not assigned on the prefab/object.");
            return;
        }

        // map linear 0-1 to decibels; clamp near-zero to -80db to avoid log(0)
        float db = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        if (!mainMixer.SetFloat(param, db))
            Debug.LogWarning($"[SettingsManager] Mixer has no exposed parameter named '{param}'. " +
                             "Right-click the group's Volume in the mixer → Expose to script, then rename it to match.");
    }

    // returns the correct mixer group for the given audio category
    public AudioMixerGroup GroupFor(AudioCategory c) =>
        c == AudioCategory.Music ? musicGroup :
        c == AudioCategory.SFX   ? sfxGroup   : uiGroup;

    // assigns an audiosource to the correct mixer group so its volume is controlled by the right slider
    public void Route(AudioSource src, AudioCategory category)
    {
        if (src == null) return;
        var group = GroupFor(category);
        if (group == null)
        {
            Debug.LogWarning($"[SettingsManager] No mixer group assigned for {category}. " +
                             "Assign the Music/SFX/UI groups on the SettingsManager prefab, " +
                             "otherwise this source bypasses the mixer and the volume slider can't silence it.");
            return;
        }
        src.outputAudioMixerGroup = group;
    }

    // updates the brightness value and fires the event so any listeners can react immediately
    public void SetBrightness(float v)
    {
        Brightness = Mathf.Clamp01(v);
        OnBrightnessChanged?.Invoke(Brightness);
    }

    // stores the mouse sensitivity value without applying it directly (read by playercamera)
    public void SetSensitivity(float v) => Sensitivity = v;

    // builds the fixed list of supported screen resolutions using the current display's refresh rate
    private void BuildResolutionList()
    {
        RefreshRate rate = Screen.currentResolution.refreshRateRatio;
        _resolutions = new Resolution[]
        {
            new Resolution { width = 1280, height = 720,  refreshRateRatio = rate },
            new Resolution { width = 1600, height = 900,  refreshRateRatio = rate },
            new Resolution { width = 1920, height = 1080, refreshRateRatio = rate }
        };
    }

    // populates a tmp dropdown with all available resolutions and selects the saved index
    public void PopulateDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null || _resolutions == null) return;
        dropdown.ClearOptions();
        var options = new List<string>();
        foreach (var r in _resolutions) options.Add($"{r.width} x {r.height}");
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(ResolutionIndex);
        dropdown.RefreshShownValue();
    }

    // updates the dropdown selection to match the currently saved resolution without triggering the change event
    public void SyncDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;
        dropdown.SetValueWithoutNotify(ResolutionIndex);
        dropdown.RefreshShownValue();
    }

    // applies the resolution at the given index to the screen and optionally saves it to playerprefs
    public void ApplyResolution(int index, bool persist = true)
    {
        if (_resolutions == null || index < 0 || index >= _resolutions.Length) return;
        ResolutionIndex = index;
        Resolution r = _resolutions[index];
        Screen.SetResolution(r.width, r.height, fullScreenMode, r.refreshRateRatio);

        // force all canvases to recalculate their layout after the resolution change
        Canvas.ForceUpdateCanvases();
        if (persist) { PlayerPrefs.SetInt(KeyResIndex, index); PlayerPrefs.Save(); }
    }
}