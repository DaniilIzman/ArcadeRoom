using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{

    #region Inspector

    [Header("Mouse Sensitivity")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookUpAngle = 90f;
    [SerializeField] private float maxLookDownAngle = 90f;

    [Header("Head Bobbing")]
    [Tooltip("Enable head bob movement for realism")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float headBobFrequency = 5f;
    [SerializeField] private float headBobAmplitude = 0.1f;

    [Header("FOV")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float crouchFOV = 55f;

    #endregion

    #region Private References

    private Camera playerCamera;
    private PlayerMovementController movementController;

    #endregion

    #region Private State

    private float rotationX = 0f;
    private float headBobTimer = 0f;
    private Vector3 cameraLocalPositionDefault;

    #endregion

    #region Unity Messages

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        movementController = GetComponentInParent<PlayerMovementController>();

        if (playerCamera == null)
        {
            Debug.LogError("[PlayerCameraController] Camera component not found!");
        }

        cameraLocalPositionDefault = transform.localPosition;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleHeadBob();
        HandleFOV();
        HandleCursorToggle();
    }

    #endregion

    #region Mouse Look

    private void HandleMouseLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        rotationX -= mouseDelta.y * mouseSensitivity;
        rotationX = Mathf.Clamp(rotationX, -maxLookDownAngle, maxLookUpAngle);

        // camera vertical
        transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        // body horizonal
        Transform playerTransform = transform.parent;
        Vector3 playerEuler = playerTransform.eulerAngles;
        playerEuler.y += mouseDelta.x * mouseSensitivity;
        playerTransform.eulerAngles = playerEuler;
    }

    #endregion

    #region Head Bobbing

    private void HandleHeadBob()
    {
        if (!enableHeadBob || movementController == null) return;

        bool isMoving = movementController.IsMoving;
        bool isGrounded = movementController.IsGrounded;

        if (!isMoving || !isGrounded)
        {
            headBobTimer = 0f;
            RestoreCameraPosition();
            return;
        }

        headBobTimer += Time.deltaTime * headBobFrequency;

        float verticalBob = Mathf.Sin(headBobTimer) * headBobAmplitude;
        float horizontalBob = Mathf.Sin(headBobTimer * 0.5f) * (headBobAmplitude * 0.5f);

        Vector3 bobOffset = new Vector3(horizontalBob, verticalBob, 0f);
        transform.localPosition = cameraLocalPositionDefault + bobOffset;
    }

    private void RestoreCameraPosition()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            cameraLocalPositionDefault,
            Time.deltaTime * 5f
        );
    }

    #endregion

    #region FOV Handling

    private void HandleFOV()
    {
        if (movementController == null) return;

        float targetFOV = movementController.IsCrouching ? crouchFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 5f);
    }

    #endregion

    #region Cursor Toggle

    private void HandleCursorToggle()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    #endregion

    #region Public API

    public float GetRotationX() => rotationX;

    #endregion
}