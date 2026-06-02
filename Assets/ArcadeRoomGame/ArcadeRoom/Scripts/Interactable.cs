using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string hoverText = "Press E to interact";
    
    // this variable tracks whether the player is currently standing inside the trigger zone
    protected bool isPlayerInside = false;

    public virtual void Interact()
    {
        Debug.Log("Interacted with a base object.");
    }

    protected virtual void Update()
    {
        // if the player is inside the zone and presses E, execute the interaction logic.
        if (isPlayerInside)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // ensure the object entering the trigger area has the Player tag assigned to it
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowHoverText(hoverText);
            }
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        // clear the text and reset the state when the player walks away
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClearHoverText();
            }
        }
    }
}