using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    [Header("Spawning Setup")]
    public GameObject pipePrefab;
    
    [Header("Difficulty Curve")]
    public float initialSpawnRate = 2f;      // How slow the game starts
    public float minimumSpawnRate = 0.8f;    // The absolute fastest the pipes will ever spawn
    public float accelerationFactor = 0.05f; // How much time is shaved off the spawn rate per pipe

    [Header("Height Randomization")]
    public float minYPosition = -2f;
    public float maxYPosition = 3f;

    private float currentSpawnRate;
    private float timer = 0f;

    private void Start()
    {
        // Set the starting speed
        currentSpawnRate = initialSpawnRate;
        
        // Spawn the very first pipe immediately
        SpawnPipe();
    }

    private void Update()
    {
        // Increment timer
        timer += Time.deltaTime;

        // Check if it's time to spawn a new pipe based on our CURRENT dynamic rate
        if (timer >= currentSpawnRate)
        {
            SpawnPipe();
            timer = 0f; // Reset timer
            
            // --- DIFFICULTY RAMP LOGIC ---
            // Squeeze the spawn rate to make the game faster, but clamp it at the minimum limit
            if (currentSpawnRate > minimumSpawnRate)
            {
                currentSpawnRate -= accelerationFactor;
                currentSpawnRate = Mathf.Max(currentSpawnRate, minimumSpawnRate);
            }
        }
    }

    private void SpawnPipe()
    {
        // Generate a random Y height within our limits
        float randomHeight = Random.Range(minYPosition, maxYPosition);
        
        // The spawn position uses the Spawner's X, but the randomized Y
        Vector3 spawnPos = new Vector3(transform.position.x, randomHeight, 0);

        // Create the pipe
        Instantiate(pipePrefab, spawnPos, Quaternion.identity);
    }
}