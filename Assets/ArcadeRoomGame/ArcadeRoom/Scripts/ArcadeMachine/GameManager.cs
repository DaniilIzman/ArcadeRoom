using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Economy Settings")]
    public int startingCredits = 500;
    public int currentCredits;

    [Header("Debug / Testing")]
    public bool forceStartingCreditsOnLoad = false;

    [Header("UI References")]
    [Tooltip("Drag your Credits Text UI element here.")]
    public TextMeshProUGUI creditsText; 

    private int activeSlot;

    // 1 - Singleton setup
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 2 - Load active slot and credits for that slot
    private void Start()
    {
        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);

        if (forceStartingCreditsOnLoad)
        {
            currentCredits = startingCredits;
            SaveCredits();
        }
        else
        {
            if (!PlayerPrefs.HasKey($"PlayerCredits_Slot{activeSlot}"))
            {
                currentCredits = startingCredits;
                SaveCredits();
            }
            else
            {
                currentCredits = PlayerPrefs.GetInt($"PlayerCredits_Slot{activeSlot}");
            }
        }
        
        UpdateCreditsUI();
    }

    // 3 - Credit transactions
    public bool TrySpendCredits(int amount)
    {
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            SaveCredits();
            return true; 
        }
        return false; 
    }

    public void AddCredits(int amount)
    {
        currentCredits += amount;
        SaveCredits();
        Debug.Log("DEBUG: Added " + amount + " credits. New total: " + currentCredits);
    }

    // 4 - Reset (new game / debug)
    public void ResetCredits()
    {
        currentCredits = startingCredits;
        
        PlayerPrefs.DeleteKey($"PlayerCredits_Slot{activeSlot}");
        PlayerPrefs.DeleteKey($"ArcadeUnlocks_Slot{activeSlot}");
        
        PlayerPrefs.Save();
        
        UpdateCreditsUI();
        Debug.Log($"DEBUG: GameManager credits for Slot {activeSlot} have been reset to " + startingCredits);
    }

    // 5 - Persistence helpers
    private void SaveCredits()
    {
        PlayerPrefs.SetInt($"PlayerCredits_Slot{activeSlot}", currentCredits);
        PlayerPrefs.Save();
        UpdateCreditsUI();
    }

    private void UpdateCreditsUI()
    {
        if (creditsText != null)
        {
            creditsText.text = "Credits: " + currentCredits.ToString();
        }
    }

    // 6 - Editor / debug tools
    [ContextMenu("Wipe Active Slot Save Data")]
    public void EditorWipeSave()
    {
        int editorActiveSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        PlayerPrefs.DeleteKey($"PlayerCredits_Slot{editorActiveSlot}");
        PlayerPrefs.DeleteKey($"ArcadeUnlocks_Slot{editorActiveSlot}");
        
        PlayerPrefs.Save();
        Debug.Log($"Save wiped for Slot {editorActiveSlot}! Uncheck 'forceStartingCreditsOnLoad' to test a fresh install.");
    }
}