using UnityEngine;

public class EndlessCameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Drag your Player GameObject here")]
    public Transform player;
    
    [Tooltip("The base position of the camera relative to the player's center")]
    public Vector3 offset = new Vector3(0f, 4f, -7f);

    [Header("Follow Axes")]
    public bool followX = true; // Tracks lane changes
    [Tooltip("Enable to make the camera follow jumps. (Usually kept FALSE in mobile runners to prevent motion sickness)")]
    public bool followY = false; 

    [Header("Smoothing")]
    [Tooltip("Lower numbers = looser follow. Higher numbers = tighter snap.")]
    public float smoothSpeed = 10f;

    // LateUpdate so the camera moves AFTER the player has finished their CharacterController movement for the frame.
    private void LateUpdate()
    {
        if (player == null) return;

        // start with our current position
        Vector3 targetPosition = transform.position;

        // apply dynamic tracking based on toggles
        if (followX) targetPosition.x = player.position.x + offset.x;
        if (followY) targetPosition.y = player.position.y + offset.y;
        
        // Z is strictly locked to the offset because the player stays at Z=0 while the track moves toward them
        targetPosition.z = offset.z;

        // smoothly interpolate from our current position to the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}