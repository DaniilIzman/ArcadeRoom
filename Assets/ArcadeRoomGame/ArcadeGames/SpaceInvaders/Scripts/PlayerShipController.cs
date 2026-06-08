using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerShipController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float xMin = -8f; 
    public float xMax = 8f;

    [Header("Combat (Particle System)")]
    public ParticleSystem laserParticles;
    public float fireRate = 0.5f; 
    private float nextFireTime = 0f;

    [Header("VFX Feedback")]
    public ParticleSystem explosionParticles;

    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip hitSound; // sound when the ship gets struck

    // protect against string tag and input typos
    private const string enemyLaserTag = "EnemyLaser";
    private const string horizontalAxis = "Horizontal";
    private const string jumpButton = "Jump";

    private Rigidbody rb;
    private Vector3 movement;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // configure physics boundaries and locks for 2d-style movement
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | 
                         RigidbodyConstraints.FreezePositionZ | 
                         RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        // capture raw horizontal axis inputs
        float moveX = Input.GetAxisRaw(horizontalAxis);
        movement = new Vector3(moveX, 0f, 0f);

        // check for instant jump tap or sustained spacebar hold
        if (Input.GetButtonDown(jumpButton) || Input.GetKey(KeyCode.Space))
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
            }
        }
    }

    private void FixedUpdate()
    {
        // calculate target movement position independently of frame rate
        Vector3 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        
        // guarantee player ship stays within explicit screen boundaries
        targetPosition.x = Mathf.Clamp(targetPosition.x, xMin, xMax);
        rb.MovePosition(targetPosition);
    }

    private void Shoot()
    {
        // step forward the weapon cooldown timestamp
        nextFireTime = Time.time + fireRate;

        // emit a single projectile graphic from particle engine
        if (laserParticles != null)
        {
            laserParticles.Emit(1);
        }

        // play firing sound layer
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        // look for collisions specifically tagged as hostile lasers
        if (other.CompareTag(enemyLaserTag))
        {
            Debug.Log("PLAYER HIT BY ENEMY LASER!");

            // trigger localized visual explosion feedback
            if (explosionParticles != null)
            {
                explosionParticles.Play();
            }
            
            // execute impact sound feedback
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            // alert core gameplay loop manager to decrement player state
            if (SpaceInvadersManager.Instance != null)
            {
                SpaceInvadersManager.Instance.LoseLife();
            }
        }
    }
}