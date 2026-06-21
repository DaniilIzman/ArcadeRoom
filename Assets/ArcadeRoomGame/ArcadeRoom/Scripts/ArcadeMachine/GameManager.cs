using UnityEngine;
using TMPro;

// singleton that manages the player's credit balance for the active save slot
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // how many credits a brand new save starts with
    [Header("Economy Settings")]
    public int startingCredits = 500;
    public int currentCredits;

    // when true, always overwrites saved credits with startingCredits on load; useful for testing
    [Header("Debug / Testing")]
    public bool forceStartingCreditsOnLoad = false;

    // the save slot number currently in use, loaded from playerprefs
    private int activeSlot;

    private void Awake()
    {
        // enforce singleton; destroy any duplicate that appears after the first
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // read which slot was selected in the main menu; defaults to 1 when testing in-scene
        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);

        if (forceStartingCreditsOnLoad)
        {
            // debug mode: always reset credits to the starting value and save immediately
            currentCredits = startingCredits;
            SaveCredits();
        }
        else
        {
            if (!PlayerPrefs.HasKey($"PlayerCredits_Slot{activeSlot}"))
            {
                // no existing save for this slot; initialise with starting credits
                currentCredits = startingCredits;
                SaveCredits();
            }
            else
            {
                // load the saved credit balance for this slot
                currentCredits = PlayerPrefs.GetInt($"PlayerCredits_Slot{activeSlot}");
            }
        }

        UpdateCreditsUI();
    }

    // deducts the given amount if the player has enough credits; returns true on success
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

    // adds credits to the current balance, saves, and logs the new total for debugging
    public void AddCredits(int amount)
    {
        currentCredits += amount;
        SaveCredits();
        Debug.Log("DEBUG: Added " + amount + " credits. New total: " + currentCredits);
    }

    // resets credits to the starting value and wipes all arcade unlocks for this slot
    public void ResetCredits()
    {
        currentCredits = startingCredits;

        // remove the credit balance and the master unlock string for the active slot
        PlayerPrefs.DeleteKey($"PlayerCredits_Slot{activeSlot}");
        PlayerPrefs.DeleteKey($"ArcadeUnlocks_Slot{activeSlot}");
        PlayerPrefs.Save();

        UpdateCreditsUI();
        Debug.Log($"DEBUG: GameManager credits for Slot {activeSlot} have been reset to " + startingCredits);
    }

    // writes the current credit balance to playerprefs and refreshes the ui
    private void SaveCredits()
    {
        PlayerPrefs.SetInt($"PlayerCredits_Slot{activeSlot}", currentCredits);
        PlayerPrefs.Save();
        UpdateCreditsUI();
    }

    // tells the shared ui manager to refresh the on-screen credit display
    private void UpdateCreditsUI()
    {
        if (UIManager.Instance != null) UIManager.Instance.UpdateCreditText(currentCredits);
    }

    // amount of credits granted by the debug key
    [Header("Debug / Testing")]
    [Tooltip("Press the debug key during play to set credits to this amount.")]
    public int debugCreditAmount = 999;
    public KeyCode debugGiveCreditsKey = KeyCode.R;

    private void Update()
    {
        // debug shortcut: press the key to top up credits to the debug amount
        if (Input.GetKeyDown(debugGiveCreditsKey))
        {
            currentCredits = debugCreditAmount;
            SaveCredits();
            Debug.Log($"DEBUG: credits set to {currentCredits} via debug key.");
        }
    }

    // editor context menu tool that wipes all save data for the active slot
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