using UnityEngine;

public class PipeMovement : MonoBehaviour
{
    // speed at which the pipe scrolls toward the player each second
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    // x position at which the pipe is destroyed to free memory once it is off screen
    public float destroyXPosition = -15f;

    private void Update()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (transform.position.x <= destroyXPosition)
            Destroy(gameObject);
    }
}