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
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player")) 
        {
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

    // calls the GridManager's highly optimized particle emitter
    public void FireLaser()
    {
        if (gridManager != null)
        {
            gridManager.FireEnemyLaserParticle(transform.position);
        }
    }
}