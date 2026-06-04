using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class ShopSaveData
{
    public List<string> boughtItems = new List<string>(); 
}

[System.Serializable]
public class ShopItem
{
    public string inspectorName; 
    public string uniqueID; 
    public int price;
    public GameObject prefab;
    public Transform spawnPoint;
    public Button buyButton;
    [HideInInspector] public bool isSoldOut;
}

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

    [Header("Shop Inventory")]
    public ShopItem[] shopItems;

    [HideInInspector] public bool isShopOpen = false;
    
    private PlayerCamera cachedCamera;
    private PlayerMovement cachedMovement;
    private NPCShopInteract currentNPC; 

    private string saveFilePath;
    private ShopSaveData saveData = new ShopSaveData();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        saveFilePath = Application.persistentDataPath + "/shopProgress.json";
    }

    private void Start()
    {
        cachedCamera = Object.FindFirstObjectByType<PlayerCamera>();
        cachedMovement = Object.FindFirstObjectByType<PlayerMovement>();

        if (shopContainer != null) shopContainer.SetActive(false);
        
        LoadGameData(); 
        WireNavigationButtons();
        InitializeInventory();
    }

    private void Update()
    {
        if (isShopOpen && Input.GetKeyDown(KeyCode.Escape)) CloseShop();
    }

    private void LoadGameData()
    {
        if (File.Exists(saveFilePath))
        {
            string jsonContent = File.ReadAllText(saveFilePath);
            saveData = JsonUtility.FromJson<ShopSaveData>(jsonContent);
            Debug.Log("Game Loaded successfully from: " + saveFilePath);
        }
        else
        {
            saveData = new ShopSaveData();
            Debug.Log("No save file found. Creating new save data.");
        }
    }

    private void SaveGameData()
    {
        string jsonContent = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, jsonContent);
    }

    private void WireNavigationButtons()
    {
        if (navPowerupsButton) navPowerupsButton.onClick.AddListener(() => SwitchTab(powerupsPanel));
        if (navTrophiesButton) navTrophiesButton.onClick.AddListener(() => SwitchTab(trophiesPanel));
        if (navLeaveButton) navLeaveButton.onClick.AddListener(CloseShop);
        if (backToDialogueButton1) backToDialogueButton1.onClick.AddListener(() => SwitchTab(npcDialoguePanel));
        if (backToDialogueButton2) backToDialogueButton2.onClick.AddListener(() => SwitchTab(npcDialoguePanel));

        Button[] navButtons = { navPowerupsButton, navTrophiesButton, navLeaveButton, backToDialogueButton1, backToDialogueButton2 };
        foreach (Button btn in navButtons)
        {
            if (btn != null) btn.onClick.AddListener(() => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });
        }
    }

    private void InitializeInventory()
    {
        foreach (ShopItem item in shopItems)
        {
            item.isSoldOut = saveData.boughtItems.Contains(item.uniqueID);
            
            if (item.isSoldOut) 
            {
                UpdateItemButtonUI(item);
                
                // spawn the physical item into the world if it was loaded from the save file
                if (item.prefab != null && item.spawnPoint != null) 
                {
                    Instantiate(item.prefab, item.spawnPoint.position, item.spawnPoint.rotation);
                }
            }

            if (item.buyButton != null)
            {
                ShopItem capturedItem = item; 
                capturedItem.buyButton.onClick.AddListener(() => BuyItem(capturedItem));
                capturedItem.buyButton.onClick.AddListener(() => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });
            }
        }
    }

    private void UpdateItemButtonUI(ShopItem item)
    {
        if (item.buyButton != null)
        {
            TextMeshProUGUI btnText = item.buyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "Sold Out";
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

    public void BuyItem(ShopItem item)
    {
        if (item.isSoldOut)
        {
            if (currentNPC != null) currentNPC.PlayRandomVoiceLine(currentNPC.outOfStockClips);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.TrySpendCredits(item.price))
        {
            if (item.prefab != null && item.spawnPoint != null) Instantiate(item.prefab, item.spawnPoint.position, item.spawnPoint.rotation);
            
            item.isSoldOut = true; 
            
            saveData.boughtItems.Add(item.uniqueID);
            SaveGameData();

            if (currentNPC != null) currentNPC.hasBoughtSomethingThisVisit = true;
            UpdateItemButtonUI(item);
        }
        else if (currentNPC != null)
        {
            currentNPC.PlayRandomVoiceLine(currentNPC.notEnoughCreditsClips);
        }
    }

    public void ResetShopProgress()
    {
        saveData.boughtItems.Clear();
        SaveGameData(); 
        Debug.Log("DEBUG: Shop JSON save file has been wiped.");
    }
}