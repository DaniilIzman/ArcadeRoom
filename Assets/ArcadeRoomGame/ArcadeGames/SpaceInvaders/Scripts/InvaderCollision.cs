using UnityEngine;

public class InvaderCollision : MonoBehaviour
{
    private InvaderGridManager gridManager;

    private void Start()
    {
        gridManager = GetComponentInParent<InvaderGridManager>(); 
    }

    private void OnParticleCollision(GameObject other)
    {
        // checks if the laser object OR its parent ship is tagged "Player"
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player")) 
        {
            if (gridManager != null) 
            {
                gridManager.OnInvaderDestroyed(gameObject);
            }
            else 
            {
                // safe destruction if grid manager isn't found
                Destroy(gameObject); 
            }
        }
    }
}