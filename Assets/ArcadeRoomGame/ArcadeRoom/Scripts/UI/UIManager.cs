using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI creditText;
    public TextMeshProUGUI interactionPromptText;

    [Header("Audio Feedback")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip sliderTickSound;
    
    [Tooltip("Minimum time between slider tick sounds to avoid audio distortion.")]
    [SerializeField] private float sliderSoundCooldown = 0.05f;
    private float lastSliderSoundTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ensure the prompt is hidden when the game starts
        HidePrompt();

        // add an AudioSource if one wasn't manually assigned
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // ensure UI sounds ignore the game's paused time scale
        uiAudioSource.playOnAwake = false;
        uiAudioSource.ignoreListenerPause = true; 
    }

    public void UpdateCreditText(int newAmount)
    {
        if (creditText != null)
        {
            creditText.text = "Credits: " + newAmount;
        }
    }

    public void ShowPrompt(string message)
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.text = message;
            interactionPromptText.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
            interactionPromptText.text = "";
        }
    }

    #region Public Audio Triggers

    public void PlayClickSound()
    {
        if (uiAudioSource != null && clickSound != null)
        {
            uiAudioSource.PlayOneShot(clickSound);
        }
    }

    public void PlaySliderTick()
    {
        // cooldown prevents rapid value updates from causing audio distortion
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