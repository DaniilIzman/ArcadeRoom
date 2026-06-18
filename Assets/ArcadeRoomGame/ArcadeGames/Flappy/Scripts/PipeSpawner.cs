using UnityEngine;

public class SpaceObstacleSpawner : MonoBehaviour
{
    // pool of obstacle prefabs randomly selected on each spawn
    [Header("Spawning Setup")]
    public GameObject[] obstaclePrefabs;

    // spawn interval starts here and decreases toward the minimum over time
    [Header("Difficulty Curve")]
    [Tooltip("How often they spawn at the start of the game")]
    public float initialSpawnRate = 2.5f;

    // hard floor on the spawn interval to prevent obstacles from overlapping
    [Tooltip("The absolute fastest they will ever spawn (prevents impossible overlapping)")]
    public float minimumSpawnRate = 1.25f;

    // how much the spawn interval shrinks with each obstacle spawned
    [Tooltip("How much time is shaved off the spawn rate per obstacle (Lower = slower progression)")]
    public float accelerationFactor = 0.015f;

    // vertical and rotational randomisation applied to each spawned obstacle
    [Header("Spatial Randomization")]
    public float minYPosition = -2f;
    public float maxYPosition = 3f;
    public float maxRotationAngle = 10f;
    public Vector2 scaleMultiplierRange = new Vector2(0.85f, 1.15f);

    // tracks the current difficulty-adjusted interval and the countdown to the next spawn
    private float currentBaseSpawnRate;
    private float timer = 0f;
    private float timeUntilNextSpawn;

    private void Start()
    {
        currentBaseSpawnRate = initialSpawnRate;
        CalculateNextSpawnTime();

        // spawn one obstacle immediately so the player faces a challenge from the first frame
        SpawnObstacle();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= timeUntilNextSpawn)
        {
            SpawnObstacle();
            timer = 0f;

            // tighten the spawn interval each cycle until the minimum is reached
            if (currentBaseSpawnRate > minimumSpawnRate)
                currentBaseSpawnRate = Mathf.Max(currentBaseSpawnRate - accelerationFactor, minimumSpawnRate);

            CalculateNextSpawnTime();
        }
    }

    // adds small random variance to the base interval so spawns don't feel perfectly rhythmic
    private void CalculateNextSpawnTime()
    {
        float randomVariance = Random.Range(-0.2f, 0.2f);
        timeUntilNextSpawn = Mathf.Max(currentBaseSpawnRate + randomVariance, minimumSpawnRate);
    }

    // picks a random prefab, randomises its position, rotation, and scale, then instantiates it
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

        // apply a uniform scale multiplier within the configured range
        float randomScale = Random.Range(scaleMultiplierRange.x, scaleMultiplierRange.y);
        spawnedObstacle.transform.localScale *= randomScale;
    }
}