using UnityEngine;
using UnityEngine.SceneManagement;

// requires both a charactercontroller and an audiosource on the same gameobject
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]

public class PlayerMovement : MonoBehaviour
{
    // movement speed values for each locomotion state
    [Header("Movement Speeds")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float crouchSpeed = 2.5f;

    // physics values that control jump arc and falling speed
    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    // capsule heights switched between when standing and crouching
    [Header("Crouching Heights")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;

    // audio clips played for each movement event
    [Header("Audio Clips")]
    public AudioClip jumpSound;
    public AudioClip crouchDownSound;
    public AudioClip standUpSound;
    public AudioClip[] walkFootsteps;
    public AudioClip[] sprintFootsteps;
    public AudioClip[] crouchFootsteps;

    // time between footstep sounds and master volume for all movement audio
    [Header("Audio Timings & Volume")]
    public float walkStepInterval = 0.5f;
    public float sprintStepInterval = 0.3f;
    public float crouchStepInterval = 0.7f;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    private CharacterController controller;
    private AudioSource audioSource;

    // vertical velocity accumulated each frame by gravity and jumping
    private Vector3 velocity;
    private bool isGrounded;

    // counts down to zero to determine when the next footstep sound should play
    private float stepTimer;

    // tracks the previous frame's crouch state to detect transitions
    private bool wasCrouching = false;

    // individual flags that each freeze movement input from different systems
    [HideInInspector] public bool isPausedByMenu = false;
    [HideInInspector] public bool isFrozenByArcade = false;
    [HideInInspector] public bool isShopping = false;

    // publicly readable grounded state used by other scripts such as arcademachine
    public bool IsGrounded => isGrounded;

    // static fields used to carry the player's position and rotation across scene loads
    public static bool restorePosition = false;
    public static Vector3 savedPos;
    public static Quaternion savedRot;

    // recorded at startup for potential debug use
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;

    // resets static state when domain reload is skipped between play sessions
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

        // teleport the player back to their saved position when returning from an arcade scene
        if (restorePosition)
        {
            // disable the controller briefly so the transform can be repositioned without resistance
            controller.enabled = false;
            transform.position = savedPos;
            transform.rotation = savedRot;

            // sync the physics engine to the new transform before re-enabling the controller
            Physics.SyncTransforms();
            controller.enabled = true;
            restorePosition = false;
        }
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;

        // zero out movement when any freeze flag is active
        if (isPausedByMenu || isFrozenByArcade || isShopping)
        {
            controller.Move(Vector3.zero);
            velocity = Vector3.zero;
            return;
        }

        // prevent the player from sinking into the floor when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

        // sprinting is only allowed when moving forward and not crouching
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && z > 0 && !isCrouching;

        // play a sound once when the player transitions into or out of crouch
        if (isCrouching && !wasCrouching && isGrounded)
        {
            if (crouchDownSound != null) audioSource.PlayOneShot(crouchDownSound, footstepVolume);
        }
        else if (!isCrouching && wasCrouching && isGrounded)
        {
            if (standUpSound != null) audioSource.PlayOneShot(standUpSound, footstepVolume);
        }
        wasCrouching = isCrouching;

        // select the appropriate speed for the current movement state
        float currentSpeed = walkSpeed;
        if (isSprinting) currentSpeed = sprintSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // adjust the capsule collider height to match the current stance
        controller.height = isCrouching ? crouchingHeight : standingHeight;

        HandleMovementAudio(x, z, isSprinting, isCrouching);

        // apply jump velocity only when grounded and not crouching
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlayJumpSound();
        }

        // apply gravity every frame and move the controller vertically
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    #region Audio Logic

    // determines which footstep array to use and fires a sound at the correct interval
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
            // reset timer so the next step plays immediately when movement resumes
            stepTimer = 0f;
        }
    }

    // picks a random clip from the provided array and plays it as a one-shot
    private void PlayRandomFootstep(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;

        int randomIndex = Random.Range(0, clips.Length);
        audioSource.PlayOneShot(clips[randomIndex], footstepVolume);
    }

    // plays the jump sound effect if both the clip and audiosource are available
    private void PlayJumpSound()
    {
        if (jumpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpSound, footstepVolume);
        }
    }

    #endregion
}