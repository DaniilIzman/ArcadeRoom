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

    [Header("Combat & Scaling (Particles)")]
    [Tooltip("The central Particle System that handles all enemy lasers.")]
    public ParticleSystem enemyLaserParticles; 
    public float enemyLaserSpeed = 12f;
    public float baseFireRate = 2.0f;
    public float fireRateSpeedUpPerWave = 0.2f;

    [Header("Prefabs")]
    public GameObject[] invaderPrefabs; 

    private List<GameObject> activeInvaders = new List<GameObject>();
    private int direction = 1; 
    private float currentSpeed;
    private int waveCount = 1;
    private Vector3 initialGridPosition;
    private float shotCooldownTimer;

    private void Start()
    {
        initialGridPosition = transform.position;
        StartNewWave();
    }

    private void Update()
    {
        MoveGrid();
        HandleEnemyShooting();
    }

    private void StartNewWave()
    {
        transform.position = initialGridPosition;
        direction = 1;

        currentSpeed = baseSpeed + ((waveCount - 1) * speedMultiplierPerWave);
        float currentFireRate = Mathf.Max(0.4f, baseFireRate - ((waveCount - 1) * fireRateSpeedUpPerWave));
        shotCooldownTimer = currentFireRate;

        SpawnGrid();
    }

    private void SpawnGrid()
    {
        activeInvaders.Clear();
        if (invaderPrefabs.Length == 0) return;

        for (int row = 0; row < rows; row++)
        {
            float width = (columns - 1) * spacingX;
            float startX = -width / 2f;

            for (int col = 0; col < columns; col++)
            {
                Vector3 spawnPos = new Vector3(startX + (col * spacingX), 0f, row * spacingZ);
                GameObject randomPrefab = invaderPrefabs[Random.Range(0, invaderPrefabs.Length)];

                GameObject invader = Instantiate(randomPrefab, transform);
                invader.transform.localPosition = spawnPos;
                invader.transform.localRotation = randomPrefab.transform.rotation; 
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

    private void HandleEnemyShooting()
    {
        if (activeInvaders.Count == 0 || enemyLaserParticles == null) return;

        shotCooldownTimer -= Time.deltaTime;

        if (shotCooldownTimer <= 0f)
        {
            float currentFireRate = Mathf.Max(0.4f, baseFireRate - ((waveCount - 1) * fireRateSpeedUpPerWave));
            shotCooldownTimer = currentFireRate;
            TriggerFrontRowShot();
        }
    }

    private void TriggerFrontRowShot()
    {
        List<InvaderCollision> validShooters = new List<InvaderCollision>();

        foreach (GameObject invader in activeInvaders)
        {
            if (invader == null) continue;
            
            InvaderCollision shooterComp = invader.GetComponent<InvaderCollision>();
            if (shooterComp != null && shooterComp.IsFrontRowClear())
            {
                validShooters.Add(shooterComp);
            }
        }

        if (validShooters.Count > 0)
        {
            int randomIndex = Random.Range(0, validShooters.Count);
            validShooters[randomIndex].FireLaser();
        }
    }

    // emits a particle from the specific alien's position
    public void FireEnemyLaserParticle(Vector3 spawnPosition)
    {
        if (enemyLaserParticles != null)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = spawnPosition;
            emitParams.velocity = Vector3.back * enemyLaserSpeed; 
            
            // emits exactly 1 laser particle
            enemyLaserParticles.Emit(emitParams, 1);
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