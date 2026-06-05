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
        // force the starting value to test the shop
        if (forceStartingCreditsOnLoad)
        {
            currentCredits = startingCredits;
        }
        else
        {
            // load saved credits. 
            currentCredits = PlayerPrefs.GetInt("PlayerCredits", startingCredits);
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
        
        // delete the save key entirely to ensure a true factory reset
        PlayerPrefs.DeleteKey("PlayerCredits");
        PlayerPrefs.Save();
        
        UpdateCreditsUI();
        Debug.Log("DEBUG: GameManager credits have been reset to " + startingCredits);
    }

    // helper method to prevent repeating the same save code
    private void SaveCredits()
    {
        PlayerPrefs.SetInt("PlayerCredits", currentCredits);
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

    // right-click the GameManager component header in the Unity Inspector 
    // and click "Wipe Save Data" to instantly delete save file while editing
    [ContextMenu("Wipe Save Data")]
    public void EditorWipeSave()
    {
        PlayerPrefs.DeleteKey("PlayerCredits");
        PlayerPrefs.Save();
        Debug.Log("Save wiped! Uncheck 'forceStartingCreditsOnLoad' to test a fresh install.");
    }
}