using UnityEngine;

public class InvaderCollision : MonoBehaviour
{
    private InvaderGridManager gridManager;
    private bool hasBreached = false; // NEW: Safeguards breach actions from duplicate loop calls
    
    [Header("Invasion Settings")]
    public float deathLineZ = -3.5f; 

    [Header("Scoring")]
    public int pointValue = 10;

    [Header("VFX Feedback")]
    public ParticleSystem explosionParticles;

    [Header("Audio Feedback")]
    public AudioClip explosionSound;

    private void Start()
    {
        gridManager = GetComponentInParent<InvaderGridManager>(); 
    }

    private void Update()
    {
        if (transform.position.z <= deathLineZ && !hasBreached)
        {
            hasBreached = true; // block subsequent frames

            if (SpaceInvadersManager.Instance != null)
            {
                SpaceInvadersManager.Instance.TriggerGameOver();
            }
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player")) 
        {
            //safe Arcade Explosion handling
            if (explosionParticles != null)
            {
                explosionParticles.transform.SetParent(null); // safely isolate from parent before destruction
                explosionParticles.Play();
                Destroy(explosionParticles.gameObject, explosionParticles.main.duration);
            }

            if (explosionSound != null)
            {
                // play audio out in world space so destruction doesn't clip audio mid-play
                AudioSource.PlayClipAtPoint(explosionSound, transform.position);
            }

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