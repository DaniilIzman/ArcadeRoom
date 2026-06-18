using UnityEngine;
using UnityEngine.Audio;

// requires a rigidbody for physics-based movement and an audiosource for jump and death sounds
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class BirdControls : MonoBehaviour
{
    // upward velocity applied to the rigidbody on each jump input
    [Header("Jumping Settings")]
    public float jumpForce = 6f;

    // clamps the rigidbody's vertical velocity to prevent runaway speed in either direction
    [Header("Velocity Limits")]
    public float maxUpwardVelocity = 8f;
    public float maxDownwardVelocity = -10f;

    // rotation limits and smoothing speed for the tilt visual that follows vertical velocity
    [Header("Rotation Visuals")]
    public float maxUpwardAngle = 30f;
    public float maxDownwardAngle = -75f;
    public float rotationSmoothness = 7f;

    // mixer group assigned here so the bird's sounds are controlled by the sfx slider
    [Header("Audio Settings")]
    public AudioMixerGroup sfxMixerGroup;
    public AudioClip jumpSound;
    public AudioClip deathSound;

    // particles played on jump (small burst) and on death (detached so it survives destruction)
    [Header("Visual Feedback")]
    public ParticleSystem jumpParticle;
    public ParticleSystem deathParticle;

    private Rigidbody structuralRigidbody;
    private AudioSource audioSource;

    // set to false on death to block further input and repeated death calls
    private bool canJump = true;

    private void Awake()
    {
        structuralRigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;

            // route the bird's audiosource through the sfx mixer group if one is assigned
            if (sfxMixerGroup != null)
                audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    private void Update()
    {
        // skip all input and visual updates when the game is over or paused
        if (FlappyGameManager.Instance.isGameOver || FlappyGameManager.Instance.isPaused) return;

        if (canJump && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            ExecuteJump();

        LimitVelocity();
        ApplyAestheticRotation();
    }

    // sets vertical velocity directly, plays the jump sound, and emits a small particle burst
    private void ExecuteJump()
    {
        structuralRigidbody.linearVelocity = new Vector3(structuralRigidbody.linearVelocity.x, jumpForce, 0f);
        PlayJumpAudio();

        if (jumpParticle != null)
            jumpParticle.Emit(10);
    }

    private void PlayJumpAudio()
    {
        if (audioSource != null && jumpSound != null)
            audioSource.PlayOneShot(jumpSound);
    }

    #region Physics Collision Hooks (3D Only)

    // any physical collision kills the bird
    private void OnCollisionEnter(Collision collision)
    {
        ProcessDeath();
    }

    private void OnTriggerEnter(Collider other)
    {
        // score zones are triggers too; ignore them so only hazard triggers cause death
        if (other.CompareTag("ScoreZone")) return;
        ProcessDeath();
    }

    // detaches and plays the death particle, notifies the game manager, then hides and destroys the bird
    private void ProcessDeath()
    {
        // guard prevents this from running more than once if multiple collisions fire the same frame
        if (!canJump) return;

        // detach the death particle so it continues playing after the bird is destroyed
        if (deathParticle != null)
        {
            deathParticle.transform.SetParent(null);
            deathParticle.Play();
            Destroy(deathParticle.gameObject, 2f);
        }

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        DisableControls();
        FlappyGameManager.Instance.GameOver();

        // disable the collider so no further collisions trigger after death
        if (TryGetComponent<Collider>(out Collider col)) col.enabled = false;

        // make kinematic so gravity stops pulling the invisible corpse downward
        structuralRigidbody.isKinematic = true;

        // hide all renderers so the bird disappears without an instant destroy
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = false;

        Destroy(gameObject, 2f);
    }

    #endregion

    // clamps the rigidbody's y velocity within the configured min and max bounds
    private void LimitVelocity()
    {
        float currentYVelocity = structuralRigidbody.linearVelocity.y;
        float clampedY = Mathf.Clamp(currentYVelocity, maxDownwardVelocity, maxUpwardVelocity);
        structuralRigidbody.linearVelocity = new Vector3(structuralRigidbody.linearVelocity.x, clampedY, 0f);
    }

    // tilts the bird to face the direction it is moving based on its current vertical velocity
    private void ApplyAestheticRotation()
    {
        if (!canJump) return;

        // map vertical velocity to a normalised 0-1 value then lerp between the angle limits
        float velocityRatio = Mathf.InverseLerp(maxDownwardVelocity, maxUpwardVelocity, structuralRigidbody.linearVelocity.y);
        float targetZAngle = Mathf.Lerp(maxDownwardAngle, maxUpwardAngle, velocityRatio);

        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZAngle);
        transform.rotation = Quaternion.Lerp(currentRotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    // called externally on death to prevent any further jump or rotation updates
    public void DisableControls()
    {
        canJump = false;
    }
}