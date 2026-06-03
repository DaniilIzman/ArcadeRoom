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

    [HideInInspector] public bool isFrozen = false;
    public bool IsGrounded => isGrounded; 

    // static memory fields
    public static bool restorePosition = false;
    public static Vector3 savedPos;
    public static Quaternion savedRot;

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

        if (restorePosition)
        {
            // turn off the controller so it releases its grip on the transform
            controller.enabled = false;
            
            // teleport the player
            transform.position = savedPos;
            transform.rotation = savedRot;
            
            // force Unity's physics engine to instantly register the new coordinates
            // without this, the CharacterController will snap the player back to the default spawn point
            Physics.SyncTransforms(); 
            
            // 4. Turn the controller back on
            controller.enabled = true; 

            restorePosition = false; 
        }
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;
        
        if (isFrozen)
        {
            // keep the controller engaged with a zero-vector 
            // skipping the Move command entirely causes unity to push/snap if the player is touching a wall
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

        float currentSpeed = walkSpeed;
        if (isSprinting) currentSpeed = sprintSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (isCrouching)
        {
            controller.height = crouchingHeight;
        }
        else
        {
            controller.height = standingHeight;
        }

        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}