using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 200f;
    public Transform playerBody; 

    private float xRotation = 0f;
    [HideInInspector] public bool isFrozen = false;

    // static memory fields
    public static bool restorePitch = false;
    public static float savedPitch = 0f;

    // clears static memory automatically when a full game restart happens
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
        if (isFrozen) return; 

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

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