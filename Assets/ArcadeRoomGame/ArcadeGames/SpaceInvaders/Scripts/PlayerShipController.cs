using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerShipController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float xMin = -8f; 
    public float xMax = 8f;

    [Header("Combat (Particle System)")]
    [Tooltip("The Particle System that acts as your laser cannons.")]
    public ParticleSystem laserParticles;
    public float fireRate = 0.5f; 
    private float nextFireTime = 0f;

    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip shootSound;

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
        // ensure it is an enemy laser and not our own laser somehow hitting us
        if (other.CompareTag("EnemyLaser"))
        {
            Debug.Log("PLAYER HIT BY ENEMY LASER!");
            
        }
    }
}