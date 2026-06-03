using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // CRITICAL: Required for interacting with UI Images
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class ArcadeMachine : MonoBehaviour
{
    [Header("Game Settings")]
    public string gameName = "Space Invaders";
    public int playCost = 5;
    public float loadDelay = 2f; 
    
    [Header("Scene Routing")]
    public string sceneToLoad; 

    [Header("Audio Settings")]
    public AudioClip insertCoinSound;

    [Header("UI Transitions")]
    [Tooltip("Assign a full-screen black UI Image to fade out during load.")]
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
                    {
                        UIManager.Instance.ShowPrompt("Press E to play " + gameName + " (" + playCost + " Credits)");
                    }
                    promptActive = true;
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
        promptActive = false; 

        PlayerCamera cameraLook = null;
        if (playerInZone != null) cameraLook = playerInZone.GetComponentInChildren<PlayerCamera>();

        // save position/rotation
        if (playerInZone != null)
        {
            PlayerMovement.savedPos = playerInZone.transform.position;
            PlayerMovement.savedRot = playerInZone.transform.rotation;
            PlayerMovement.restorePosition = true;
            playerInZone.isFrozen = true;
        }
        if (cameraLook != null)
        {
            PlayerCamera.savedPitch = cameraLook.GetCurrentPitch();
            PlayerCamera.restorePitch = true;
            cameraLook.isFrozen = true;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPrompt("Loading " + gameName + "...");
        }

        // fade logic
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color fadeColor = fadeOverlay.color;
            float elapsedTime = 0f;

            // smoothly increase alpha from 0 to 1 over the loadDelay duration
            while (elapsedTime < loadDelay)
            {
                elapsedTime += Time.deltaTime;
                fadeColor.a = Mathf.Clamp01(elapsedTime / loadDelay);
                fadeOverlay.color = fadeColor;
                yield return null; // wait for the exact next frame
            }
        }
        else
        {
            // fallback if you forget to assign the image
            yield return new WaitForSeconds(loadDelay);
        }

        // Load the scene
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
                if (UIManager.Instance != null && !isTransitioning) UIManager.Instance.HidePrompt();
                promptActive = false;
            }
        }
    }
}