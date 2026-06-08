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

    private readonly List<GameObject> activeInvaders = new List<GameObject>();
    private int direction = 1; 
    private float currentSpeed;
    private int waveCount = 1;
    private Vector3 initialGridPosition;
    private float shotCooldownTimer;

    // computed property to dynamically return scaled fire rate based on current wave progress
    private float CurrentFireRate => Mathf.Max(0.4f, baseFireRate - ((waveCount - 1) * fireRateSpeedUpPerWave));

    private void Start()
    {
        // cache global origin point for resetting between rounds
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
        // snap parent point back to standard spawn entry origin
        transform.position = initialGridPosition;
        direction = 1;

        // adjust movement and shooting attributes for the current difficulty curve
        currentSpeed = baseSpeed + ((waveCount - 1) * speedMultiplierPerWave);
        shotCooldownTimer = CurrentFireRate;

        // tell ui to display wave number and play matching tracking audio
        if (SpaceInvadersManager.Instance != null)
        {
            SpaceInvadersManager.Instance.AnnounceNewWave(waveCount);
        }

        SpawnGrid();
    }

    private void SpawnGrid()
    {
        activeInvaders.Clear();
        if (invaderPrefabs == null || invaderPrefabs.Length == 0) return;

        // generate row and column structural array coordinates
        for (int row = 0; row < rows; row++)
        {
            float width = (columns - 1) * spacingX;
            float startX = -width / 2f;

            for (int col = 0; col < columns; col++)
            {
                Vector3 spawnPos = new Vector3(startX + (col * spacingX), 0f, row * spacingZ);
                GameObject randomPrefab = invaderPrefabs[Random.Range(0, invaderPrefabs.Length)];

                // skip instantiation if an individual prefab array slot happens to be unassigned
                if (randomPrefab == null) continue;

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

        // progress parent root position laterally
        transform.Translate(Vector3.right * (direction * currentSpeed * Time.deltaTime));

        bool hitWall = false;
        
        // iterate across existing elements to discover horizontal boundary oversteps
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

        // reverse directions and step closer to the player's line when walls are triggered
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
            shotCooldownTimer = CurrentFireRate;
            TriggerFrontRowShot();
        }
    }

    private void TriggerFrontRowShot()
    {
        List<InvaderCollision> validShooters = new List<InvaderCollision>();

        // extract every alien currently occupying a clear firing lane down toward the baseline
        foreach (GameObject invader in activeInvaders)
        {
            if (invader == null) continue;
            
            InvaderCollision shooterComp = invader.GetComponent<InvaderCollision>();
            if (shooterComp != null && shooterComp.IsFrontRowClear())
            {
                validShooters.Add(shooterComp);
            }
        }

        // choose exactly one casual shooter uniformly out of the compiled pool
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
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = spawnPosition,
                velocity = Vector3.back * enemyLaserSpeed
            };
            
            // emits exactly 1 laser particle
            enemyLaserParticles.Emit(emitParams, 1);
        }
    }

    public void OnInvaderDestroyed(GameObject invader)
    {
        if (invader == null) return;

        if (activeInvaders.Contains(invader))
        {
            activeInvaders.Remove(invader);
            Destroy(invader);
            
            // scale speed up gradually with each individual target clearance
            currentSpeed += speedIncreasePerKill;

            // verify if array is completely wiped to step up next wave loop progression
            if (activeInvaders.Count == 0)
            {
                waveCount++;
                Invoke(nameof(StartNewWave), 1.0f); 
            }
        }
    }
}