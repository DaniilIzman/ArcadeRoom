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
    [Tooltip("The sound effect that plays when the 'Press E' UI prompt appears.")]
    public AudioClip promptSound;
    [Tooltip("The sound effect that plays the instant credits are spent.")]
    public AudioClip insertCoinSound;

    [Header("UI Transitions")]
    [Tooltip("Assign a full-screen black UI Image to fade out during load.")]
    public Image fadeOverlay;

    private bool isPlayerInside = false;
    private bool isTransitioning = false; 
    private bool promptActive = false; // tracks if the text is currently visible
    
    private PlayerMovement playerInZone = null;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false; 
    }

    private void Update()
    {
        // continuously monitor the player's feet while they are inside the trigger
        if (isPlayerInside && playerInZone != null && !isTransitioning)
        {
            if (playerInZone.IsGrounded)
            {
                // show prompt when grounded
                if (!promptActive)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowPrompt("Press E to play " + gameName + " (" + playCost + " Credits)");
                    }
                    promptActive = true;

                    // play a quick notification chime when the prompt appears
                    if (audioSource != null && promptSound != null)
                    {
                        audioSource.PlayOneShot(promptSound);
                    }
                }

                // only accept input if they are grounded
                if (Input.GetKeyDown(KeyCode.E))
                {
                    AttemptPlayGame();
                }
            }
            else
            {
                // instantly hide the prompt if the player jumps or falls
                if (promptActive)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.HidePrompt();
                    }
                    promptActive = false;
                }
            }
        }
    }

    private void AttemptPlayGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.TrySpendCredits(playCost))
        {
            // play the sound feedback immediately on credit deduction
            if (audioSource != null && insertCoinSound != null)
            {
                audioSource.PlayOneShot(insertCoinSound);
            }

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
        promptActive = false; // reset toggle so it's clean when we return

        PlayerCamera cameraLook = null;
        if (playerInZone != null)
        {
            cameraLook = playerInZone.GetComponentInChildren<PlayerCamera>();
        }

        // save coordinates
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

        // freeze the player
        if (playerInZone != null) playerInZone.isFrozen = true;
        if (cameraLook != null) cameraLook.isFrozen = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPrompt("Loading " + gameName + "...");
        }

        // fade out the background lobby room audio 
        if (AmbientAudio.Instance != null)
        {
            AmbientAudio.Instance.FadeOut(loadDelay);
        }

        // check for and fade out local ambient audio if attached
        SpatialAudioEmitter localEmitter = GetComponent<SpatialAudioEmitter>();
        if (localEmitter != null)
        {
            localEmitter.FadeOut(loadDelay);
        }

        // countdown delay (with UI visual fade)
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color fadeColor = fadeOverlay.color;
            float elapsedTime = 0f;

            // smoothly increase alpha 
            while (elapsedTime < loadDelay)
            {
                elapsedTime += Time.deltaTime;
                fadeColor.a = Mathf.Clamp01(elapsedTime / loadDelay);
                fadeOverlay.color = fadeColor;
                yield return null; 
            }
        }
        else
        {
            yield return new WaitForSeconds(loadDelay);
        }

        // load Scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("Scene name is empty! Unfreezing player.");
            if (playerInZone != null) playerInZone.isFrozen = false;
            if (cameraLook != null) cameraLook.isFrozen = false;
            isTransitioning = false;
            PlayerMovement.restorePosition = false; 
            PlayerCamera.restorePitch = false;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.HidePrompt();
            }
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
                {
                    UIManager.Instance.HidePrompt();
                }
                promptActive = false;
            }
        }
    }
}