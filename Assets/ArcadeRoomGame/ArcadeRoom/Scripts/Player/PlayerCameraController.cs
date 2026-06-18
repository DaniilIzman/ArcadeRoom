using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    // mouse sensitivity read from the settings slider; recommended range is 0.5 to 10
    [Header("Look Settings")]
    [Tooltip("Adjust this in your UI Canvas Settings Slider. A good range is 0.5 to 10.")]
    public float mouseSensitivity = 2.0f;

    // reference to the player body transform so horizontal mouse input rotates the whole character
    public Transform playerBody;

    // accumulated vertical rotation used to clamp and apply pitch
    private float xRotation = 0f;

    // individual flags that each freeze camera input from different systems
    [HideInInspector] public bool isPausedByMenu = false;
    [HideInInspector] public bool isFrozenByArcade = false;
    [HideInInspector] public bool isShopping = false;

    // static flags used to carry the camera pitch across scene loads
    public static bool restorePitch = false;
    public static float savedPitch = 0f;

    // resets static state when domain reload is skipped between play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        restorePitch = false;
        savedPitch = 0f;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // snap to the saved pitch if returning from an arcade machine scene
        if (restorePitch)
        {
            SnapPitch(savedPitch);
            restorePitch = false;
        }
    }

    private void Update()
    {
        // skip all look input when the camera is frozen by any system
        if (isPausedByMenu || isFrozenByArcade || isShopping) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // subtract mouseY to invert so moving the mouse up tilts the camera upward
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // apply vertical pitch to this transform and horizontal yaw to the player body
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    // returns the current vertical pitch so it can be saved before a scene transition
    public float GetCurrentPitch()
    {
        return xRotation;
    }

    // immediately sets the pitch to a specific angle without any interpolation
    public void SnapPitch(float targetPitch)
    {
        xRotation = targetPitch;
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}