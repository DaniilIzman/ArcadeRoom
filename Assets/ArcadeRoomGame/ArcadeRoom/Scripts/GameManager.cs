using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ssingleton setup so other scripts can easily talk to this one
    public static GameManager Instance { get; private set; }

    [Header("Player Economy")]
    public int currentCredits = 50; // starting money

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
        // update the UI as soon as the game starts
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCreditText(currentCredits);
        }
    }

    // method to attempt buying something
    public bool TrySpendCredits(int amount)
    {
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            Debug.Log("Spent " + amount + " credits. Remaining: " + currentCredits);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCreditText(currentCredits);
            }
            return true; // transaction successful
        }
        
        Debug.Log("Not enough credits!");
        return false; // transaction failed
    }
}