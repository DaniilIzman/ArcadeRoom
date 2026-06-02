using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 200f;
    public Transform playerBody; // Player GameObject

    private float xRotation = 0f;

    private void Start()
    {
        // lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // get mouse Input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // calculate up/down rotation (clamped so the player does not break neck)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // apply Camera Rotation (up/down)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // apply player rotation (left/right)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}