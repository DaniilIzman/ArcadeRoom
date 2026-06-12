using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EndlessPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float laneDistance = 3f; 
    public float laneChangeSpeed = 15f; 
    public float jumpForce = 8f;
    public float gravity = 20f;

    [Header("Slide Settings")]
    public float slideDuration = 1.0f;
    private bool isSliding = false;
    private float originalHeight;
    private Vector3 originalCenter;

    [Header("Animation Settings")]
    public Animator anim;

    private CharacterController controller;
    private int currentLane = 1; 
    private float verticalVelocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // save the original collider dimensions so we can restore them after sliding
        originalHeight = controller.height;
        originalCenter = controller.center;
    }

    private void Update()
    {
        if (EndlessRunnerManager.Instance != null && 
           (EndlessRunnerManager.Instance.isGameOver || EndlessRunnerManager.Instance.isPaused))
            return;

        HandleLaneInputs();
        
        // send physics data to the animator parameters every frame
        if (anim != null)
        {
            anim.SetBool("isGrounded", controller.isGrounded);
            anim.SetFloat("verticalVelocity", verticalVelocity); // Tracks up/down speed
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
        // cancel the slide immediately if the player decides to jump mid-slide
        if (isSliding) StopSlide();

        verticalVelocity = jumpForce;

        if (anim != null)
        {
            // forces the animation to snap back to frame 0, allowing rapid consecutive jumps
            anim.Play("Jump", -1, 0f);
        }
    }

    private void Slide()
    {
        if (isSliding) return;
        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        
        if (anim != null)
        {
            anim.SetTrigger("Slide");
        }

        // shrink the character collider to half its height and lower its center
        controller.height = originalHeight / 2f;
        controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

        // wait for the slide to finish
        yield return new WaitForSeconds(slideDuration);

        // put the collider back to normal
        StopSlide();
    }

    private void StopSlide()
    {
        StopAllCoroutines(); 
        
        // restore collider to full standing height
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
                EndlessRunnerManager.Instance.AddDistance(10); 
            }
            Destroy(other.gameObject);
        }
    }
}