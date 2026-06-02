using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Text Elements")]
    public TextMeshProUGUI creditText;
    public TextMeshProUGUI hoverText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && creditText != null)
        {
            creditText.text = "Credits: " + GameManager.Instance.Credits;
        }
    }

    public void ShowHoverText(string message)
    {
        if (hoverText != null)
        {
            hoverText.text = message;
        }
    }

    public void ClearHoverText()
    {
        if (hoverText != null)
        {
            hoverText.text = "";
        }
    }
}