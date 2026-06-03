using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class ArcadeMachine : MonoBehaviour
{
    [Header("Game Settings")]
    public string gameName = "Space Invaders";
    public int playCost = 5;
    public float loadDelay = 2f; 
    
    [Header("Scene Routing")]
    public string sceneToLoad; 

    private bool isPlayerInside = false;
    private bool isTransitioning = false; 
    private bool promptActive = false; // Tracks if the text is currently visible
    
    private PlayerMovement playerInZone = null;

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

        // countdown delay
        yield return new WaitForSeconds(loadDelay);

        // load scene
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
            // grab the PlayerMovement script once when they enter so we can read IsGrounded in Update()
            playerInZone = other.GetComponent<PlayerMovement>(); 
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            playerInZone = null;
            
            // clean up the prompt as the player walk away
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