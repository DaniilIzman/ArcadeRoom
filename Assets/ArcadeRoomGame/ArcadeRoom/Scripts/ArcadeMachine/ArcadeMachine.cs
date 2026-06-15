using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;

public class ArcadeMachine : MonoBehaviour
{
    [Header("Game Settings")]
    public string gameName = "Space Invaders";
    public float loadDelay = 2f; 
    
    [Header("Scene Routing")]
    public string sceneToLoad; 

    [Header("Economy & Unlocks")]
    public bool requiresUnlock = true;
    public int unlockCost = 150;
    public AudioClip unlockSound;

    [Header("Audio Settings")]
    public AudioClip promptSound;
    public AudioClip cabinetBootSound; 

    [Header("UI Transitions")]
    public Image fadeOverlay;

    private bool isPlayerInside = false;
    private bool isTransitioning = false; 
    private bool promptActive = false; 
    private bool isUnlocked = false;
    
    private PlayerMovement playerInZone = null;
    private AudioSource audioSource;
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
            if (playerInZone.IsGrounded)
            {
                isUnlocked = CheckIfUnlocked();

                if (!promptActive)
                {
                    if (UIManager.Instance != null)
                    {
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
                    {
                        BootArcadeCabinet();
                    }
                    else
                    {
                        AttemptUnlock();
                    }
                }
            }
            else
            {
                if (promptActive)
                {
                    ClearPromptState();
                }
            }
        }
    }

    private string GetMasterUnlockKey()
    {
        int activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        return $"ArcadeUnlocks_Slot{activeSlot}";
    }

    private bool CheckIfUnlocked()
    {
        if (!requiresUnlock) return true;
        
        string unlockedData = PlayerPrefs.GetString(GetMasterUnlockKey(), "");
        return unlockedData.Contains($"[{gameName}]");
    }

    private void SaveUnlock()
    {
        string key = GetMasterUnlockKey();
        string unlockedData = PlayerPrefs.GetString(key, "");
        
        if (!unlockedData.Contains($"[{gameName}]"))
        {
            // Append this game to the master list
            PlayerPrefs.SetString(key, unlockedData + $"[{gameName}],");
            PlayerPrefs.Save();
        }
    }

    private void AttemptUnlock()
    {
        if (GameManager.Instance != null && GameManager.Instance.TrySpendCredits(unlockCost))
        {
            isUnlocked = true;
            SaveUnlock(); // Save to the master list

            if (audioSource != null && unlockSound != null)
                audioSource.PlayOneShot(unlockSound);

            BootArcadeCabinet();
        }
        else
        {
            if (warningRoutine != null) StopCoroutine(warningRoutine);
            warningRoutine = StartCoroutine(FlashWarningSequence());
        }
    }

    private IEnumerator FlashWarningSequence()
    {
        if (UIManager.Instance != null) 
            UIManager.Instance.ShowPrompt("<color=red>INSUFFICIENT CREDITS!</color>");
        
        yield return new WaitForSecondsRealtime(1.5f);
        
        promptActive = false; 
    }

    private void BootArcadeCabinet()
    {
        if (audioSource != null && cabinetBootSound != null)
            audioSource.PlayOneShot(cabinetBootSound);

        StartCoroutine(PlayGameSequence());
    }

    private IEnumerator PlayGameSequence()
    {
        isTransitioning = true;
        promptActive = false; 

        if (EscapeMenu.Instance != null) EscapeMenu.Instance.ForceCloseAndLock();

        PlayerCamera cameraLook = null;
        if (playerInZone != null) cameraLook = playerInZone.GetComponentInChildren<PlayerCamera>();

        if (playerInZone != null)
        {
            PlayerMovement.savedPos = playerInZone.transform.position;
            PlayerMovement.savedRot = playerInZone.transform.rotation;
            PlayerMovement.restorePosition = true;
        }
        if (cameraLook != null)
        {
            PlayerCamera.savedPitch = cameraLook.GetCurrentPitch();
            PlayerCamera.restorePitch = true;
        }

        if (playerInZone != null) playerInZone.isFrozenByArcade = true;
        if (cameraLook != null) cameraLook.isFrozenByArcade = true;

        if (UIManager.Instance != null) UIManager.Instance.ShowPrompt("Loading " + gameName + "...");

        if (AmbientAudio.Instance != null) AmbientAudio.Instance.FadeOut(loadDelay);

        SpatialAudioEmitter localEmitter = GetComponent<SpatialAudioEmitter>();
        if (localEmitter != null) localEmitter.FadeOut(loadDelay);

        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color fadeColor = fadeOverlay.color;
            float elapsedTime = 0f;

            while (elapsedTime < loadDelay)
            {
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

    private void ClearPromptState()
    {
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
        promptActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
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
            
            if (promptActive || warningRoutine != null)
            {
                ClearPromptState();
            }
        }
    }

    [ContextMenu("Debug Lock Machine Data")]
    public void DebugLockMachine()
    {
        // Wipes the master key for this slot
        PlayerPrefs.DeleteKey(GetMasterUnlockKey());
        PlayerPrefs.Save();
        isUnlocked = !requiresUnlock;
        promptActive = false;
        
        int activeSlot = PlayerPrefs.GetInt("Global_LastPlayedSlot", 1);
        Debug.Log($"All machines wiped for Slot {activeSlot}.");
    }
}