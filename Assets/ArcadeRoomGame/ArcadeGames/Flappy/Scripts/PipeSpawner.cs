using UnityEngine;

public class SpaceObstacleSpawner : MonoBehaviour
{
    [Header("Spawning Setup")]
    public GameObject[] obstaclePrefabs;
    
    [Header("Difficulty Curve")]
    [Tooltip("How often they spawn at the start of the game")]
    public float initialSpawnRate = 2.5f;      
    
    [Tooltip("The absolute fastest they will ever spawn (prevents impossible overlapping)")]
    public float minimumSpawnRate = 1.25f;    
    
    [Tooltip("How much time is shaved off the spawn rate per obstacle (Lower = slower progression)")]
    public float accelerationFactor = 0.015f; 

    [Header("Spatial Randomization")]
    public float minYPosition = -2f;
    public float maxYPosition = 3f;
    public float maxRotationAngle = 10f; 
    public Vector2 scaleMultiplierRange = new Vector2(0.85f, 1.15f);

    private float currentBaseSpawnRate;
    private float timer = 0f;
    private float timeUntilNextSpawn;

    private void Start()
    {
        currentBaseSpawnRate = initialSpawnRate;
        CalculateNextSpawnTime();
        SpawnObstacle();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= timeUntilNextSpawn)
        {
            SpawnObstacle();
            timer = 0f; 
            
            // Push difficulty limits up incrementally, but MUCH slower now
            if (currentBaseSpawnRate > minimumSpawnRate)
            {
                currentBaseSpawnRate = Mathf.Max(currentBaseSpawnRate - accelerationFactor, minimumSpawnRate);
            }

            CalculateNextSpawnTime();
        }
    }

    private void CalculateNextSpawnTime()
    {
        // Adds a tiny bit of randomness (+/- 0.2 seconds) to the spawn rate 
        // so the player can't rely on a perfect rhythmic metronome in their head.
        float randomVariance = Random.Range(-0.2f, 0.2f);
        timeUntilNextSpawn = Mathf.Max(currentBaseSpawnRate + randomVariance, minimumSpawnRate);
    }

    private void SpawnObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject prefabToSpawn = obstaclePrefabs[randomIndex];

        float randomHeight = Random.Range(minYPosition, maxYPosition);
        Vector3 spawnPos = new Vector3(transform.position.x, randomHeight, 0f);

        float randomZRot = Random.Range(-maxRotationAngle, maxRotationAngle);
        Quaternion spawnRot = Quaternion.Euler(0f, 0f, randomZRot);

        GameObject spawnedObstacle = Instantiate(prefabToSpawn, spawnPos, spawnRot);

        float randomScale = Random.Range(scaleMultiplierRange.x, scaleMultiplierRange.y);
        spawnedObstacle.transform.localScale *= randomScale;
    }
}