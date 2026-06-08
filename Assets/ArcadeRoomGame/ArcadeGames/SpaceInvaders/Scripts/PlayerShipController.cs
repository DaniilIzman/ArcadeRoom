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
    public ParticleSystem explosionParticles; // assign the player's custom child explosion system

    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip hitSound; // sound when the ship gets struck

    private Rigidbody rb;
    private Vector3 movement;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | 
                         RigidbodyConstraints.FreezePositionZ | 
                         RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        movement = new Vector3(moveX, 0f, 0f);

        if (Input.GetButtonDown("Jump") || Input.GetKey(KeyCode.Space))
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
            }
        }
    }

    private void FixedUpdate()
    {
        Vector3 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        targetPosition.x = Mathf.Clamp(targetPosition.x, xMin, xMax);
        rb.MovePosition(targetPosition);
    }

    private void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        if (laserParticles != null)
        {
            laserParticles.Emit(1);
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("EnemyLaser"))
        {
            Debug.Log("PLAYER HIT BY ENEMY LASER!");

            // fire Player Explosion Elements
            if (explosionParticles != null)
            {
                explosionParticles.Play();
            }
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            if (SpaceInvadersManager.Instance != null)
            {
                SpaceInvadersManager.Instance.LoseLife();
            }
        }
    }
}