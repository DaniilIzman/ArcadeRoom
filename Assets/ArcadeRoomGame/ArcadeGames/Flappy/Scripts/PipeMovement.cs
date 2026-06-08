using UnityEngine;

public class PipeMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float destroyXPosition = -15f; 

    private void Update()
    {
        // move the pipe left at a constant speed
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // destroy the pipe once it passes the left edge of the screen
        if (transform.position.x <= destroyXPosition)
        {
            Destroy(gameObject);
        }
    }
}