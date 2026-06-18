using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// singleton that handles the in-game pause and settings panels
public class EscapeMenu : MonoBehaviour
{
    public static EscapeMenu Instance { get; private set; }

    // the two panels toggled depending on whether the player is in the pause or settings view
    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    // fullscreen overlay image used to simulate brightness by darkening the screen
    public Image      brightnessOverlay;

    // all settings controls wired up in start
    [Header("Settings Controls")]
    public TMP_Dropdown    resolutionDropdown;
    public Slider          brightnessSlider;
    public Slider          musicSlider;
    public Slider          sfxSlider;
    public Slider          uiSlider;
    public Slider          sensitivitySlider;

    // label on the settings back button; changes to "confirm & save" when there are unsaved changes
    [Header("Dynamic Button Text")]
    public TextMeshProUGUI backButtonText;

    // scene to load when the player chooses to return to the main menu
    [Header("Scene Routing")]
    public string mainMenuSceneName = "MainMenu";

    // tracks the current state of the menu so applyPanelState can derive what to show
    private bool _isPaused   = false;
    private bool _inSettings = false;
    private bool _hasChanges = false;

    // set to false by other systems (shop, arcade) to prevent the escape menu from opening
    public  bool canPause    = true;

    // cached at startup to avoid repeated scene searches
    private PlayerCamera   _cachedCamera;
    private PlayerMovement _cachedMovement;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _cachedCamera   = Object.FindFirstObjectByType<PlayerCamera>();
        _cachedMovement = Object.FindFirstObjectByType<PlayerMovement>();

        _isPaused   = false;
        _inSettings = false;
        ApplyPanelState();

        // populate and wire the resolution dropdown if both references are available
        if (resolutionDropdown != null && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.PopulateDropdown(resolutionDropdown);
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
            resolutionDropdown.onValueChanged.AddListener(_ => MarkSettingsAsDirty());
        }

        if (musicSlider)       musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider)         sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiSlider)          uiSlider.onValueChanged.AddListener(SetUIVolume);
        if (brightnessSlider)  brightnessSlider.onValueChanged.AddListener(SetBrightness);
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(v =>
        {
            SettingsManager.Instance?.SetSensitivity(v);
            ApplySensitivityToCamera(v);
            MarkSettingsAsDirty();
        });

        // subscribe to brightness changes so the overlay stays in sync
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged += UpdateBrightnessOverlay;

        WireButtonSounds(pausePanel);
        WireButtonSounds(settingsPanel);
        WireSliderSounds();

        LoadSliders();
        ResetBackButtonText();
    }

    private void OnDestroy()
    {
        // restore timescale and unsubscribe from the brightness event to avoid stale references
        Time.timeScale = 1f;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnBrightnessChanged -= UpdateBrightnessOverlay;
    }

    private void Update()
    {
        if (!canPause) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // pressing escape while in settings saves and returns to the pause panel
            if (_isPaused && _inSettings) CloseSettingsAndSave();
            else TogglePauseState();
        }
    }

    // determines which panels should be visible based on the current pause and settings state
    private void ApplyPanelState()
    {
        if (pausePanel)    pausePanel.SetActive(_isPaused && !_inSettings);
        if (settingsPanel) settingsPanel.SetActive(_isPaused && _inSettings);
    }

    // toggles between paused and unpaused
    public void TogglePauseState() => SetPaused(!_isPaused);

    // resumes the game from the pause button
    public void ResumeGame() => SetPaused(false);

    // applies all side effects of changing the paused state
    private void SetPaused(bool paused)
    {
        _isPaused = paused;

        // always exit the settings sub-panel when unpausing
        if (!paused) _inSettings = false;

        Time.timeScale = paused ? 0f : 1f;
        ApplyPanelState();
        SetPlayerFrozen(paused);
        ManageCursorState(paused);

        // sync sliders to current saved values each time the pause menu opens
        if (paused) LoadSliders();
    }

    // closes the menu and prevents it from being re-opened until unlocked
    public void ForceCloseAndLock()
    {
        canPause    = false;
        _isPaused   = false;
        _inSettings = false;
        Time.timeScale = 1f;
        ApplyPanelState();
        SetPlayerFrozen(false);
    }

    // re-enables opening the escape menu after it was force-closed
    public void UnlockMenu() => canPause = true;

    // opens the settings panel from within the pause menu
    public void OpenSettings()
    {
        if (!_isPaused) return;
        _hasChanges = false;
        ResetBackButtonText();

        if (resolutionDropdown != null && SettingsManager.Instance != null)
            SettingsManager.Instance.SyncDropdown(resolutionDropdown);

        LoadSliders();
        _inSettings = true;
        ApplyPanelState();
    }

    // saves any pending changes and returns from settings to the pause panel
    public void CloseSettingsAndSave()
    {
        if (_hasChanges)
        {
            if (sensitivitySlider != null)
            {
                SettingsManager.Instance?.SetSensitivity(sensitivitySlider.value);
                ApplySensitivityToCamera(sensitivitySlider.value);
            }
            SettingsManager.Instance?.SaveAll();
            _hasChanges = false;
        }
        _inSettings = false;
        ApplyPanelState();
        ResetBackButtonText();
    }

    // resets timescale and loads the main menu scene using the scene fader if available
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        if (SceneFader.Instance != null) SceneFader.Instance.LoadScene(mainMenuSceneName);
        else if (!string.IsNullOrEmpty(mainMenuSceneName)) SceneManager.LoadScene(mainMenuSceneName);
    }

    // resets the back button text and the unsaved changes flag
    private void ResetBackButtonText()
    {
        if (backButtonText) backButtonText.text = "Back";
    }

    // marks that an unsaved change exists and updates the back button label
    private void MarkSettingsAsDirty()
    {
        if (_hasChanges) return;
        _hasChanges = true;
        if (backButtonText) backButtonText.text = "Confirm & Save";
    }

    // reads current values from settingsmanager and updates all sliders without triggering their events
    private void LoadSliders()
    {
        if (!SettingsManager.Instance) return;
        if (musicSlider)      musicSlider.SetValueWithoutNotify(SettingsManager.Instance.MusicVolume);
        if (sfxSlider)        sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.SFXVolume);
        if (uiSlider)         uiSlider.SetValueWithoutNotify(SettingsManager.Instance.UIVolume);
        if (brightnessSlider)
        {
            brightnessSlider.SetValueWithoutNotify(SettingsManager.Instance.Brightness);
            UpdateBrightnessOverlay(SettingsManager.Instance.Brightness);
        }
        float sensitivity = SettingsManager.Instance.Sensitivity;
        if (sensitivitySlider) sensitivitySlider.SetValueWithoutNotify(sensitivity);
        ApplySensitivityToCamera(sensitivity);
        SettingsManager.Instance.ApplyAllAudio();
    }

    // each method passes the new value to settingsmanager and flags the change as unsaved
    public void SetResolution(int index)
    {
        SettingsManager.Instance?.ApplyResolution(index);
        MarkSettingsAsDirty();
    }

    public void SetMusicVolume(float v)  { SettingsManager.Instance?.SetMusicVolume(v); MarkSettingsAsDirty(); }
    public void SetSFXVolume(float v)    { SettingsManager.Instance?.SetSFXVolume(v);   MarkSettingsAsDirty(); }
    public void SetUIVolume(float v)     { SettingsManager.Instance?.SetUIVolume(v);    MarkSettingsAsDirty(); }

    public void SetBrightness(float v)
    {
        // setting brightness fires onbrightnesschanged which calls updatebrightnessoverlay
        SettingsManager.Instance?.SetBrightness(v);
        MarkSettingsAsDirty();
    }

    // adjusts the alpha of the black overlay image to simulate a brightness change
    private void UpdateBrightnessOverlay(float value)
    {
        if (brightnessOverlay == null) return;
        float alpha = Mathf.Lerp(0.85f, 0f, Mathf.Clamp01(value));
        brightnessOverlay.color = new Color(0f, 0f, 0f, alpha);
    }

    // pushes the sensitivity value directly to the cached camera component
    private void ApplySensitivityToCamera(float v) { if (_cachedCamera) _cachedCamera.mouseSensitivity = v; }

    // sets the pause freeze flag on both the camera and movement controllers
    private void SetPlayerFrozen(bool freeze)
    {
        if (_cachedCamera)   _cachedCamera.isPausedByMenu   = freeze;
        if (_cachedMovement) _cachedMovement.isPausedByMenu = freeze;
    }

    // locks or unlocks the cursor depending on whether the menu is visible
    private void ManageCursorState(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = visible;
    }

    // attaches a click sound listener to every button found inside the given panel
    private void WireButtonSounds(GameObject panel)
    {
        if (panel == null) return;
        foreach (var btn in panel.GetComponentsInChildren<Button>(true))
            btn.onClick.AddListener(() => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });
    }

    // attaches slider tick and dropdown click sounds to all settings controls
    private void WireSliderSounds()
    {
        void SliderTick() { if (UIManager.Instance) UIManager.Instance.PlaySliderTick(); }
        if (musicSlider)       musicSlider.onValueChanged.AddListener(_       => SliderTick());
        if (sfxSlider)         sfxSlider.onValueChanged.AddListener(_         => SliderTick());
        if (uiSlider)          uiSlider.onValueChanged.AddListener(_          => SliderTick());
        if (brightnessSlider)  brightnessSlider.onValueChanged.AddListener(_  => SliderTick());
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(_ => SliderTick());
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(_ =>
        { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });
    }
}