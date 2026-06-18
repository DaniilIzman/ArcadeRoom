using UnityEngine;

public class InvaderCollision : MonoBehaviour
{
    // prevents game over from being triggered multiple times if several aliens cross the line in one frame
    private InvaderGridManager gridManager;
    private bool hasBreached = false;

    // z position at which this invader is considered to have breached the player's line
    [Header("Invasion Settings")]
    public float deathLineZ = -3.5f;

    // credits and score awarded when this invader is destroyed
    [Header("Scoring")]
    public int pointValue = 10;

    // particle effect detached and played at the invader's position on death
    [Header("VFX Feedback")]
    public ParticleSystem explosionParticles;

    // clip sent to the persistent manager's audiosource so it survives object destruction
    [Header("Audio Feedback")]
    public AudioClip explosionSound;

    // cached tag string to guard against typos
    private const string playerTag = "Player";

    private void Start()
    {
        // walk up the hierarchy to find the grid manager this invader belongs to
        gridManager = GetComponentInParent<InvaderGridManager>();
    }

    private void Update()
    {
        // check each frame whether this invader has crossed the death line
        if (transform.position.z <= deathLineZ && !hasBreached)
        {
            hasBreached = true;
            if (SpaceInvadersManager.Instance != null)
                SpaceInvadersManager.Instance.TriggerGameOver();
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        // only respond to particles that originate from or belong to the player
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            // detach the explosion particle system before destroying the invader so it can finish playing
            if (explosionParticles != null)
            {
                explosionParticles.transform.SetParent(null);
                explosionParticles.Play();
                Destroy(explosionParticles.gameObject, explosionParticles.main.duration);
            }

            // delegate sound playback to the manager so it survives this object's destruction
            // and respects the sfx mixer slider
            if (explosionSound != null && SpaceInvadersManager.Instance != null)
                SpaceInvadersManager.Instance.PlayEnemyExplosionSound(explosionSound);

            // award points through the manager so the score and economy stay in sync
            if (SpaceInvadersManager.Instance != null)
                SpaceInvadersManager.Instance.AddScore(pointValue);

            // remove from the grid's active list, or destroy directly if not tracked
            if (gridManager != null)
                gridManager.OnInvaderDestroyed(gameObject);
            else
                Destroy(gameObject);
        }
    }

    // casts a ray toward the player's line to check whether any friendly invader is blocking this one
    public bool IsFrontRowClear()
    {
        Ray ray = new Ray(transform.position, Vector3.back);

        if (Physics.Raycast(ray, out RaycastHit hit, 25f))
        {
            // if the ray hits another invader, this one is not in the front row
            if (hit.collider.GetComponent<InvaderCollision>() != null) return false;
        }

        return true;
    }

    // delegates laser firing to the grid manager which handles all particle emission centrally
    public void FireLaser()
    {
        if (gridManager != null)
            gridManager.FireEnemyLaserParticle(transform.position);
    }
}