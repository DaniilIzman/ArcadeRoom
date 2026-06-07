using UnityEngine;
using System.Collections.Generic;

public class InvaderGridManager : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public int rows = 5;
    public int columns = 11;
    public float spacingX = 1.5f;
    public float spacingZ = 1.5f;

    [Header("Movement Settings")]
    public float baseSpeed = 1f;
    public float speedMultiplierPerWave = 0.25f;
    public float speedIncreasePerKill = 0.05f;
    public float dropAmountZ = 1.0f; 
    public float xBoundary = 8f;

    [Header("Prefabs")]
    public GameObject[] invaderPrefabs; 

    private List<GameObject> activeInvaders = new List<GameObject>();
    private int direction = 1; 
    private float currentSpeed;
    private int waveCount = 1;
    private Vector3 initialGridPosition;

    private void Start()
    {
        initialGridPosition = transform.position;
        StartNewWave();
    }

    private void Update()
    {
        MoveGrid();
    }

    private void StartNewWave()
    {
        transform.position = initialGridPosition;
        direction = 1;

        currentSpeed = baseSpeed + ((waveCount - 1) * speedMultiplierPerWave);
        Debug.Log($"👾 WAVE {waveCount} START! Base Speed: {currentSpeed}");

        SpawnGrid();
    }

    private void SpawnGrid()
    {
        activeInvaders.Clear();

        for (int row = 0; row < rows; row++)
        {
            GameObject prefab = invaderPrefabs[Mathf.Min(row, invaderPrefabs.Length - 1)];

            float width = (columns - 1) * spacingX;
            float startX = -width / 2f;

            for (int col = 0; col < columns; col++)
            {
                Vector3 spawnPos = new Vector3(
                    startX + (col * spacingX),
                    0f, 
                    row * spacingZ
                );

                GameObject invader = Instantiate(prefab, transform);
                invader.transform.localPosition = spawnPos;
                
                activeInvaders.Add(invader);
            }
        }
    }

    private void MoveGrid()
    {
        if (activeInvaders.Count == 0) return;

        transform.Translate(Vector3.right * direction * currentSpeed * Time.deltaTime);

        bool hitWall = false;
        foreach (GameObject invader in activeInvaders)
        {
            if (invader == null) continue;

            if ((direction == 1 && invader.transform.position.x >= xBoundary) ||
                (direction == -1 && invader.transform.position.x <= -xBoundary))
            {
                hitWall = true;
                break;
            }
        }

        if (hitWall)
        {
            direction *= -1;
            transform.position += new Vector3(0f, 0f, -dropAmountZ);
        }
    }

    public void OnInvaderDestroyed(GameObject invader)
    {
        if (activeInvaders.Contains(invader))
        {
            activeInvaders.Remove(invader);
            Destroy(invader);

            currentSpeed += speedIncreasePerKill;

            if (activeInvaders.Count == 0)
            {
                waveCount++;
                Invoke(nameof(StartNewWave), 1.0f); 
            }
        }
    }
}