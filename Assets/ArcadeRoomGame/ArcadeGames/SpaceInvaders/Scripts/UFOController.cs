using UnityEngine;

public class UFOController : MonoBehaviour
{
    [Header("Movement Profiles")]
    public float speed = 6f;
    public float leftSpawnX = -12f;
    public float rightDestroyX = 12f;
    public float spawnZCoordinate = 8f;

    [Header("Timing Loops")]
    public float minSpawnInterval = 15f;
    public float maxSpawnInterval = 30f;

    [Header("Audio (Optional)")]
    public AudioSource ufoAudioSource;
    public AudioClip ufoLoopSound;

    private bool isMoving = false;
    private float nextSpawnTime;

    private void Start()
    {
        // deactivate the visuals/physics initially
        ToggleUFOState(false);
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (!isMoving)
        {
            if (Time.time >= nextSpawnTime)
            {
                LaunchUFO();
            }
        }
        else
        {
            // move steadily across the screen along the positive X axis
            transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);

            // check if it has passed beyond the right boundary
            if (transform.position.x >= rightDestroyX)
            {
                ResetUFO();
            }
        }
    }

    private void LaunchUFO()
    {
        isMoving = true;
        // position the UFO at the far left edge on the gameplay plane (Y=0)
        transform.position = new Vector3(leftSpawnX, 0f, spawnZCoordinate);
        ToggleUFOState(true);

        if (ufoAudioSource != null && ufoLoopSound != null)
        {
            ufoAudioSource.clip = ufoLoopSound;
            ufoAudioSource.loop = true;
            ufoAudioSource.Play();
        }
    }

    private void ResetUFO()
    {
        isMoving = false;
        ToggleUFOState(false);
        
        if (ufoAudioSource != null) ufoAudioSource.Stop();
        
        ScheduleNextSpawn();
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void ToggleUFOState(bool state)
    {
        // keeps the controller running but hides meshes and disables colliders when inactive
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = state;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = state;
    }

    // handles player shooting down the UFO
    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            Debug.Log("UFO DESTROYED!");
            ResetUFO();
        }
    }
}