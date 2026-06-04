using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Singleton pattern to ensure only one GameManager exists and can be accessed anywhere
    public static GameManager Instance { get; private set; }

    [Header("Economy Settings")]
    [Tooltip("The amount of credits the player starts with on a brand new save.")]
    public int startingCredits = 500;
    
    [Tooltip("The player's current money. (Auto-updates)")]
    public int currentCredits;

    [Header("UI References")]
    [Tooltip("Drag your Credits Text UI element here.")]
    public TextMeshProUGUI creditsText; 

    private void Awake()
    {
        // Standard Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // load saved credits; if it's the player's very first time 
        // (no save found), default to 'startingCredits'.
        currentCredits = PlayerPrefs.GetInt("PlayerCredits", startingCredits);
        
        // refresh the UI immediately on start
        UpdateCreditsUI();
    }

    /// Called by ShopManager.cs when the player clicks a "Buy" button.
    /// <param name="amount">The price of the item.</param>
    /// <returns>True if the transaction was successful, false if not enough credits.</returns>
    public bool TrySpendCredits(int amount)
    {
        if (currentCredits >= amount)
        {
            // deduct the cost
            currentCredits -= amount;
            
            // immediately save the new credit balance to the hard drive
            PlayerPrefs.SetInt("PlayerCredits", currentCredits);
            PlayerPrefs.Save();
            
            // update the visual text on screen
            UpdateCreditsUI();
            
            return true; // Transaction approved
        }
        
        return false; // transaction denied
    }

    /// called by PlayerMovement.cs during the Debug 'R' reset sequence.
    public void ResetCredits()
    {
        // Revert back to default
        currentCredits = startingCredits;
        
        // Overwrite the save file with the default amount
        PlayerPrefs.SetInt("PlayerCredits", currentCredits);
        PlayerPrefs.Save();
        
        UpdateCreditsUI();
        Debug.Log("DEBUG: GameManager credits have been reset to " + startingCredits);
    }

    /// updates the TextMeshPro UI element if one is assigned.
    private void UpdateCreditsUI()
    {
        if (creditsText != null)
        {
            creditsText.text = "Credits: " + currentCredits.ToString();
        }
    }

    public void AddCredits(int amount)
    {
        // add the money
        currentCredits += amount;
        
        // immediately save the new credit balance to the hard drive/registry
        PlayerPrefs.SetInt("PlayerCredits", currentCredits);
        PlayerPrefs.Save();
        
        // update the visual text on screen
        UpdateCreditsUI();
        
        Debug.Log("DEBUG: Added " + amount + " credits. New total: " + currentCredits);
    }
}