using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

// handles player interaction with a single arcade cabinet: prompting, unlocking, and launching its game scene
public class ArcadeMachine : MonoBehaviour
{
    // display name of the game used in prompts and stored in the unlock save string
    [Header("Game Settings")]
    public string gameName = "Space Invaders";

    // how many seconds the fade and audio transition lasts before the scene loads
    public float loadDelay = 2f;

    // name of the unity scene to load when this cabinet is played
    [Header("Scene Routing")]
    public string sceneToLoad;

    // whether this cabinet requires spending credits before it can be played
    [Header("Economy & Unlocks")]
    public bool requiresUnlock = true;
    public int unlockCost = 150;
    public AudioClip unlockSound;

    // sound played when the player enters the trigger and the boot sequence starts
    [Header("Audio Settings")]
    public AudioClip promptSound;
    public AudioClip cabinetBootSound;

    // optional ui image used to fade to black before loading the game scene
    [Header("UI Transitions")]
    public Image fadeOverlay;

    private bool isPlayerInside = false;
    private bool isTransitioning = false;
    private bool promptActive = false;
    private bool isUnlocked = false;

    private PlayerMovement playerInZone = null;
    private AudioSource audioSource;

    // reference kept so the coroutine can be cancelled if the player leaves
    private Coroutine warningRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;
    }

    private void Start()
    {
        isUnlocked = CheckIfUnlocked();
    }

    private void Update()
    {
        if (isPlayerInside && playerInZone != null && !isTransitioning)
        {
            // only show the prompt and accept input while the player is on the ground
            if (playerInZone.IsGrounded)
            {
                // re-check unlock state in case credits were spent elsewhere this session
                isUnlocked = CheckIfUnlocked();

                if (!promptActive)
                {
                    if (UIManager.Instance != null)
                    {
                        // prompt text differs based on whether the cabinet is already unlocked
                        string promptText = isUnlocked
                            ? $"Press E to play {gameName}"
                            : $"Press E to unlock {gameName} ({unlockCost} Credits)";

                        UIManager.Instance.ShowPrompt(promptText);
                    }
                    promptActive = true;

                    if (audioSource != null && promptSound != null)
                        audioSource.PlayOneShot(promptSound);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (isUnlocked)
                        BootArcadeCabinet();
                    else
                        AttemptUnlock();
                }
            }
            else
            {
                // hide the prompt when the player leaves the ground
                if (promptActive)
                    ClearPromptState();
            }
        }
    }

    // returns the playerprefs key that holds all unlock data for the active save slot
    private string GetMasterUnlockKey()
    {
        int activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        return $"ArcadeUnlocks_Slot{activeSlot}";
    }

    // checks if this cabinet's game name is present in the slot's master unlock string
    private bool CheckIfUnlocked()
    {
        if (!requiresUnlock) return true;
        string unlockedData = PlayerPrefs.GetString(GetMasterUnlockKey(), "");
        return unlockedData.Contains($"[{gameName}]");
    }

    // appends this cabinet's game name to the master unlock string if not already present
    private void SaveUnlock()
    {
        string key = GetMasterUnlockKey();
        string unlockedData = PlayerPrefs.GetString(key, "");

        if (!unlockedData.Contains($"[{gameName}]"))
        {
            PlayerPrefs.SetString(key, unlockedData + $"[{gameName}],");
            PlayerPrefs.Save();
        }
    }

    // deducts credits and unlocks the cabinet, or shows an insufficient credits warning
    private void AttemptUnlock()
    {
        if (GameManager.Instance != null && GameManager.Instance.TrySpendCredits(unlockCost))
        {
            isUnlocked = true;
            SaveUnlock();

            if (audioSource != null && unlockSound != null)
                audioSource.PlayOneShot(unlockSound);

            BootArcadeCabinet();
        }
        else
        {
            // cancel any existing warning before starting a new one
            if (warningRoutine != null) StopCoroutine(warningRoutine);
            warningRoutine = StartCoroutine(FlashWarningSequence());
        }
    }

    // briefly shows an insufficient credits message then clears the prompt flag
    private IEnumerator FlashWarningSequence()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPrompt("<color=red>INSUFFICIENT CREDITS!</color>");

        yield return new WaitForSecondsRealtime(1.5f);

        promptActive = false;
    }

    // plays the boot sound and starts the scene transition sequence
    private void BootArcadeCabinet()
    {
        if (audioSource != null && cabinetBootSound != null)
            audioSource.PlayOneShot(cabinetBootSound);

        StartCoroutine(PlayGameSequence());
    }

    // saves the player's position and camera pitch, fades to black, then loads the game scene
    private IEnumerator PlayGameSequence()
    {
        isTransitioning = true;
        promptActive = false;

        // close and lock the escape menu so it can't be opened during the transition
        if (EscapeMenu.Instance != null) EscapeMenu.Instance.ForceCloseAndLock();

        PlayerCamera cameraLook = null;
        if (playerInZone != null) cameraLook = playerInZone.GetComponentInChildren<PlayerCamera>();

        // store the player's world position and rotation so they can be restored on return
        if (playerInZone != null)
        {
            PlayerMovement.savedPos = playerInZone.transform.position;
            PlayerMovement.savedRot = playerInZone.transform.rotation;
            PlayerMovement.restorePosition = true;
        }

        // store the camera's current pitch so it can be restored on return
        if (cameraLook != null)
        {
            PlayerCamera.savedPitch = cameraLook.GetCurrentPitch();
            PlayerCamera.restorePitch = true;
        }

        // freeze both movement and camera input during the transition
        if (playerInZone != null) playerInZone.isFrozenByArcade = true;
        if (cameraLook != null) cameraLook.isFrozenByArcade = true;

        if (UIManager.Instance != null) UIManager.Instance.ShowPrompt("Loading " + gameName + "...");

        // fade out the global ambient music and any spatial emitter on this cabinet
        if (AmbientAudio.Instance != null) AmbientAudio.Instance.FadeOut(loadDelay);
        SpatialAudioEmitter localEmitter = GetComponent<SpatialAudioEmitter>();
        if (localEmitter != null) localEmitter.FadeOut(loadDelay);

        // animate the fade overlay to black over the load delay duration
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color fadeColor = fadeOverlay.color;
            float elapsedTime = 0f;

            while (elapsedTime < loadDelay)
            {
                // use unscaled time so the fade works even if timescale is zero
                elapsedTime += Time.unscaledDeltaTime;
                fadeColor.a = Mathf.Clamp01(elapsedTime / loadDelay);
                fadeOverlay.color = fadeColor;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(loadDelay);
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            // no scene was assigned; cancel the transition and restore the player
            Debug.LogWarning("Scene name is empty! Unfreezing player.");

            if (playerInZone != null) playerInZone.isFrozenByArcade = false;
            if (cameraLook != null) cameraLook.isFrozenByArcade = false;
            if (EscapeMenu.Instance != null) EscapeMenu.Instance.UnlockMenu();

            isTransitioning = false;
            PlayerMovement.restorePosition = false;
            PlayerCamera.restorePitch = false;

            if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
        }
    }

    // stops any active warning coroutine and hides the ui prompt
    private void ClearPromptState()
    {
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
        promptActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // register the player and their movement component when they enter the zone
        if (other.CompareTag("Player") && !isTransitioning)
        {
            isPlayerInside = true;
            playerInZone = other.GetComponent<PlayerMovement>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            playerInZone = null;

            // clean up any active prompt or warning when the player leaves
            if (promptActive || warningRoutine != null)
                ClearPromptState();
        }
    }

    // editor utility that wipes all machine unlocks for the active slot so they can be re-tested
    [ContextMenu("Debug Lock Machine Data")]
    public void DebugLockMachine()
    {
        PlayerPrefs.DeleteKey(GetMasterUnlockKey());
        PlayerPrefs.Save();
        isUnlocked = !requiresUnlock;
        promptActive = false;

        int activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        Debug.Log($"All machines wiped for Slot {activeSlot}.");
    }
}