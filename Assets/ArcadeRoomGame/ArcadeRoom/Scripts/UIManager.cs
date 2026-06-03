using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI creditText;
    public TextMeshProUGUI interactionPromptText;

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
}