using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
public class NPCShopInteract : MonoBehaviour
{
    [Header("NPC Settings")]
    public string npcName = "Merchant";
    
    [Header("NPC Voice Lines (SFX Arrays)")]
    public AudioClip[] greetingClips;
    public AudioClip[] openShopClips;
    public AudioClip[] notEnoughCreditsClips;
    public AudioClip[] outOfStockClips;
    public AudioClip[] leaveBoughtClips;
    public AudioClip[] leaveDidNotBuyClips;

    private AudioSource npcAudioSource;
    private bool isPlayerInside = false;
    private PlayerMovement playerInZone = null;
    
    // tracks if the player made a purchase during the current interaction
    [HideInInspector] public bool hasBoughtSomethingThisVisit = false;

    private void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        
        npcAudioSource = GetComponent<AudioSource>();
        npcAudioSource.playOnAwake = false;
        npcAudioSource.spatialBlend = 1.0f; // makes the audio 3D so it comes from the NPC
    }

    private void Update()
    {
        if (isPlayerInside && playerInZone != null && playerInZone.IsGrounded)
        {
            if (Input.GetKeyDown(KeyCode.E) && ShopManager.Instance != null && !ShopManager.Instance.isShopOpen)
            {
                if (EscapeMenu.Instance != null && EscapeMenu.Instance.canPause)
                {
                    if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
                    
                    // reset purchase state for this new visit and open the shop
                    hasBoughtSomethingThisVisit = false; 
                    ShopManager.Instance.OpenShop(this); 
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
                PlayRandomVoiceLine(greetingClips);
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

    // plays a random line and interrupts the previous one so the NPC doesn't talk over themselves
    public void PlayRandomVoiceLine(AudioClip[] voiceLines)
    {
        if (voiceLines == null || voiceLines.Length == 0 || npcAudioSource == null) return;
        
        int randomIndex = Random.Range(0, voiceLines.Length);
        npcAudioSource.clip = voiceLines[randomIndex];
        npcAudioSource.Play();
    }

    public void PlayLeaveShopVoiceLine()
    {
        if (hasBoughtSomethingThisVisit) PlayRandomVoiceLine(leaveBoughtClips);
        else PlayRandomVoiceLine(leaveDidNotBuyClips);
    }
}