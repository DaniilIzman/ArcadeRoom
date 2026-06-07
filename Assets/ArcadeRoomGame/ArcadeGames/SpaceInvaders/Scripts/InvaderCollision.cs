using UnityEngine;

public class InvaderCollision : MonoBehaviour
{
    private InvaderGridManager gridManager;
    
    [Header("Invasion Settings")]
    public float deathLineZ = -3.5f; 

    [Header("Scoring")]
    [Tooltip("How many points the player gets for shooting this specific alien type.")]
    public int pointValue = 10;

    private void Start()
    {
        gridManager = GetComponentInParent<InvaderGridManager>(); 
    }

    private void Update()
    {
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
            // give the player points
            if (SpaceInvadersManager.Instance != null)
            {
                SpaceInvadersManager.Instance.AddScore(pointValue);
            }

            if (gridManager != null) gridManager.OnInvaderDestroyed(gameObject);
            else Destroy(gameObject);
        }
    }

    public bool IsFrontRowClear()
    {
        Ray ray = new Ray(transform.position, Vector3.back); 
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