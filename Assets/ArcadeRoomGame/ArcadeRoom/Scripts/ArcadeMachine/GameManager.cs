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

    private int activeSlot; // tracks which slot we are currently playing

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
        // fetch the slot chosen in the Main Menu (defaults to 1 if testing directly in the scene)
        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);

        if (forceStartingCreditsOnLoad)
        {
            currentCredits = startingCredits;
            SaveCredits(); // FIX: Immediately commit forced credits to disk
        }
        else
        {
            // FIX: Check if the key exists. If it doesn't, this is a brand new save!
            if (!PlayerPrefs.HasKey($"PlayerCredits_Slot{activeSlot}"))
            {
                currentCredits = startingCredits;
                SaveCredits(); // Immediately commit brand new player money to disk
            }
            else
            {
                // Load existing saved credits specifically for the active slot
                currentCredits = PlayerPrefs.GetInt($"PlayerCredits_Slot{activeSlot}");
            }
        }
        
        UpdateCreditsUI();
    }

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

    public void ResetCredits()
    {
        currentCredits = startingCredits;
        
        // delete the save key entirely to ensure a true factory reset for this slot
        PlayerPrefs.DeleteKey($"PlayerCredits_Slot{activeSlot}");
        PlayerPrefs.Save();
        
        UpdateCreditsUI();
        Debug.Log($"DEBUG: GameManager credits for Slot {activeSlot} have been reset to " + startingCredits);
    }

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

    [ContextMenu("Wipe Active Slot Save Data")]
    public void EditorWipeSave()
    {
        // editor tool will wipe whichever slot is currently considered active
        int editorActiveSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        PlayerPrefs.DeleteKey($"PlayerCredits_Slot{editorActiveSlot}");
        PlayerPrefs.Save();
        Debug.Log($"Save wiped for Slot {editorActiveSlot}! Uncheck 'forceStartingCreditsOnLoad' to test a fresh install.");
    }
}