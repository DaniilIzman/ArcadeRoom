using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    [Header("Spawning Setup")]
    public GameObject pipePrefab;
    
    [Header("Difficulty Curve")]
    public float initialSpawnRate = 2f;      
    public float minimumSpawnRate = 0.8f;    
    public float accelerationFactor = 0.05f; 

    [Header("Height Randomization")]
    public float minYPosition = -2f;
    public float maxYPosition = 3f;

    private float currentSpawnRate;
    private float timer = 0f;

    private void Start()
    {
        currentSpawnRate = initialSpawnRate;
        SpawnPipe();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= currentSpawnRate)
        {
            SpawnPipe();
            timer = 0f; 
            
            // push difficulty limits up incrementally by reducing waiting intervals between pipe pairs
            if (currentSpawnRate > minimumSpawnRate)
            {
                currentSpawnRate = Mathf.Max(currentSpawnRate - accelerationFactor, minimumSpawnRate);
            }
        }
    }

    private void SpawnPipe()
    {
        if (pipePrefab == null) return;

        float randomHeight = Random.Range(minYPosition, maxYPosition);
        Vector3 spawnPos = new Vector3(transform.position.x, randomHeight, 0f);

        Instantiate(pipePrefab, spawnPos, Quaternion.identity);
    }
}