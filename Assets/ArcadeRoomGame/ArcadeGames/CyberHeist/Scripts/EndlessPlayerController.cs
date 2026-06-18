using System.Collections;
using UnityEngine;

// requires a character controller on the same object; handles lane movement, jumping, sliding,
// pickups, powerups, death, and footstep audio for the endless runner player
[RequireComponent(typeof(CharacterController))]
public class EndlessPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float laneDistance = 3f;      // world-space distance between each of the three lanes
    public float laneChangeSpeed = 15f;  // how quickly the player snaps across to a new lane
    public float baseJumpForce = 8f;     // upward velocity applied when the player jumps
    public float gravity = 20f;          // downward acceleration applied while the player is airborne

    [Header("Slide Settings")]
    public float slideDuration = 1.0f;       // how long a slide lasts in seconds
    public float slideCooldown = 0.3f;       // minimum time between slides after one ends
    [Tooltip("How long the player must wait to slide after landing from a jump")]
    public float jumpLandingSlideDelay = 0.2f; // prevents immediately sliding the moment the player touches down
    private bool isSliding = false;            // true while the slide coroutine is running
    private float originalHeight;             // full capsule height stored at start for restoring after a slide
    private Vector3 originalCenter;           // full capsule center stored at start for restoring after a slide
    private float nextSlideTime = 0f;         // earliest time.time at which another slide is allowed
    private bool wasGrounded = true;          // grounded state from the previous frame, used for landing detection

    [Header("Pickup Settings")]
    public float jumpBoostMultiplier = 1.5f; // factor applied to jump force while jump boots are active
    public float jumpBoostDuration = 5f;     // how many seconds the jump boots powerup lasts
    private float currentJumpForce;          // the jump force actually used; changes when boots are active
    private Coroutine jumpBootsRoutine;      // reference kept so the coroutine can be restarted on re-pickup

    [Header("Animation Settings")]
    public Animator anim;

    [Header("Effects & Visuals")]
    [Tooltip("Drag the child object containing your character's 3D mesh here")]
    public GameObject playerModel;
    public GameObject crashParticlePrefab; // spawned at the player's position when they hit an obstacle

    [Header("Audio Settings")]
    public AudioSource sfxAudioSource;      // plays one-shot sounds: jump, slide, crash, powerup
    public AudioSource footstepAudioSource; // dedicated looping source so footsteps can be paused independently
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip runSound;    // looping clip played through footstepAudioSource while running
    public AudioClip powerUpSound;

    private CharacterController controller;
    private int currentLane = 1;   // 0 = left, 1 = centre, 2 = right
    private float verticalVelocity; // tracks the current y velocity so gravity accumulates correctly
    private bool isDead = false;    // set to true on collision with an obstacle; stops most processing

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // store the original capsule dimensions so they can be restored after sliding
        originalHeight = controller.height;
        originalCenter = controller.center;

        currentJumpForce = baseJumpForce;
        wasGrounded = controller.isGrounded;

        // configure the footstep source for looping; playback is controlled in ManageFootstepAudio
        if (footstepAudioSource != null && runSound != null)
        {
            footstepAudioSource.clip = runSound;
            footstepAudioSource.loop = true;
        }
    }

    private void Update()
    {
        // footstep management runs unconditionally so it can cleanly stop in any state
        ManageFootstepAudio();

        // skip all input and physics when dead or the game is paused/over
        if (isDead || (EndlessRunnerManager.Instance != null &&
           (EndlessRunnerManager.Instance.isGameOver || EndlessRunnerManager.Instance.isPaused)))
            return;

        // detect the frame the player lands; push the slide cooldown forward so the player
        // can't immediately slide on touchdown from a jump
        if (!wasGrounded && controller.isGrounded)
        {
            nextSlideTime = Mathf.Max(nextSlideTime, Time.time + jumpLandingSlideDelay);
        }
        wasGrounded = controller.isGrounded;

        HandleLaneInputs();

        // keep the animator in sync with physics and track speed
        if (anim != null && playerModel.activeSelf)
        {
            anim.SetBool("isGrounded", controller.isGrounded);
            anim.SetFloat("verticalVelocity", verticalVelocity);

            if (TrackManager.Instance != null)
            {
                // scale the run animation speed to match how fast the track is currently moving
                anim.SetFloat("runSpeedMultiplier", TrackManager.Instance.GetAnimationSpeedMultiplier());
            }
        }

        // calculate the world-space x position for the target lane
        Vector3 targetPosition = transform.position;
        targetPosition.x = (currentLane - 1) * laneDistance;

        Vector3 moveVector = Vector3.zero;

        // slide x toward the target lane using a speed-scaled delta rather than lerp
        moveVector.x = (targetPosition.x - transform.position.x) * laneChangeSpeed;

        if (controller.isGrounded)
        {
            // keep the player firmly on the ground with a small negative velocity
            // instead of 0 so the grounded check stays reliable
            verticalVelocity = -0.5f;

            // jump and slide are only available while on the ground
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                Slide();
            }
        }
        else
        {
            // accumulate gravity while airborne
            verticalVelocity -= gravity * Time.deltaTime;
        }

        moveVector.y = verticalVelocity;
        controller.Move(moveVector * Time.deltaTime);
    }

    private void ManageFootstepAudio()
    {
        if (footstepAudioSource == null) return;

        // only query isGrounded when the controller is enabled; querying a disabled
        // controller throws an error
        bool isActuallyGrounded = controller.enabled && controller.isGrounded;

        // footsteps should only play when the player is alive, grounded, and not sliding
        bool shouldPlayFootsteps = isActuallyGrounded && !isSliding && !isDead;

        // also suppress during game over or pause even if the above conditions are met
        if (EndlessRunnerManager.Instance != null &&
           (EndlessRunnerManager.Instance.isGameOver || EndlessRunnerManager.Instance.isPaused))
        {
            shouldPlayFootsteps = false;
        }

        if (shouldPlayFootsteps)
        {
            if (!footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Play();
            }

            // match the footstep pitch to the current track speed so they feel in sync
            if (TrackManager.Instance != null)
            {
                footstepAudioSource.pitch = TrackManager.Instance.GetAnimationSpeedMultiplier();
            }
        }
        else
        {
            if (footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Pause();
            }
        }
    }

    // reads left and right input and clamps the lane index to the valid range 0-2
    private void HandleLaneInputs()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (currentLane < 2) currentLane++;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (currentLane > 0) currentLane--;
        }
    }

    private void Jump()
    {
        // cancel an active slide before jumping so the capsule is full size in the air
        if (isSliding) StopSlide();

        verticalVelocity = currentJumpForce;

        // restart the jump animation from the beginning in case it was already playing
        if (anim != null) anim.Play("Jump", -1, 0f);
        if (sfxAudioSource != null && jumpSound != null) sfxAudioSource.PlayOneShot(jumpSound);
    }

    private void Slide()
    {
        // ignore the input if a slide is already active or the cooldown hasn't expired
        if (isSliding || Time.time < nextSlideTime) return;

        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;

        if (anim != null) anim.SetTrigger("Slide");
        if (sfxAudioSource != null && slideSound != null) sfxAudioSource.PlayOneShot(slideSound);

        // halve the capsule height and raise the center so the bottom stays at the same position
        controller.height = originalHeight / 2f;
        controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

        yield return new WaitForSeconds(slideDuration);

        StopSlide();
    }

    private void StopSlide()
    {
        // stop any running coroutines (including the slide timer) before restoring the capsule
        StopAllCoroutines();

        // restore the capsule to its original dimensions
        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;

        // start the cooldown window so the player can't chain slides instantly
        nextSlideTime = Time.time + slideCooldown;
    }

    // temporarily multiplies jump force for the duration of the jump boots powerup
    private IEnumerator JumpBootsSequence()
    {
        currentJumpForce = baseJumpForce * jumpBoostMultiplier;
        yield return new WaitForSeconds(jumpBoostDuration);
        currentJumpForce = baseJumpForce;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ignore all triggers after death to prevent double-processing
        if (isDead) return;

        if (other.CompareTag("Obstacle"))
        {
            isDead = true;

            // stop footsteps immediately so they don't keep playing during the game over screen
            if (footstepAudioSource != null) footstepAudioSource.Stop();

            // spawn and immediately play the crash particle effect at the player's position
            if (crashParticlePrefab != null)
            {
                GameObject crashEffect = Instantiate(crashParticlePrefab, transform.position, Quaternion.identity);

                ParticleSystem ps = crashEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
            }

            // hide the player mesh and disable the controller so no further movement occurs
            if (playerModel != null)
            {
                playerModel.SetActive(false);
            }
            controller.enabled = false;

            // tell the manager to trigger the game over sequence
            if (EndlessRunnerManager.Instance != null)
                EndlessRunnerManager.Instance.PlayerCrashed();
        }
        else if (other.CompareTag("Coin"))
        {
            // add score and play the coin sound, then remove the coin from the scene
            if (EndlessRunnerManager.Instance != null)
            {
                EndlessRunnerManager.Instance.PlayCoinPickupSound();
                EndlessRunnerManager.Instance.AddScore(100);
            }
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("SpeedBoost"))
        {
            float boostDuration = 4f;

            if (sfxAudioSource != null && powerUpSound != null) sfxAudioSource.PlayOneShot(powerUpSound);

            // tell the track manager to move everything faster for the boost duration
            if (TrackManager.Instance != null)
            {
                TrackManager.Instance.ApplySpeedBoost(1.5f, boostDuration);
            }

            // show the on-screen countdown timer for the speed boost
            if (EndlessRunnerManager.Instance != null)
            {
                EndlessRunnerManager.Instance.ActivateSpeedBoostUI(boostDuration);
            }
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("JumpBoots"))
        {
            // stop any existing boots coroutine before restarting it, allowing re-pickup to refresh the timer
            if (jumpBootsRoutine != null) StopCoroutine(jumpBootsRoutine);
            jumpBootsRoutine = StartCoroutine(JumpBootsSequence());

            if (sfxAudioSource != null && powerUpSound != null) sfxAudioSource.PlayOneShot(powerUpSound);

            // show the on-screen countdown timer for the jump boost
            if (EndlessRunnerManager.Instance != null)
            {
                EndlessRunnerManager.Instance.ActivateJumpBoostUI(jumpBoostDuration);
            }
            Destroy(other.gameObject);
        }
    }
}