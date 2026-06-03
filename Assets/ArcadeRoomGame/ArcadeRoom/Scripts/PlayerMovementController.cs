using UnityEngine;

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

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // ground check
        isGrounded = controller.isGrounded;
        
        // reset gravity build-up if grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        // read keyboard Input (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // determine current state
        bool isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && z > 0 && !isCrouching;

        // calculate speed
        float currentSpeed = walkSpeed;
        if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }

        // apply movement (X and Z axis)
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // handle crouching height
        if (isCrouching)
        {
            controller.height = crouchingHeight;
        }
        else
        {
            controller.height = standingHeight;
        }

        // handle jumping
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            // physics formula for calculating jump velocity based on desired height
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // apply Gravity (Y axis)
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}