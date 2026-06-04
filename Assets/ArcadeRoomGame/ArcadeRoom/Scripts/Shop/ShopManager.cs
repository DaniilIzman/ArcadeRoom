using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("UI Panels")]
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
    
    private bool hasBoughtCube = false; 
    private PlayerCamera cachedCamera;
    private PlayerMovement cachedMovement;
    private NPCShopInteract currentNPC; 

    //a constant string key for our save file
    private const string SAVE_KEY_CUBE_BOUGHT = "Shop_HasBoughtCube";

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        cachedCamera = Object.FindFirstObjectByType<PlayerCamera>();
        cachedMovement = Object.FindFirstObjectByType<PlayerMovement>();

        if (shopContainer != null) shopContainer.SetActive(false);
        WireButtons();

        // load the saved state from the hard drive
        hasBoughtCube = PlayerPrefs.GetInt(SAVE_KEY_CUBE_BOUGHT, 0) == 1;
        
        // if they already bought it in a previous session, visually update the button immediately
        if (hasBoughtCube && buyCubeButton != null)
        {
            TextMeshProUGUI btnText = buyCubeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "Sold Out";
        }
    }

    private void Update()
    {
        if (isShopOpen && Input.GetKeyDown(KeyCode.Escape)) CloseShop();
    }

    private void WireButtons()
    {
        if (navPowerupsButton) navPowerupsButton.onClick.AddListener(() => SwitchTab(powerupsPanel));
        if (navTrophiesButton) navTrophiesButton.onClick.AddListener(() => SwitchTab(trophiesPanel));
        if (navLeaveButton) navLeaveButton.onClick.AddListener(CloseShop);
        if (backToDialogueButton1) backToDialogueButton1.onClick.AddListener(() => SwitchTab(npcDialoguePanel));
        if (backToDialogueButton2) backToDialogueButton2.onClick.AddListener(() => SwitchTab(npcDialoguePanel));
        if (buyCubeButton) buyCubeButton.onClick.AddListener(BuyCube);

        Button[] allButtons = { navPowerupsButton, navTrophiesButton, navLeaveButton, backToDialogueButton1, backToDialogueButton2, buyCubeButton };
        foreach (Button btn in allButtons)
        {
            if (btn != null) btn.onClick.AddListener(() => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });
        }
    }

    public void OpenShop(NPCShopInteract interactingNPC)
    {
        isShopOpen = true;
        currentNPC = interactingNPC;
        
        if (EscapeMenu.Instance != null) EscapeMenu.Instance.canPause = false;
        if (cachedCamera != null) cachedCamera.isShopping = true;
        if (cachedMovement != null) cachedMovement.isShopping = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        shopContainer.SetActive(true);
        SwitchTab(npcDialoguePanel);

        if (currentNPC != null) currentNPC.PlayRandomVoiceLine(currentNPC.openShopClips);
    }

    public void CloseShop()
    {
        isShopOpen = false;
        shopContainer.SetActive(false);

        if (EscapeMenu.Instance != null) EscapeMenu.Instance.canPause = true;
        if (cachedCamera != null) cachedCamera.isShopping = false;
        if (cachedMovement != null) cachedMovement.isShopping = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (currentNPC != null) 
        {
            currentNPC.PlayLeaveShopVoiceLine();
            currentNPC = null; 
        }
    }

    private void SwitchTab(GameObject activePanel)
    {
        if (npcDialoguePanel) npcDialoguePanel.SetActive(false);
        if (powerupsPanel) powerupsPanel.SetActive(false);
        if (trophiesPanel) trophiesPanel.SetActive(false);
        if (activePanel) activePanel.SetActive(true);
    }

    public void BuyCube()
    {
        if (hasBoughtCube)
        {
            if (currentNPC != null) currentNPC.PlayRandomVoiceLine(currentNPC.outOfStockClips);
            return;
        }

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.TrySpendCredits(cubePrice))
            {
                if (cubePrefab != null && cubeSpawnPoint != null) Instantiate(cubePrefab, cubeSpawnPoint.position, cubeSpawnPoint.rotation);
                
                hasBoughtCube = true; 
                
                // save the purchase to the hard drive immediately
                PlayerPrefs.SetInt(SAVE_KEY_CUBE_BOUGHT, 1);
                PlayerPrefs.Save();

                if (currentNPC != null) currentNPC.hasBoughtSomethingThisVisit = true;

                if (buyCubeButton != null)
                {
                    TextMeshProUGUI btnText = buyCubeButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = "Sold Out";
                }
            }
            else
            {
                if (currentNPC != null) currentNPC.PlayRandomVoiceLine(currentNPC.notEnoughCreditsClips);
            }
        }
    }
}