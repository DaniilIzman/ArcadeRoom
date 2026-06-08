using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class InvasionTrigger : MonoBehaviour
{
    [Header("Invasion Settings")]
    [Tooltip("The tag assigned to your alien prefabs.")]
    public string invaderTag = "Enemy";

    // safeguards breach actions from duplicate loop calls if multiple aliens hit the line at once
    private bool hasBreached = false; 

    private void Start()
    {
        // automatically enforce trigger state on the attached collider just in case it was missed in the inspector
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // abort immediately if the game is already ending
        if (hasBreached) return;

        // evaluate if the object crossing the threshold is actually an invader
        if (other.CompareTag(invaderTag))
        {
            hasBreached = true;

            // notify the core loop to terminate the game session
            if (SpaceInvadersManager.Instance != null)
            {
                SpaceInvadersManager.Instance.TriggerGameOver();
            }
        }
    }
}