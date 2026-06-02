using UnityEngine;

public class PrizeCounter : Interactable
{
    [Header("Prize Settings")]
    public string prizeName = "Neon Sign";
    public int prizeCost = 50;
    
    [Header("Target Decoration")]
    public GameObject decorationToUnlock; 

    private void Start()
    {
        // this formats the text specifically for shop transactions
        hoverText = "Press E to buy " + prizeName + " for " + prizeCost + " credits";
    }

    public override void Interact()
    {
        if (decorationToUnlock != null)
        {
            if (decorationToUnlock.activeSelf)
            {
                Debug.Log("You have already purchased this prize!");
                return;
            }
        }

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.SpendCredits(prizeCost))
            {
                UnlockPrize();
            }
        }
    }

    private void UnlockPrize()
    {
        if (decorationToUnlock != null)
        {
            decorationToUnlock.SetActive(true);
        }
        
        Debug.Log("Successfully purchased and displayed: " + prizeName);
        
        // change the text state to reflect that the item has been bought
        hoverText = prizeName + " (Purchased)";
        
        // refresh the user interface immediately if the player is still in the zone
        if (isPlayerInside)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowHoverText(hoverText);
            }
        }
    }
}