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
    private bool isSliding = false;
    private float originalHeight;
    private Vector3 originalCenter;

    [Header("Pickup Settings")]
    public float jumpBoostMultiplier = 1.5f;
    public float jumpBoostDuration = 5f;
    private float currentJumpForce;
    private Coroutine jumpBootsRoutine;

    [Header("Animation Settings")]
    public Animator anim;

    private CharacterController controller;
    private int currentLane = 1; 
    private float verticalVelocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        originalCenter = controller.center;
        currentJumpForce = baseJumpForce;
    }

    private void Update()
    {
        if (EndlessRunnerManager.Instance != null && 
           (EndlessRunnerManager.Instance.isGameOver || EndlessRunnerManager.Instance.isPaused))
            return;

        HandleLaneInputs();
        
        if (anim != null)
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
    }

    private void Slide()
    {
        if (isSliding) return;
        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        if (anim != null) anim.SetTrigger("Slide");

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
    }

    private IEnumerator JumpBootsSequence()
    {
        currentJumpForce = baseJumpForce * jumpBoostMultiplier;
        yield return new WaitForSeconds(jumpBoostDuration);
        currentJumpForce = baseJumpForce;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
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
            if (TrackManager.Instance != null)
            {
                TrackManager.Instance.ApplySpeedBoost(1.5f, 4f); 
            }
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("JumpBoots"))
        {
            if (jumpBootsRoutine != null) StopCoroutine(jumpBootsRoutine); 
            jumpBootsRoutine = StartCoroutine(JumpBootsSequence());
            Destroy(other.gameObject);
        }
    }
}