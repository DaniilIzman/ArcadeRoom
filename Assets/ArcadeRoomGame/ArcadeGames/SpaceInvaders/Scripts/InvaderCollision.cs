using UnityEngine;

public class InvaderCollision : MonoBehaviour
{
    private InvaderGridManager gridManager;
    
    [Header("Invasion Settings")]
    [Tooltip("The exact Z coordinate that causes a Game Over when an alien crosses it.")]
    public float deathLineZ = -3.5f; 

    private void Start()
    {
        gridManager = GetComponentInParent<InvaderGridManager>(); 
    }

    private void Update()
    {
        // 100% Bulletproof math check. No Rigidbodies or Triggers required.
        // (Change .z to .y if your game is built standing up instead of flat on the ground)
        if (transform.position.z <= deathLineZ)
        {
            if (SpaceInvadersManager.Instance != null)
            {
                Debug.Log("🚨 INVASION SUCCESSFUL! BASE OVERRUN!");
                SpaceInvadersManager.Instance.TriggerGameOver();
            }
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player")) 
        {
            if (gridManager != null) gridManager.OnInvaderDestroyed(gameObject);
            else Destroy(gameObject);
        }
    }

    public bool IsFrontRowClear()
    {
        Ray ray = new Ray(transform.position, Vector3.back); // Change to Vector3.down if using Y axis
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 25f))
        {
            if (hit.collider.GetComponent<InvaderCollision>() != null) return false; 
        }
        return true; 
    }

    public void FireLaser()
    {
        if (gridManager != null)
        {
            gridManager.FireEnemyLaserParticle(transform.position);
        }
    }
}