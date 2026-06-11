using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EndlessPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float laneDistance = 3f; // distance between the Left, Center, and Right lanes
    public float laneChangeSpeed = 15f; // how snappy the snap to the next lane is
    public float jumpForce = 8f;
    public float gravity = 20f;
    
    [Header("Slide Settings")]
    public float slideDuration = 1.0f;
    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding = false;

    private CharacterController controller;
    private int currentLane = 1; // 0 = Left, 1 = Middle, 2 = Right
    private float verticalVelocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Cache original hitbox sizes so we can restore them after sliding
        originalHeight = controller.height;
        originalCenter = controller.center;
    }

    private void Update()
    {
        // Stop processing movement if the game is over or paused
        if (EndlessRunnerManager.Instance != null && 
           (EndlessRunnerManager.Instance.isGameOver || EndlessRunnerManager.Instance.isPaused))
            return;

        HandleLaneInputs();
        
        // Calculate the target X position based on the current lane
        Vector3 targetPosition = transform.position;
        targetPosition.x = (currentLane - 1) * laneDistance;

        Vector3 moveVector = Vector3.zero;

        // X axis - Smoothly interpolate toward the target lane
        moveVector.x = (targetPosition.x - transform.position.x) * laneChangeSpeed;

        // Y axis - Gravity, Jumping, and Sliding
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; // small downward force to ensure the controller registers as grounded
            
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
            // Apply gravity over time
            verticalVelocity -= gravity * Time.deltaTime;
            
            // Arcade Mechanic: Pressing down mid-air instantly slams player to the ground
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                verticalVelocity = -jumpForce;
            }
        }

        moveVector.y = verticalVelocity;
        
        // apply movement. Z is 0 because the world moves toward the player, the player stays at Z=0.
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
        if (isSliding) return; // Prevent jumping while actively sliding under a barrier
        verticalVelocity = jumpForce;
    }

    private void Slide()
    {
        if (!isSliding)
        {
            StartCoroutine(SlideRoutine());
        }
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        
        // shrink the CharacterController height in half and lower its center
        controller.height = originalHeight / 2f;
        controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

        // anim.SetTrigger("Slide");

        yield return new WaitForSeconds(slideDuration);

        // restore the original hitbox
        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;
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
                EndlessRunnerManager.Instance.AddDistance(10); // reward bonus points/distance for coins
            }
            Destroy(other.gameObject);
        }
    }
}