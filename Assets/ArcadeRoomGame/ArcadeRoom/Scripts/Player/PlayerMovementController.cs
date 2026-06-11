using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 2.5f;

    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Crouching Heights")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;

    [Header("Audio Clips")]
    public AudioClip jumpSound;
    public AudioClip crouchDownSound;  
    public AudioClip standUpSound;     
    public AudioClip[] walkFootsteps;
    public AudioClip[] sprintFootsteps;
    public AudioClip[] crouchFootsteps;

    [Header("Audio Timings & Volume")]
    public float walkStepInterval = 0.5f;
    public float sprintStepInterval = 0.3f;
    public float crouchStepInterval = 0.7f;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    private CharacterController controller;
    private AudioSource audioSource;
    private Vector3 velocity;
    private bool isGrounded;
    private float stepTimer;
    private bool wasCrouching = false;

    // state separation
    [HideInInspector] public bool isPausedByMenu = false;
    [HideInInspector] public bool isFrozenByArcade = false;
    [HideInInspector] public bool isShopping = false;
    public bool IsGrounded => isGrounded; 

    public static bool restorePosition = false;
    public static Vector3 savedPos;
    public static Quaternion savedRot;

    // debug tracking
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        restorePosition = false;
        savedPos = Vector3.zero;
        savedRot = Quaternion.identity;
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;

        if (restorePosition)
        {
            controller.enabled = false;
            transform.position = savedPos;
            transform.rotation = savedRot;
            Physics.SyncTransforms(); 
            controller.enabled = true; 
            restorePosition = false; 
        }
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;
        
        if (isPausedByMenu || isFrozenByArcade || isShopping)
        {
            controller.Move(Vector3.zero);
            velocity = Vector3.zero;
            return;
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && z > 0 && !isCrouching;

        if (isCrouching && !wasCrouching && isGrounded)
        {
            if (crouchDownSound != null) audioSource.PlayOneShot(crouchDownSound, footstepVolume);
        }
        else if (!isCrouching && wasCrouching && isGrounded)
        {
            if (standUpSound != null) audioSource.PlayOneShot(standUpSound, footstepVolume);
        }
        wasCrouching = isCrouching; 

        float currentSpeed = walkSpeed;
        if (isSprinting) currentSpeed = sprintSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        controller.height = isCrouching ? crouchingHeight : standingHeight;

        HandleMovementAudio(x, z, isSprinting, isCrouching);

        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlayJumpSound();
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    #region Audio Logic

    private void HandleMovementAudio(float x, float z, bool isSprinting, bool isCrouching)
    {
        bool isMoving = (Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f);

        if (isGrounded && isMoving)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                if (isSprinting)
                {
                    PlayRandomFootstep(sprintFootsteps);
                    stepTimer = sprintStepInterval;
                }
                else if (isCrouching)
                {
                    PlayRandomFootstep(crouchFootsteps);
                    stepTimer = crouchStepInterval;
                }
                else
                {
                    PlayRandomFootstep(walkFootsteps);
                    stepTimer = walkStepInterval;
                }
            }
        }
        else
        {
            stepTimer = 0f; 
        }
    }

    private void PlayRandomFootstep(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        
        int randomIndex = Random.Range(0, clips.Length);
        audioSource.PlayOneShot(clips[randomIndex], footstepVolume);
    }

    private void PlayJumpSound()
    {
        if (jumpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpSound, footstepVolume);
        }
    }

    #endregion
}