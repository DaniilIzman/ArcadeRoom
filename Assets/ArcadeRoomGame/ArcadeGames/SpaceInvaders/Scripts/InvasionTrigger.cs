using UnityEngine;

public class InvasionTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // if the object entering the trigger has the InvaderCollision script on it
        if (other.GetComponent<InvaderCollision>() != null)
        {
            Debug.Log("THE INVADERS HAVE BREACHED OUR DEFENSES!");
            
            if (SpaceInvadersManager.Instance != null)
            {
                SpaceInvadersManager.Instance.TriggerGameOver();
            }
        }
    }
}