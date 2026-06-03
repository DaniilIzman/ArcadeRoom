using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

[RequireComponent(typeof(Collider))]
public class ArcadeMachine : MonoBehaviour
{
    [Header("Game Settings")]
    public string gameName = "Space Invaders";
    public int playCost = 5;
    
    [Header("Scene Routing")]
    public string sceneToLoad; 

    private bool isPlayerInside = false;

    private void Update()
    {
        // if the player is in the zone and presses E
        if (isPlayerInside && Input.GetKeyDown(KeyCode.E))
        {
            AttemptPlayGame();
        }
    }

    private void AttemptPlayGame()
    {
        // check if the GameManager exists and if we have enough money
        if (GameManager.Instance != null && GameManager.Instance.TrySpendCredits(playCost))
        {
            Debug.Log("Starting " + gameName + "! Loading scene: " + sceneToLoad);
            
            // load the mini-game scene
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogWarning("Scene name is empty! Please assign a scene in the inspector.");
            }
        }
        else
        {

            Debug.Log("Cannot play " + gameName + ". Insufficient credits.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;

            if (UIManager.Instance != null)
            {
                // show the dynamic text when walking up to the machine
                UIManager.Instance.ShowPrompt("Press E to play " + gameName + " (" + playCost + " Credits)");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;

            if (UIManager.Instance != null)
            {
                // hide the text when walking away
                UIManager.Instance.HidePrompt();
            }
        }
    }
}