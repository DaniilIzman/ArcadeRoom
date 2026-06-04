using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("The master parent GameObject holding all shop UI elements.")]
    public GameObject shopContainer;       
    public GameObject npcDialoguePanel;    
    public GameObject powerupsPanel;       
    public GameObject trophiesPanel;       

    [Header("Navigation Buttons")]
    public Button navPowerupsButton;
    public Button navTrophiesButton;
    public Button navLeaveButton;
    public Button backToDialogueButton1;   
    public Button backToDialogueButton2;   

    [Header("Cube Item Settings")]
    public int cubePrice = 50;
    public GameObject cubePrefab;       
    public Transform cubeSpawnPoint;    
    public Button buyCubeButton;

    [HideInInspector] public bool isShopOpen = false;
    
    private PlayerCamera cachedCamera;
    private PlayerMovement cachedMovement;

    private void Awake()
    {
        // Enforce Singleton Pattern
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
        // Cache player references to minimize performance overhead during open/close transitions
        cachedCamera = Object.FindFirstObjectByType<PlayerCamera>();
        cachedMovement = Object.FindFirstObjectByType<PlayerMovement>();

        // Ensure the shop UI starts completely hidden
        if (shopContainer != null) 
        {
            shopContainer.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[ShopManager] ⚠️ Shop Container is MISSING in the Inspector!");
        }

        WireButtons();
        VerifyInspectorAssignments();
    }

    private void Update()
    {
        // Allow the player to cleanly exit the shop via the Escape key
        if (isShopOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }
    }

    private void WireButtons()
    {
        // Hook up navigation listeners dynamically
        if (navPowerupsButton) navPowerupsButton.onClick.AddListener(() => SwitchTab(powerupsPanel));
        if (navTrophiesButton) navTrophiesButton.onClick.AddListener(() => SwitchTab(trophiesPanel));
        if (navLeaveButton) navLeaveButton.onClick.AddListener(CloseShop);
        
        if (backToDialogueButton1) backToDialogueButton1.onClick.AddListener(() => SwitchTab(npcDialoguePanel));
        if (backToDialogueButton2) backToDialogueButton2.onClick.AddListener(() => SwitchTab(npcDialoguePanel));

        if (buyCubeButton) buyCubeButton.onClick.AddListener(BuyCube);
    }

    public void OpenShop()
    {
        isShopOpen = true;
        
        // Prevent the Escape/Pause menu from opening simultaneously
        if (EscapeMenu.Instance != null) EscapeMenu.Instance.canPause = false;

        // Halt player input and physics
        if (cachedCamera != null) cachedCamera.isShopping = true;
        if (cachedMovement != null) cachedMovement.isShopping = true;

        // Free the cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        shopContainer.SetActive(true);
        SwitchTab(npcDialoguePanel);
    }

    public void CloseShop()
    {
        isShopOpen = false;
        shopContainer.SetActive(false);

        // Restore default game behavior and menus
        if (EscapeMenu.Instance != null) EscapeMenu.Instance.canPause = true;

        // Unfreeze player mechanics
        if (cachedCamera != null) cachedCamera.isShopping = false;
        if (cachedMovement != null) cachedMovement.isShopping = false;

        // Re-lock cursor to the center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SwitchTab(GameObject activePanel)
    {
        // Disable all sub-panels first to avoid rendering overlaps
        if (npcDialoguePanel) npcDialoguePanel.SetActive(false);
        if (powerupsPanel) powerupsPanel.SetActive(false);
        if (trophiesPanel) trophiesPanel.SetActive(false);

        // Render the selected target panel
        if (activePanel) activePanel.SetActive(true);
    }

    public void BuyCube()
    {
        if (GameManager.Instance != null)
        {
            // Request transaction authorization from the centralized GameManager economy
            if (GameManager.Instance.TrySpendCredits(cubePrice))
            {
                // Materialize the 3D asset into the scene context
                if (cubePrefab != null && cubeSpawnPoint != null)
                {
                    Instantiate(cubePrefab, cubeSpawnPoint.position, cubeSpawnPoint.rotation);
                    Debug.Log("[ShopManager] Cube purchased and spawned successfully!");
                }
                
                // Disable button interactivity to restrict duplicate item purchases
                if (buyCubeButton != null) buyCubeButton.interactable = false;
            }
            else
            {
                Debug.Log("[ShopManager] Transaction denied: Insufficient credit balance.");
            }
        }
        else
        {
            Debug.LogError("[ShopManager] Critical failure: GameManager instance is missing from the active environment.");
        }
    }

    private void VerifyInspectorAssignments()
    {
        if (cubePrefab == null) Debug.LogWarning("[ShopManager] ⚠️ Cube Prefab asset is unassigned!");
        if (cubeSpawnPoint == null) Debug.LogWarning("[ShopManager] ⚠️ Cube Spawn Point Transform reference is unassigned!");
        if (buyCubeButton == null) Debug.LogWarning("[ShopManager] ⚠️ Buy Cube Button UI component reference is unassigned!");
    }
}