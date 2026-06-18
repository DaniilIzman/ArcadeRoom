using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

// serializable container persisted as json to track which items have been purchased
[System.Serializable]
public class ShopSaveData
{
    public List<string> boughtItems = new List<string>();
}

// data for a single purchasable item in the shop
[System.Serializable]
public class ShopItem
{
    public string inspectorName;

    // unique string key used to identify this item in the save file
    public string uniqueID;
    public int price;

    // prefab spawned in the world when this item is purchased
    public GameObject prefab;

    // where in the scene the prefab is instantiated on purchase
    public Transform spawnPoint;
    public Button buyButton;

    // set to true once the item has been purchased so it cannot be bought again
    [HideInInspector] public bool isSoldOut;
}

// singleton that manages the in-game shop: opening, purchasing, saving, and closing
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    // top-level container and the two tab panels shown inside the shop ui
    [Header("ui panels")]
    public GameObject shopContainer;
    public GameObject npcDialoguePanel;
    public GameObject trophiesPanel;

    // buttons used to switch tabs and close the shop
    [Header("navigation buttons")]
    public Button navTrophiesButton;
    public Button navLeaveButton;
    public Button backToDialogueButton;

    // all purchasable items configured in the inspector
    [Header("shop inventory")]
    public ShopItem[] shopItems;

    // read by other scripts to know whether to block input while the shop is open
    [HideInInspector] public bool isShopOpen = false;

    // cached at startup to avoid repeated scene searches
    private PlayerCamera cachedCamera;
    private PlayerMovement cachedMovement;

    // the npc that opened this shop session, used to trigger farewell voice lines
    private NPCShopInteract currentNPC;

    // file path for the json save file, built using the active slot number
    private string saveFilePath;
    private ShopSaveData saveData = new ShopSaveData();
    private int activeSlot;

    // total number of items in the shop and how many have been purchased, used by the completion system
    public int TotalItems  => shopItems != null ? shopItems.Length : 0;
    public int BoughtCount => saveData != null ? saveData.boughtItems.Count : 0;

    // returns true only when every item in the shop has been bought
    public bool AllItemsBought()
    {
        if (shopItems == null || shopItems.Length == 0) return false;
        foreach (ShopItem item in shopItems)
            if (!saveData.boughtItems.Contains(item.uniqueID)) return false;
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // build the save file path for the active slot on awake so it's ready before start
        activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        saveFilePath = Application.persistentDataPath + $"/shopProgress_Slot{activeSlot}.json";
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
        // allow the player to close the shop with escape as an alternative to the leave button
        if (isShopOpen && Input.GetKeyDown(KeyCode.Escape)) CloseShop();
    }

    // reads the json save file if it exists, otherwise initialises empty save data
    private void LoadGameData()
    {
        if (File.Exists(saveFilePath))
        {
            string jsonContent = File.ReadAllText(saveFilePath);
            saveData = JsonUtility.FromJson<ShopSaveData>(jsonContent);
        }
        else
        {
            saveData = new ShopSaveData();
        }
    }

    // serialises the current save data to json and writes it to disk
    private void SaveGameData()
    {
        string jsonContent = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, jsonContent);
    }

    // attaches click listeners to the tab navigation and leave buttons, including click sounds
    private void WireNavigationButtons()
    {
        if (navTrophiesButton) navTrophiesButton.onClick.AddListener(() => SwitchTab(trophiesPanel));
        if (navLeaveButton) navLeaveButton.onClick.AddListener(CloseShop);
        if (backToDialogueButton) backToDialogueButton.onClick.AddListener(() => SwitchTab(npcDialoguePanel));

        Button[] navButtons = { navTrophiesButton, navLeaveButton, backToDialogueButton };
        foreach (Button btn in navButtons)
        {
            if (btn != null) btn.onClick.AddListener(() => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });
        }
    }

    // marks already-purchased items as sold out, spawns their prefabs, and wires buy button listeners
    private void InitializeInventory()
    {
        foreach (ShopItem item in shopItems)
        {
            item.isSoldOut = saveData.boughtItems.Contains(item.uniqueID);

            if (item.isSoldOut)
            {
                UpdateItemButtonUI(item);

                // re-spawn the prefab so previously bought items appear in the scene on load
                if (item.prefab != null && item.spawnPoint != null)
                    Instantiate(item.prefab, item.spawnPoint.position, item.spawnPoint.rotation);
            }

            if (item.buyButton != null)
            {
                // capture a local reference so the lambda closes over the correct item
                ShopItem capturedItem = item;
                capturedItem.buyButton.onClick.AddListener(() => BuyItem(capturedItem));
                capturedItem.buyButton.onClick.AddListener(() => { if (UIManager.Instance) UIManager.Instance.PlayClickSound(); });
            }
        }
    }

    // changes the buy button label to "sold out" when an item has been purchased
    private void UpdateItemButtonUI(ShopItem item)
    {
        if (item.buyButton != null)
        {
            TextMeshProUGUI btnText = item.buyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "Sold Out";
        }
    }

    // freezes the player and camera, shows the shop ui, and plays the npc's open-shop voice line
    public void OpenShop(NPCShopInteract interactingNPC)
    {
        isShopOpen = true;
        currentNPC = interactingNPC;

        // prevent the escape menu from interrupting while the shop is open
        if (EscapeMenu.Instance != null) EscapeMenu.Instance.canPause = false;
        if (cachedCamera != null) cachedCamera.isShopping = true;
        if (cachedMovement != null) cachedMovement.isShopping = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        shopContainer.SetActive(true);
        SwitchTab(npcDialoguePanel);

        if (currentNPC != null) currentNPC.PlayRandomVoiceLine(currentNPC.openShopClips);
    }

    // restores player and camera state, hides the shop ui, and triggers the npc's farewell line
    public void CloseShop()
    {
        isShopOpen = false;
        shopContainer.SetActive(false);

        // re-enable the escape menu once the shop is closed
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

    // hides all tab panels then activates the requested one
    private void SwitchTab(GameObject activePanel)
    {
        if (npcDialoguePanel) npcDialoguePanel.SetActive(false);
        if (trophiesPanel) trophiesPanel.SetActive(false);
        if (activePanel) activePanel.SetActive(true);
    }

    // attempts to purchase the given item; deducts credits, spawns the prefab, and saves on success
    public void BuyItem(ShopItem item)
    {
        if (item.isSoldOut)
        {
            // play the out-of-stock voice line if the player clicks a sold-out button
            if (currentNPC != null) currentNPC.PlayRandomVoiceLine(currentNPC.outOfStockClips);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.TrySpendCredits(item.price))
        {
            if (item.prefab != null && item.spawnPoint != null)
                Instantiate(item.prefab, item.spawnPoint.position, item.spawnPoint.rotation);

            item.isSoldOut = true;
            saveData.boughtItems.Add(item.uniqueID);
            SaveGameData();

            // flag the purchase so the correct farewell line plays when the shop closes
            if (currentNPC != null) currentNPC.hasBoughtSomethingThisVisit = true;
            UpdateItemButtonUI(item);
        }
        else if (currentNPC != null)
        {
            // play the not-enough-credits voice line when the purchase fails
            currentNPC.PlayRandomVoiceLine(currentNPC.notEnoughCreditsClips);
        }
    }

    // clears all purchase records and writes the empty save to disk
    public void ResetShopProgress()
    {
        saveData.boughtItems.Clear();
        SaveGameData();
    }
}