using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class NPCShopInteract : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Merchant";
    
    private bool isPlayerInside = false;
    private PlayerMovement playerInZone = null;

    private void Start()
    {
        // ensure the collider is set to trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Update()
    {
        if (isPlayerInside && playerInZone != null && playerInZone.IsGrounded)
        {
            // only allow interaction if the shop isn't already open and the escape menu is closed
            if (Input.GetKeyDown(KeyCode.E) && ShopManager.Instance != null && !ShopManager.Instance.isShopOpen)
            {
                if (EscapeMenu.Instance != null && EscapeMenu.Instance.canPause)
                {
                    if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
                    ShopManager.Instance.OpenShop();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            playerInZone = other.GetComponent<PlayerMovement>();
            if (UIManager.Instance != null && (!ShopManager.Instance || !ShopManager.Instance.isShopOpen))
            {
                UIManager.Instance.ShowPrompt("Press E to talk to " + npcName);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            playerInZone = null;
            if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
        }
    }
}