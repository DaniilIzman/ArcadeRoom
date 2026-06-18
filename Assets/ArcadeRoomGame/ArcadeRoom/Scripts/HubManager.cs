using UnityEngine;
using TMPro;

public class HubManager : MonoBehaviour
{
    public static HubManager Instance { get; private set; }

    [Header("economy")]
    public int startingCredits = 100;
    public TextMeshProUGUI creditsText;

    private int currentCredits;
    private int activeSlot;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // capture hardware profile key
        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        string key = $"PlayerCredits_Slot{activeSlot}";

        if (!PlayerPrefs.HasKey(key))
        {
            currentCredits = startingCredits;
            savecredits();
        }
        else
        {
            currentCredits = PlayerPrefs.GetInt(key);
        }
        
        updatecreditsui();
    }

    public bool usecredits(int amount)
    {
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            savecredits();
            return true;
        }
        return false;
    }

    private void savecredits()
    {
        // log monetary value directly to storage
        PlayerPrefs.SetInt($"PlayerCredits_Slot{activeSlot}", currentCredits);
        PlayerPrefs.Save();
        updatecreditsui();
    }

    private void updatecreditsui()
    {
        if (creditsText) creditsText.text = $"credits: {currentCredits}";
    }
}