using UnityEngine;

// placed at the player's end of the play field; triggers game over when any invader crosses it
[RequireComponent(typeof(BoxCollider))]
public class InvasionTrigger : MonoBehaviour
{
    // tag that invader prefabs must have for the trigger to recognise them
    [Header("Invasion Settings")]
    [Tooltip("The tag assigned to your alien prefabs.")]
    public string invaderTag = "Enemy";

    // prevents game over from firing more than once if multiple invaders cross in the same frame
    private bool hasBreached = false;

    private void Start()
    {
        // enforce trigger mode on the collider in case it was left unchecked in the inspector
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // abort if the ending sequence has already been started
        if (hasBreached) return;

        if (other.CompareTag(invaderTag))
        {
            hasBreached = true;

            if (SpaceInvadersManager.Instance != null)
                SpaceInvadersManager.Instance.TriggerGameOver();
        }
    }
}