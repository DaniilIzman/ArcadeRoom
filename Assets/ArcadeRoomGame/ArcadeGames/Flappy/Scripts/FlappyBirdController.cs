using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class BirdControls : MonoBehaviour
{
    [Header("Jumping Settings")]
    public float jumpForce = 6f;

    [Header("Velocity Limits")]
    public float maxUpwardVelocity = 8f;
    public float maxDownwardVelocity = -10f;

    [Header("Rotation Visuals")]
    public float maxUpwardAngle = 30f;    // Snaps to this angle when flapping up
    public float maxDownwardAngle = -75f; // Rotates down to this angle when diving
    public float rotationSmoothness = 7f; // How fast the bird transitions between angles

    [Header("Audio Settings")]
    public AudioClip jumpSound;

    private Rigidbody2D structuralRigidbody;
    private AudioSource audioSource;
    private bool canJump = true;

    private void Awake()
    {
        structuralRigidbody = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        if (canJump)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ExecuteJump();
            }
        }

        LimitVelocity();
        ApplyAestheticRotation(); // Visual rotation calculations happen here
    }

    private void ExecuteJump()
    {
        structuralRigidbody.linearVelocity = new Vector2(structuralRigidbody.linearVelocity.x, jumpForce);
        PlayJumpAudio();
    }

    private void PlayJumpAudio()
    {
        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }

    private void LimitVelocity()
    {
        float currentYVelocity = structuralRigidbody.linearVelocity.y;

        if (currentYVelocity > maxUpwardVelocity)
        {
            structuralRigidbody.linearVelocity = new Vector2(structuralRigidbody.linearVelocity.x, maxUpwardVelocity);
        }
        
        if (currentYVelocity < maxDownwardVelocity)
        {
            structuralRigidbody.linearVelocity = new Vector2(structuralRigidbody.linearVelocity.x, maxDownwardVelocity);
        }
    }

    private void ApplyAestheticRotation()
    {
        // 1. Find out where our current velocity sits between falling flat-out and moving up flat-out
        float velocityRatio = Mathf.InverseLerp(maxDownwardVelocity, maxUpwardVelocity, structuralRigidbody.linearVelocity.y);

        // 2. Map that ratio directly to our desired target rotation angles
        float targetZAngle = Mathf.Lerp(maxDownwardAngle, maxUpwardAngle, velocityRatio);

        // 3. Smoothly interpolate from our current rotation to that target rotation over time
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZAngle);
        
        transform.rotation = Quaternion.Lerp(currentRotation, targetRotation, Time.deltaTime * rotationSmoothness);
    }

    public void DisableControls()
    {
        canJump = false;
    }
}