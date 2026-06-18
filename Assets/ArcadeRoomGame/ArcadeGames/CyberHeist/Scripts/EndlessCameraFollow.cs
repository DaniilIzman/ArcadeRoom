using UnityEngine;

// follows the player smoothly, with per-axis tracking toggles to suit a side-scrolling runner feel
public class EndlessCameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Drag your Player GameObject here")]
    public Transform player;

    [Tooltip("The base position of the camera relative to the player's center")]
    public Vector3 offset = new Vector3(0f, 4f, -7f);

    [Header("Follow Axes")]
    public bool followX = true; // tracks the player left and right for lane changes
    [Tooltip("Enable to make the camera follow jumps. (Usually kept FALSE in mobile runners to prevent motion sickness)")]
    public bool followY = false; // tracks the player vertically; usually disabled to avoid motion sickness

    [Header("Smoothing")]
    [Tooltip("Lower numbers = looser follow. Higher numbers = tighter snap.")]
    public float smoothSpeed = 10f;

    // lateupdate runs after all character controller movement for the frame has finished,
    // which prevents the camera from lagging one frame behind the player
    private void LateUpdate()
    {
        if (player == null) return;

        // start from the camera's current position so untracked axes stay where they are
        Vector3 targetPosition = transform.position;

        // only update the axes that are toggled on
        if (followX) targetPosition.x = player.position.x + offset.x;
        if (followY) targetPosition.y = player.position.y + offset.y;

        // z is always locked to the offset because the player stays at z = 0
        // while the track scrolls toward them; the camera never moves forward
        targetPosition.z = offset.z;

        // smoothly move toward the target position using linear interpolation
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}