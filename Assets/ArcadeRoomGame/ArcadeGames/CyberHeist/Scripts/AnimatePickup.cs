using UnityEngine;

public class AnimatePickup : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Vector3 rotationSpeed = new Vector3(0f, 100f, 0f); // spins around the Y axis

    [Header("Hover Settings")]
    public float bounceMagnitude = 0.2f;
    public float bounceSpeed = 2f;

    private Vector3 startPosition;

    private void Start()
    {
        // cache the local starting position relative to the moving track piece
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        // spin the pickup
        transform.Rotate(rotationSpeed * Time.deltaTime);

        // make the pickup gently hover up and down using a sine wave
        float newY = startPosition.y + (Mathf.Sin(Time.time * bounceSpeed) * bounceMagnitude);
        transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
    }
}