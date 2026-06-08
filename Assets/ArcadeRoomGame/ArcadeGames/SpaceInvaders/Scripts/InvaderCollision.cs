using UnityEngine;

[RequireComponent(typeof(AudioSource))] // Ensures the alien has an AudioSource attached
public class InvaderCollision : MonoBehaviour
{
    private InvaderGridManager gridManager;
    private AudioSource sfxAudioSource;
    private bool hasBreached = false; // safeguards breach actions from duplicate loop calls
    
    [Header("Invasion Settings")]
    public float deathLineZ = -3.5f; 

    [Header("Scoring")]
    public int pointValue = 10;

    [Header("VFX Feedback")]
    public ParticleSystem explosionParticles;

    [Header("Audio Feedback")]
    public AudioClip explosionSound;

    // protect against string tag typos
    private const string playerTag = "Player";

    private void Start()
    {
        // locate the parent grid manager to report status back up the chain
        gridManager = GetComponentInParent<InvaderGridManager>(); 
        
        // FIX 1: Initialize the audio source 
        sfxAudioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // constantly evaluate if this specific alien has crossed the lethal baseline
        if (transform.position.z <= deathLineZ && !hasBreached)
        {
            hasBreached = true; // block subsequent frames

            // notify the core loop to terminate the game session
            if (SpaceInvadersManager.Instance != null)
            {
                SpaceInvadersManager.Instance.TriggerGameOver();
            }
        }
    }
    
    public void PlayEnemyExplosionSound(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }
    
    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag)) 
        {
            // safe arcade explosion handling
            if (explosionParticles != null)
            {
                explosionParticles.transform.SetParent(null); // safely isolate from parent before destruction
                explosionParticles.Play();
                Destroy(explosionParticles.gameObject, explosionParticles.main.duration);
            }

            // --- THE FIX ---
            // PlayClipAtPoint creates a temporary audio object that survives the alien's destruction
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
            }

            // award points to the global economy and score tracker
            if (SpaceInvadersManager.Instance != null)
            {
                SpaceInvadersManager.Instance.AddScore(pointValue);
            }

            // notify the grid array that this alien is wiped, or immediately destroy if rogue
            if (gridManager != null)
            {
                gridManager.OnInvaderDestroyed(gameObject); // Object is destroyed here!
            }
            else 
            {
                Destroy(gameObject); // Or here!
            }
        }
    }

    public bool IsFrontRowClear()
    {
        // cast a ray straight down to see if any friendly aliens are blocking the shot
        Ray ray = new Ray(transform.position, Vector3.back); 

        // utilizing inline out variable declaration for cleaner memory handling
        if (Physics.Raycast(ray, out RaycastHit hit, 25f))
        {
            if (hit.collider.GetComponent<InvaderCollision>() != null) return false; 
        }
        
        return true; 
    }

    public void FireLaser()
    {
        // delegate actual projectile spawning to the master grid system for batching efficiency
        if (gridManager != null)
        {
            gridManager.FireEnemyLaserParticle(transform.position);
        }
    }
}