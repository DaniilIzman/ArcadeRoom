using UnityEngine;

public class GameManager : MonoBehaviour
{
    // The static instance allows any script to access GameManager.Instance from anywhere in the project.
    public static GameManager Instance { get; private set; }

    [Header("Economy")]
    [SerializeField] private int credits = 0;

    // This is a traditional getter property that permits other scripts to read the credit count without modifying it directly.
    public int Credits
    {
        get
        {
            return credits;
        }
    }

    private void Awake()
    {
        // This singleton pattern ensures that there is only ever one GameManager active in the game scene.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void AddCredits(int amount)
    {
        credits += amount;
        Debug.Log("Credits added! Total: " + credits);
    }

    public bool SpendCredits(int amount)
    {
        if (credits >= amount)
        {
            credits -= amount;
            Debug.Log("Credits spent! Total: " + credits);
            return true;
        }
        
        Debug.Log("Not enough credits!");
        return false;
    }
}