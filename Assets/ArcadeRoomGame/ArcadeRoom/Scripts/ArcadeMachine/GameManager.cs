using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Economy Settings")]
    [SerializeField] private int startingCredits = 10;
    private int currentCredits;

    private void Awake()
    {
        // 1. Check if an instance already exists when reloading the lobby scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy the duplicate copy
            return;
        }

        Instance = this;
        
        // 2. CRITICAL: Tells Unity not to destroy this object when changing scenes
        DontDestroyOnLoad(gameObject); 

        // Initialize credits only once when the game first boots up
        currentCredits = startingCredits;
    }

    private void OnEnable()
    {
        // Listen for scene loads so we can update the UI automatically on return
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        UpdateCreditUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 3. FIX: Whenever you return to the lobby, instantly push the persistent credit count
        // to the brand new UIManager instance in that scene.
        UpdateCreditUI();
    }

    public bool TrySpendCredits(int amount)
    {
        if (currentCredits >= amount)
        {
            currentCredits -= amount;
            UpdateCreditUI();
            return true;
        }
        return false;
    }

    // Call this from your mini-game scripts when the player wins/earns rewards!
    public void AddCredits(int amount)
    {
        currentCredits += amount;
        UpdateCreditUI();
    }

    private void UpdateCreditUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCreditText(currentCredits);
        }
    }
}