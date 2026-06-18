using UnityEngine;
using System.Collections.Generic;

public class InvaderGridManager : MonoBehaviour
{
    // number of rows and columns spawned each wave
    [Header("Grid Dimensions")]
    public int rows = 5;
    public int columns = 11;
    public float spacingX = 1.5f;
    public float spacingZ = 1.5f;

    // lateral speed increases each wave and with each kill; drop controls how far the grid steps forward on wall bounce
    [Header("Movement Settings")]
    public float baseSpeed = 1f;
    public float speedMultiplierPerWave = 0.25f;
    public float speedIncreasePerKill = 0.05f;
    public float dropAmountZ = 1.0f;
    public float xBoundary = 8f;

    // single centralised particle system used for all enemy laser emissions
    [Header("Combat & Scaling (Particles)")]
    [Tooltip("The central Particle System that handles all enemy lasers.")]
    public ParticleSystem enemyLaserParticles;
    public float enemyLaserSpeed = 12f;
    public float baseFireRate = 2.0f;
    public float fireRateSpeedUpPerWave = 0.2f;

    // pool of prefabs randomly selected when spawning each invader
    [Header("Prefabs")]
    public GameObject[] invaderPrefabs;

    // live list of all invader gameobjects currently in the scene
    private readonly List<GameObject> activeInvaders = new List<GameObject>();

    // 1 for moving right, -1 for moving left
    private int direction = 1;
    private float currentSpeed;
    private int waveCount = 1;

    // recorded at start so the grid can be snapped back to it each wave
    private Vector3 initialGridPosition;
    private float shotCooldownTimer;

    // fire rate decreases each wave, clamped to a minimum to prevent instant firing
    private float CurrentFireRate => Mathf.Max(0.4f, baseFireRate - ((waveCount - 1) * fireRateSpeedUpPerWave));

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

    // resets the grid's position and direction, scales difficulty, announces the wave, then spawns invaders
    private void StartNewWave()
    {
        transform.position = initialGridPosition;
        direction = 1;

        currentSpeed = baseSpeed + ((waveCount - 1) * speedMultiplierPerWave);
        shotCooldownTimer = CurrentFireRate;

        if (SpaceInvadersManager.Instance != null)
            SpaceInvadersManager.Instance.AnnounceNewWave(waveCount);

        SpawnGrid();
    }

    // instantiates a full grid of invaders as children of this transform using local-space offsets
    private void SpawnGrid()
    {
        activeInvaders.Clear();
        if (invaderPrefabs == null || invaderPrefabs.Length == 0) return;

        for (int row = 0; row < rows; row++)
        {
            // centre the grid horizontally regardless of column count
            float width = (columns - 1) * spacingX;
            float startX = -width / 2f;

            for (int col = 0; col < columns; col++)
            {
                Vector3 spawnPos = new Vector3(startX + (col * spacingX), 0f, row * spacingZ);
                GameObject randomPrefab = invaderPrefabs[Random.Range(0, invaderPrefabs.Length)];

                // skip any unassigned prefab slots without breaking the loop
                if (randomPrefab == null) continue;

                GameObject invader = Instantiate(randomPrefab, transform);
                invader.transform.localPosition = spawnPos;
                invader.transform.localRotation = randomPrefab.transform.rotation;
                activeInvaders.Add(invader);
            }
        }
    }

    // moves the entire grid laterally and drops it forward when an invader touches a boundary
    private void MoveGrid()
    {
        if (activeInvaders.Count == 0) return;

        transform.Translate(Vector3.right * (direction * currentSpeed * Time.deltaTime));

        bool hitWall = false;

        // check if any invader has passed the horizontal boundary in the current direction
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

    // counts down the fire rate timer and triggers a shot when it expires
    private void HandleEnemyShooting()
    {
        if (activeInvaders.Count == 0 || enemyLaserParticles == null) return;

        shotCooldownTimer -= Time.deltaTime;

        if (shotCooldownTimer <= 0f)
        {
            shotCooldownTimer = CurrentFireRate;
            TriggerFrontRowShot();
        }
    }

    // collects all invaders in a clear firing lane and picks one at random to shoot
    private void TriggerFrontRowShot()
    {
        List<InvaderCollision> validShooters = new List<InvaderCollision>();

        foreach (GameObject invader in activeInvaders)
        {
            if (invader == null) continue;

            InvaderCollision shooterComp = invader.GetComponent<InvaderCollision>();
            if (shooterComp != null && shooterComp.IsFrontRowClear())
                validShooters.Add(shooterComp);
        }

        if (validShooters.Count > 0)
        {
            int randomIndex = Random.Range(0, validShooters.Count);
            validShooters[randomIndex].FireLaser();
        }
    }

    // emits a single laser particle from the given world position moving toward the player
    public void FireEnemyLaserParticle(Vector3 spawnPosition)
    {
        if (enemyLaserParticles != null)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = spawnPosition,
                velocity = Vector3.back * enemyLaserSpeed
            };

            enemyLaserParticles.Emit(emitParams, 1);
        }
    }

    // removes the invader from the active list, increases speed, and starts the next wave if the grid is cleared
    public void OnInvaderDestroyed(GameObject invader)
    {
        if (invader == null) return;

        if (activeInvaders.Contains(invader))
        {
            activeInvaders.Remove(invader);
            Destroy(invader);

            currentSpeed += speedIncreasePerKill;

            // delay slightly before spawning the next wave to give the player a brief pause
            if (activeInvaders.Count == 0)
            {
                waveCount++;
                Invoke(nameof(StartNewWave), 1.0f);
            }
        }
    }
}