using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Look Settings")]
    [Tooltip("Adjust this in your UI Canvas Settings Slider. A good range is 0.5 to 10.")]
    public float mouseSensitivity = 2.0f;
    public Transform playerBody; 

    private float xRotation = 0f;

    // state separation
    [HideInInspector] public bool isPausedByMenu = false;
    [HideInInspector] public bool isFrozenByArcade = false;

    public static bool restorePitch = false;
    public static float savedPitch = 0f;

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

        if (restorePitch)
        {
            SnapPitch(savedPitch);
            restorePitch = false; 
        }
    }

    private void Update()
    {
        // dual state check
        if (isPausedByMenu || isFrozenByArcade) return; 

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public float GetCurrentPitch() 
    {
        return xRotation;
    }

    public void SnapPitch(float targetPitch)
    {
        xRotation = targetPitch;
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}