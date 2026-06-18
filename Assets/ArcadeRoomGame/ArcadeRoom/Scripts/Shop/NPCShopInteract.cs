using UnityEngine;

// requires a box collider used as the interaction trigger and an audiosource for voice lines
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
public class NPCShopInteract : MonoBehaviour
{
    // display name shown in the interaction prompt
    [Header("NPC Settings")]
    public string npcName = "Merchant";

    // voice line arrays played at different points during a shop visit
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

    // reset to false each time the shop opens so the correct farewell line is chosen on close
    [HideInInspector] public bool hasBoughtSomethingThisVisit = false;

    // timestamp and duration used to prevent the greeting from spamming on repeated entry
    private float lastGreetTime;
    private float greetCooldown = 3.0f;

    private void Start()
    {
        // route npc voice lines through the sfx mixer group
        SettingsManager.Instance?.Route(npcAudioSource, SettingsManager.AudioCategory.SFX);
        GetComponent<BoxCollider>().isTrigger = true;

        npcAudioSource = GetComponent<AudioSource>();
        npcAudioSource.playOnAwake = false;

        // fully positional 3d audio so voice lines appear to come from the npc's location
        npcAudioSource.spatialBlend = 1.0f;
    }

    private void Update()
    {
        if (isPlayerInside && playerInZone != null && playerInZone.IsGrounded)
        {
            // only open the shop if the escape menu is in a pauseable state and the shop isn't already open
            if (Input.GetKeyDown(KeyCode.E) && ShopManager.Instance != null && !ShopManager.Instance.isShopOpen)
            {
                if (EscapeMenu.Instance != null && EscapeMenu.Instance.canPause)
                {
                    if (UIManager.Instance != null) UIManager.Instance.HidePrompt();

                    // clear purchase tracking for the new visit before opening
                    hasBoughtSomethingThisVisit = false;
                    ShopManager.Instance.OpenShop(this);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // guard against duplicate triggers from low framerate or overlapping colliders
        if (other.CompareTag("Player") && !isPlayerInside)
        {
            isPlayerInside = true;
            playerInZone = other.GetComponent<PlayerMovement>();

            if (UIManager.Instance != null && (!ShopManager.Instance || !ShopManager.Instance.isShopOpen))
            {
                UIManager.Instance.ShowPrompt("Press E to talk to " + npcName);

                // only play the greeting if enough time has passed since the last one
                if (Time.unscaledTime - lastGreetTime > greetCooldown)
                {
                    PlayRandomVoiceLine(greetingClips);
                    lastGreetTime = Time.unscaledTime;
                }
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

    // picks a random clip from the given array and plays it, interrupting any currently playing line
    public void PlayRandomVoiceLine(AudioClip[] voiceLines)
    {
        if (voiceLines == null || voiceLines.Length == 0 || npcAudioSource == null) return;

        int randomIndex = Random.Range(0, voiceLines.Length);
        npcAudioSource.clip = voiceLines[randomIndex];
        npcAudioSource.Play();
    }

    // plays the appropriate farewell line depending on whether the player bought anything this visit
    public void PlayLeaveShopVoiceLine()
    {
        if (hasBoughtSomethingThisVisit) PlayRandomVoiceLine(leaveBoughtClips);
        else PlayRandomVoiceLine(leaveDidNotBuyClips);
    }
}