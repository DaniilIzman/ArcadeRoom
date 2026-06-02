using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovementController : MonoBehaviour
{

    #region Inspector

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float friction = 15f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float airDrag = 0.1f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private bool enableJump = true;

    #endregion

    #region Private References

    private Rigidbody playerRigidbody;
    private CapsuleCollider playerCollider;

    #endregion

    #region Private State

    private Vector2 inputMovement = Vector2.zero;
    private bool isSprinting = false;
    private bool isCrouching = false;
    private bool isGrounded = false;
    private bool isMoving = false;

    private Vector3 velocitySmoothed = Vector3.zero;
    private float currentHeight;
    private float currentCenterY;

    #endregion

    #region Properties

    public bool IsMoving => isMoving;
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;

    #endregion

    #region Unity Messages

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

        playerRigidbody.freezeRotation = true;
        playerRigidbody.constraints = RigidbodyConstraints.FreezeRotationX |
                                      RigidbodyConstraints.FreezeRotationY |
                                      RigidbodyConstraints.FreezeRotationZ;

        currentHeight = playerCollider.height;
        currentCenterY = playerCollider.center.y;
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += OnInputActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnInputActionChange;
    }

    private void Update()
    {
        HandleInput();
        UpdateCrouch();
        DetectGround();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        // wasd movement
        Vector2 moveInput = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
        inputMovement = new Vector2(moveInput.x, moveInput.y);

        // sprint
        isSprinting = InputSystem.actions.FindAction("Sprint").IsPressed();

        // crouch toggle
        if (InputSystem.actions.FindAction("Crouch").WasPressedThisFrame())
        {
            isCrouching = !isCrouching;
        }

        // jump
        if (enableJump && isGrounded && InputSystem.actions.FindAction("Jump").WasPressedThisFrame())
        {
            Jump();
        }
    }

    private void OnInputActionChange(object action, InputActionChange change)
    {
        // callback for input system
    }

    #endregion

    #region Movement

    private void HandleMovement()
    {
        // calculate desired velocity based on input
        Vector3 moveDirection = transform.right * inputMovement.x + transform.forward * inputMovement.y;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float currentSpeed = GetCurrentSpeed();
        Vector3 desiredVelocity = moveDirection * currentSpeed;

        // maintain Y velocity gravity
        desiredVelocity.y = playerRigidbody.linearVelocity.y;

        // smooth velocity change
        velocitySmoothed = Vector3.Lerp(
            velocitySmoothed,
            desiredVelocity,
            Time.fixedDeltaTime * acceleration
        );

        // drag
        float dragCoefficient = isGrounded ? groundDrag : airDrag;
        velocitySmoothed = Vector3.Lerp(
            velocitySmoothed,
            new Vector3(0f, velocitySmoothed.y, 0f),
            Time.fixedDeltaTime * dragCoefficient
        );

        playerRigidbody.linearVelocity = velocitySmoothed;

        isMoving = moveDirection.sqrMagnitude > 0.001f;
    }

    private float GetCurrentSpeed()
    {
        if (isCrouching)
            return crouchSpeed;
        
        return isSprinting ? sprintSpeed : walkSpeed;
    }

    #endregion

    #region Crouching

    private void UpdateCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float targetCenterY = isCrouching ? (standingHeight * 0.25f) : (standingHeight * 0.5f);

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        currentCenterY = Mathf.Lerp(currentCenterY, targetCenterY, Time.deltaTime * crouchTransitionSpeed);

        playerCollider.height = currentHeight;
        playerCollider.center = new Vector3(0f, currentCenterY, 0f);
    }

    #endregion

    #region Ground Detection

    private void DetectGround()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * (playerCollider.bounds.min.y + 0.1f);
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundLayer);

        Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    #endregion

    #region Jump

    private void Jump()
    {
        Vector3 jumpVelocity = playerRigidbody.linearVelocity;
        jumpVelocity.y = jumpForce;
        playerRigidbody.linearVelocity = jumpVelocity;
    }

    #endregion
}