using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EndlessPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float laneDistance = 3f; 
    public float laneChangeSpeed = 15f; 
    public float baseJumpForce = 8f;
    public float gravity = 20f;

    [Header("Slide Settings")]
    public float slideDuration = 1.0f;
    public float slideCooldown = 0.3f; 
    [Tooltip("How long the player must wait to slide after landing from a jump")]
    public float jumpLandingSlideDelay = 0.2f;
    private bool isSliding = false;
    private float originalHeight;
    private Vector3 originalCenter;
    private float nextSlideTime = 0f;  
    private bool wasGrounded = true;

    [Header("Pickup Settings")]
    public float jumpBoostMultiplier = 1.5f;
    public float jumpBoostDuration = 5f;
    private float currentJumpForce;
    private Coroutine jumpBootsRoutine;

    [Header("Animation Settings")]
    public Animator anim;

    [Header("Effects & Visuals")]
    [Tooltip("Drag the child object containing your character's 3D mesh here")]
    public GameObject playerModel; 
    public GameObject crashParticlePrefab; 

    [Header("Audio Settings")]
    public AudioSource sfxAudioSource;      // For Jump, Slide, Crash, and Powerup one-shots
    public AudioSource footstepAudioSource; // Dedicated looping source for running
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip runSound;              // Looping footstep clip
    public AudioClip powerUpSound;

    private CharacterController controller;
    private int currentLane = 1; 
    private float verticalVelocity;
    private bool isDead = false; 

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        originalCenter = controller.center;
        currentJumpForce = baseJumpForce;
        wasGrounded = controller.isGrounded;

        // Initialize looping footsteps
        if (footstepAudioSource != null && runSound != null)
        {
            footstepAudioSource.clip = runSound;
            footstepAudioSource.loop = true;
        }
    }

    private void Update()
    {
        // Handle Footstep Audio regardless of state (it safely stops itself if dead/paused)
        ManageFootstepAudio();

        // Stop all processing if dead or game is over/paused
        if (isDead || (EndlessRunnerManager.Instance != null && 
           (EndlessRunnerManager.Instance.isGameOver || EndlessRunnerManager.Instance.isPaused)))
            return;

        // If NOT grounded last frame, but player is grounded , just landed.
        if (!wasGrounded && controller.isGrounded)
        {
            // Push the next allowed slide time forward.
            // Mathf.Max so player don't accidentally shorten an existing slide cooldown.
            nextSlideTime = Mathf.Max(nextSlideTime, Time.time + jumpLandingSlideDelay);
        }
        wasGrounded = controller.isGrounded;
        // ------------------------------

        HandleLaneInputs();
        
        if (anim != null && playerModel.activeSelf)
        {
            anim.SetBool("isGrounded", controller.isGrounded);
            anim.SetFloat("verticalVelocity", verticalVelocity);

            if (TrackManager.Instance != null)
            {
                anim.SetFloat("runSpeedMultiplier", TrackManager.Instance.GetAnimationSpeedMultiplier());
            }
        }

        Vector3 targetPosition = transform.position;
        targetPosition.x = (currentLane - 1) * laneDistance;

        Vector3 moveVector = Vector3.zero;
        moveVector.x = (targetPosition.x - transform.position.x) * laneChangeSpeed;

        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; 
            
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
            verticalVelocity -= gravity * Time.deltaTime;
        }

        moveVector.y = verticalVelocity;
        controller.Move(moveVector * Time.deltaTime);
    }

    private void ManageFootstepAudio()
    {
        if (footstepAudioSource == null) return;

        // Safely check if grounded ONLY if the controller is actually enabled to prevent errors
        bool isActuallyGrounded = controller.enabled && controller.isGrounded;

        // Determine if footsteps should be playing
        bool shouldPlayFootsteps = isActuallyGrounded && !isSliding && !isDead;

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
            
            // Sync footstep pitch with the game's running speed
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
        if (isSliding) StopSlide();
        verticalVelocity = currentJumpForce;

        if (anim != null) anim.Play("Jump", -1, 0f);
        if (sfxAudioSource != null && jumpSound != null) sfxAudioSource.PlayOneShot(jumpSound);
    }

    private void Slide()
    {
        // reject input if already sliding OR if the cooldown/landing delay hasn't finished
        if (isSliding || Time.time < nextSlideTime) return;
        
        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        if (anim != null) anim.SetTrigger("Slide");
        if (sfxAudioSource != null && slideSound != null) sfxAudioSource.PlayOneShot(slideSound);

        controller.height = originalHeight / 2f;
        controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

        yield return new WaitForSeconds(slideDuration);
        StopSlide();
    }

    private void StopSlide()
    {
        StopAllCoroutines(); 
        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;

        nextSlideTime = Time.time + slideCooldown;
    }

    private IEnumerator JumpBootsSequence()
    {
        currentJumpForce = baseJumpForce * jumpBoostMultiplier;
        yield return new WaitForSeconds(jumpBoostDuration);
        currentJumpForce = baseJumpForce;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return; 

        if (other.CompareTag("Obstacle"))
        {
            isDead = true;

            // IMMEDIATELY stop the footsteps so they don't hang or error out
            if (footstepAudioSource != null) footstepAudioSource.Stop(); 

            if (crashParticlePrefab != null)
            {
                // Store the instantiated object in a variable
                GameObject crashEffect = Instantiate(crashParticlePrefab, transform.position, Quaternion.identity);
                
                // Find the ParticleSystem component and force it to play
                ParticleSystem ps = crashEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
            }

            if (playerModel != null)
            {
                playerModel.SetActive(false);
            }
            controller.enabled = false;

            if (EndlessRunnerManager.Instance != null)
                EndlessRunnerManager.Instance.PlayerCrashed();
        }
        else if (other.CompareTag("Coin"))
        {
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

            if (TrackManager.Instance != null)
            {
                TrackManager.Instance.ApplySpeedBoost(1.5f, boostDuration); 
            }
            if (EndlessRunnerManager.Instance != null)
            {
                EndlessRunnerManager.Instance.ActivateSpeedBoostUI(boostDuration);
            }
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("JumpBoots"))
        {
            if (jumpBootsRoutine != null) StopCoroutine(jumpBootsRoutine); 
            jumpBootsRoutine = StartCoroutine(JumpBootsSequence());
            
            if (sfxAudioSource != null && powerUpSound != null) sfxAudioSource.PlayOneShot(powerUpSound);

            if (EndlessRunnerManager.Instance != null)
            {
                EndlessRunnerManager.Instance.ActivateJumpBoostUI(jumpBoostDuration);
            }
            Destroy(other.gameObject);
        }
    }
}