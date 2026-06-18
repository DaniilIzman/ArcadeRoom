using UnityEngine;
using TMPro;

// singleton that owns shared ui elements and provides audio feedback methods used across all menus
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // hud text elements updated by other systems
    [Header("UI References")]
    public TextMeshProUGUI creditText;
    public TextMeshProUGUI interactionPromptText;

    // audiosource and clips for button and slider sounds played anywhere in the ui
    [Header("Audio Feedback - UI Only")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip sliderTickSound;

    // minimum time between successive slider tick sounds to prevent audio distortion
    [Tooltip("Minimum time between slider tick sounds to avoid audio distortion.")]
    [SerializeField] private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        HidePrompt();

        if (uiAudioSource == null) uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.ignoreListenerPause = true;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.Route(uiAudioSource, SettingsManager.AudioCategory.UI);

        // read the active slot and initialize the credit display with the stored balance
        int activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        int credits = PlayerPrefs.GetInt("PlayerCredits_Slot" + activeSlot, 0);
        UpdateCreditText(credits);
    }

    // updates the credit counter text displayed in the hud
    public void UpdateCreditText(int newAmount) { if (creditText != null) creditText.text = "Credits: " + newAmount; }

    // shows the interaction prompt with the given message
    public void ShowPrompt(string message)
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.text = message;
            interactionPromptText.gameObject.SetActive(true);
        }
    }

    // hides the interaction prompt and clears its text
    public void HidePrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
            interactionPromptText.text = "";
        }
    }

    #region Public Audio Triggers

    // plays the click sound as a one-shot so overlapping clicks don't cut each other off
    public void PlayClickSound() { if (uiAudioSource && clickSound) uiAudioSource.PlayOneShot(clickSound); }

    // plays the slider tick sound, rate-limited by the cooldown to prevent audio spam
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

    #endregion
}