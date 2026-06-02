using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public LayerMask interactableLayer; 

    private void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();

            if (interactable != null)
            {
                // this line sends the hover text to the UI manager.
                UIManager.Instance.ShowHoverText(interactable.hoverText);
                
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();
                }
            }
            else
            {
                UIManager.Instance.ClearHoverText();
            }
        }
        else
        {
            // this clears the text when you look away from an object.
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClearHoverText();
            }
        }
    }
}