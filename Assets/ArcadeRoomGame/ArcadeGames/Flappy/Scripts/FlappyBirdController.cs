using UnityEngine;
using UnityEngine.Audio; // Required to communicate with the Audio Mixer

[RequireComponent(typeof(Rigidbody))] 
[RequireComponent(typeof(AudioSource))]
public class BirdControls : MonoBehaviour
{
    [Header("Jumping Settings")]
    public float jumpForce = 6f;

    [Header("Velocity Limits")]
    public float maxUpwardVelocity = 8f;
    public float maxDownwardVelocity = -10f;

    [Header("Rotation Visuals")]
    public float maxUpwardAngle = 30f;    
    public float maxDownwardAngle = -75f; 
    public float rotationSmoothness = 7f; 

    [Header("Audio Settings")]
    public AudioMixerGroup sfxMixerGroup; // The new slot for your SFX Mixer routing
    public AudioClip jumpSound;
    public AudioClip deathSound; 

    [Header("Visual Feedback")]
    public ParticleSystem jumpParticle;   
    public ParticleSystem deathParticle; 

    private Rigidbody structuralRigidbody; 
    private AudioSource audioSource;
    private bool canJump = true;

    private void Awake()
    {
        structuralRigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            
            // Force the Bird's audio to route through the SFX slider automatically
            if (sfxMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = sfxMixerGroup;
            }
        }
    }

    private void Update()
    {
        if (FlappyGameManager.Instance.isGameOver || FlappyGameManager.Instance.isPaused) return;

        if (canJump && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            ExecuteJump();
        }

        LimitVelocity();
        ApplyAestheticRotation();
    }

    private void ExecuteJump()
    {
        structuralRigidbody.linearVelocity = new Vector3(structuralRigidbody.linearVelocity.x, jumpForce, 0f);
        PlayJumpAudio();
        
        if (jumpParticle != null)
        {
            jumpParticle.Emit(10); // Hardcoded micro-burst count
        }
    }

    private void PlayJumpAudio()
    {
        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }

    #region Physics Collision Hooks (3D Only)

    private void OnCollisionEnter(Collision collision)
    {
        ProcessDeath();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ScoreZone")) return; 
        ProcessDeath();
    }

    private void ProcessDeath()
    {
        if (!canJump) return; 

        // Play the explosion particle safely
        if (deathParticle != null)
        {
            deathParticle.transform.SetParent(null); // Detach so it doesn't hide or move with the dead bird
            deathParticle.Play();
            Destroy(deathParticle.gameObject, 2f);   // Clean it up from the scene hierarchy after it finishes
        }

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        DisableControls();
        FlappyGameManager.Instance.GameOver();

        if (TryGetComponent<Collider>(out Collider col)) col.enabled = false;
        structuralRigidbody.isKinematic = true; 

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        Destroy(gameObject, 2f);
    }

    #endregion

    private void LimitVelocity()
    {
        float currentYVelocity = structuralRigidbody.linearVelocity.y;
        float clampedY = Mathf.Clamp(currentYVelocity, maxDownwardVelocity, maxUpwardVelocity);
        
        structuralRigidbody.linearVelocity = new Vector3(structuralRigidbody.linearVelocity.x, clampedY, 0f);
    }

    private void ApplyAestheticRotation()
    {
        if (!canJump) return; 

        float velocityRatio = Mathf.InverseLerp(maxDownwardVelocity, maxUpwardVelocity, structuralRigidbody.linearVelocity.y);
        float targetZAngle = Mathf.Lerp(maxDownwardAngle, maxUpwardAngle, velocityRatio);

        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZAngle);
        
        transform.rotation = Quaternion.Lerp(currentRotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    public void DisableControls()
    {
        canJump = false;
    }
}