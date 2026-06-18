using UnityEngine;

// controls the player's ship movement, shooting, and hit response using a rigidbody
[RequireComponent(typeof(Rigidbody))]
public class PlayerShipController : MonoBehaviour
{
    // lateral movement speed and the screen boundaries the ship is clamped within
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float xMin = -8f;
    public float xMax = 8f;

    // particle system used to emit a single laser projectile per shot
    [Header("Combat (Particle System)")]
    public ParticleSystem laserParticles;

    // minimum time between shots
    public float fireRate = 0.5f;
    private float nextFireTime = 0f;

    // particle effect played at the ship's position when it is hit
    [Header("VFX Feedback")]
    public ParticleSystem explosionParticles;

    // audiosource and clips for shooting and being hit
    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip hitSound;

    // cached string constants to guard against tag and axis name typos
    private const string enemyLaserTag = "EnemyLaser";
    private const string horizontalAxis = "Horizontal";
    private const string jumpButton = "Jump";

    private Rigidbody rb;
    private Vector3 movement;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // disable gravity and lock all axes except horizontal so the ship moves in 2d only
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                         RigidbodyConstraints.FreezePositionZ |
                         RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        float moveX = Input.GetAxisRaw(horizontalAxis);
        movement = new Vector3(moveX, 0f, 0f);

        // allow firing with either a tap or a held spacebar as long as the cooldown has elapsed
        if (Input.GetButtonDown(jumpButton) || Input.GetKey(KeyCode.Space))
        {
            if (Time.time >= nextFireTime)
                Shoot();
        }
    }

    private void FixedUpdate()
    {
        // use fixedDeltaTime here so movement is framerate-independent and physics-consistent
        Vector3 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;

        // clamp the ship's x position to keep it within the play area boundaries
        targetPosition.x = Mathf.Clamp(targetPosition.x, xMin, xMax);
        rb.MovePosition(targetPosition);
    }

    // advances the fire cooldown, emits a laser particle, and plays the shoot sound
    private void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        if (laserParticles != null)
            laserParticles.Emit(1);

        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }

    private void OnParticleCollision(GameObject other)
    {
        // only react to particles tagged as enemy lasers
        if (other.CompareTag(enemyLaserTag))
        {
            if (explosionParticles != null)
                explosionParticles.Play();

            if (audioSource != null && hitSound != null)
                audioSource.PlayOneShot(hitSound);

            // notify the game manager to decrement the player's life count
            if (SpaceInvadersManager.Instance != null)
                SpaceInvadersManager.Instance.LoseLife();
        }
    }
}