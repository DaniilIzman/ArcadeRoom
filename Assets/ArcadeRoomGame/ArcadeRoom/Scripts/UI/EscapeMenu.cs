using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class EscapeMenu : MonoBehaviour
{
    public static EscapeMenu Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Settings Controls")]
    public TMP_Dropdown resolutionDropdown;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider uiVolumeSlider;
    public Slider sensitivitySlider;

    [Header("Dynamic Button Settings")]
    public TextMeshProUGUI backButtonText;

    [Header("Scene Routing")]
    public string mainMenuSceneName = "MainMenu";
    public float  mainMenuLoadDelay = 1.0f;

    private bool isPaused   = false;
    private bool hasChanges = false;
    public  bool canPause   = true;

    private PlayerCamera   cachedCamera;
    private PlayerMovement cachedMovement;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        cachedCamera   = Object.FindFirstObjectByType<PlayerCamera>();
        cachedMovement = Object.FindFirstObjectByType<PlayerMovement>();

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        if (resolutionDropdown != null)
        {
            SettingsManager.Instance.PopulateDropdown(resolutionDropdown);
            // ApplyResolution is already wired by PopulateDropdown; just add the dirty flag on top
            resolutionDropdown.onValueChanged.AddListener(_ => MarkSettingsAsDirty());
        }

        LoadSlidersFromSettings();
        ResetBackButtonText();

        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider)   sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiVolumeSlider)    uiVolumeSlider.onValueChanged.AddListener(SetUIVolume);
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        WireEscapeMenuAudio();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && canPause)
        {
            if (settingsPanel.activeSelf) CloseSettingsAndSave();
            else TogglePauseState();
        }
    }

    // ── Pause ─────────────────────────────────────────────────────────────────

    public void TogglePauseState()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pausePanel.SetActive(isPaused);
        UpdatePlayerConstraints(isPaused);
        ManageCursorState(isPaused);
    }

    public void ForceCloseAndLock()
    {
        canPause = false;
        if (isPaused) TogglePauseState();
    }

    public void UnlockMenu() => canPause = true;

    public void OpenSettings()
    {
        hasChanges = false;
        ResetBackButtonText();
        LoadSlidersFromSettings(); // refresh in case another scene changed saved values
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettingsAndSave()
    {
        if (hasChanges)
        {
            SettingsManager.Instance.SaveAll();
            hasChanges = false;
        }
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
        ResetBackButtonText();
    }

    public void LoadMainMenu() => StartCoroutine(DelayedMenuLoadRoutine());

    private IEnumerator DelayedMenuLoadRoutine()
    {
        yield return new WaitForSecondsRealtime(mainMenuLoadDelay);
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName)) SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    private void ResetBackButtonText()
    {
        if (backButtonText != null) backButtonText.text = "Back";
    }

    private void MarkSettingsAsDirty()
    {
        if (!hasChanges)
        {
            hasChanges = true;
            if (backButtonText != null) backButtonText.text = "Confirm & Save";
        }
    }

    private void LoadSlidersFromSettings()
    {
        if (!SettingsManager.Instance) return;
        if (musicVolumeSlider) musicVolumeSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedMusicVol);
        if (sfxVolumeSlider)   sfxVolumeSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedSFXVol);
        if (uiVolumeSlider)    uiVolumeSlider.SetValueWithoutNotify(SettingsManager.Instance.SavedUIVol);

        float savedSensitivity = PlayerPrefs.GetFloat("Setting_MouseSensitivity", 2.0f);
        if (sensitivitySlider) sensitivitySlider.SetValueWithoutNotify(savedSensitivity);
        ApplySensitivityToCamera(savedSensitivity);
    }

    public void SetMusicVolume(float value) { SettingsManager.Instance.SetMusicVolume(value); MarkSettingsAsDirty(); }
    public void SetSFXVolume(float value)   { SettingsManager.Instance.SetSFXVolume(value);   MarkSettingsAsDirty(); }
    public void SetUIVolume(float value)    { SettingsManager.Instance.SetUIVolume(value);     MarkSettingsAsDirty(); }

    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat("Setting_MouseSensitivity", value);
        ApplySensitivityToCamera(value);
        MarkSettingsAsDirty();
    }

    // Kept as a public passthrough for any Inspector-wired bindings
    public void SetResolution(int index)
    {
        SettingsManager.Instance.ApplyResolution(index);
        MarkSettingsAsDirty();
    }

    private void ApplySensitivityToCamera(float value)
    {
        if (cachedCamera != null) cachedCamera.mouseSensitivity = value;
    }

    private void UpdatePlayerConstraints(bool shouldFreeze)
    {
        if (cachedCamera  != null) cachedCamera.isPausedByMenu  = shouldFreeze;
        if (cachedMovement != null) cachedMovement.isPausedByMenu = shouldFreeze;
    }

    private void ManageCursorState(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = visible;
    }

    private void WireEscapeMenuAudio()
    {
        if (musicVolumeSlider)  musicVolumeSlider.onValueChanged.AddListener(_  => { if (UIManager.Instance) UIManager.Instance.PlaySliderTick(); });
        if (sfxVolumeSlider)    sfxVolumeSlider.onValueChanged.AddListener(_    => { if (UIManager.Instance) UIManager.Instance.PlaySliderTick(); });
        if (uiVolumeSlider)     uiVolumeSlider.onValueChanged.AddListener(_     => { if (UIManager.Instance) UIManager.Instance.PlaySliderTick(); });
        if (sensitivitySlider)  sensitivitySlider.onValueChanged.AddListener(_  => { if (UIManager.Instance) UIManager.Instance.PlaySliderTick(); });
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(_ => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });

        Button[] settingsButtons = settingsPanel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in settingsButtons)
            btn.onClick.AddListener(() => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });

        Button[] pauseButtons = pausePanel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in pauseButtons)
            btn.onClick.AddListener(() => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });
    }

    private void OnDestroy() => Time.timeScale = 1f;
}