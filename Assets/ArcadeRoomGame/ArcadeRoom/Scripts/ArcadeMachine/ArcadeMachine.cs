using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;

public class ArcadeMachine : MonoBehaviour
{
    [Header("Game Settings")]
    public string gameName = "Space Invaders";
    public int playCost = 5;
    public float loadDelay = 2f; 
    
    [Header("Scene Routing")]
    public string sceneToLoad; 

    [Header("Audio Settings")]
    public AudioClip promptSound;
    public AudioClip insertCoinSound;

    [Header("UI Transitions")]
    public Image fadeOverlay;

    private bool isPlayerInside = false;
    private bool isTransitioning = false; 
    private bool promptActive = false; 
    
    private PlayerMovement playerInZone = null;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false; 
    }

    private void Update()
    {
        if (isPlayerInside && playerInZone != null && !isTransitioning)
        {
            if (playerInZone.IsGrounded)
            {
                if (!promptActive)
                {
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowPrompt("Press E to play " + gameName + " (" + playCost + " Credits)");
                    promptActive = true;

                    if (audioSource != null && promptSound != null)
                        audioSource.PlayOneShot(promptSound);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    AttemptPlayGame();
                }
            }
            else
            {
                if (promptActive)
                {
                    if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
                    promptActive = false;
                }
            }
        }
    }

    private void AttemptPlayGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.TrySpendCredits(playCost))
        {
            if (audioSource != null && insertCoinSound != null)
                audioSource.PlayOneShot(insertCoinSound);

            StartCoroutine(PlayGameSequence());
        }
        else
        {
            Debug.Log("Cannot play " + gameName + ". Insufficient credits.");
        }
    }

    private IEnumerator PlayGameSequence()
    {
        isTransitioning = true;
        promptActive = false; 

        // prevent UI overlap fix
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

        // acade freeze target fix
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
                elapsedTime += Time.unscaledDeltaTime; // sse unscaled to ignore menu pauses
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
            
            // --- Arcade Unfreeze Target Fix ---
            if (playerInZone != null) playerInZone.isFrozenByArcade = false;
            if (cameraLook != null) cameraLook.isFrozenByArcade = false;
            if (EscapeMenu.Instance != null) EscapeMenu.Instance.UnlockMenu();

            isTransitioning = false;
            PlayerMovement.restorePosition = false; 
            PlayerCamera.restorePitch = false;

            if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
        }
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
            
            if (promptActive)
            {
                if (UIManager.Instance != null && !isTransitioning)
                    UIManager.Instance.HidePrompt();
                promptActive = false;
            }
        }
    }
}