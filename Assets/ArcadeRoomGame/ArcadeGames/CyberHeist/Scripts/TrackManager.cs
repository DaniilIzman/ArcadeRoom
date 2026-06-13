using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance { get; private set; }

    [Header("Track Setup")]
    public GameObject[] trackPrefabs;
    public float segmentLength = 30f;
    public int segmentsOnScreen = 5;

    [Header("Platform Transform Alignment")]
    public Vector3 trackOffset = new Vector3(0f, -1f, 0f);
    public float startingZ = 3.2f;

    [Header("Treadmill Speed")]
    public float currentSpeed = 15f;
    public float maxSpeed = 40f;
    public float acceleration = 0.2f;

    private List<GameObject> activeTracks = new List<GameObject>();
    private float distanceAccumulator = 0f;
    private float initialSpeed;
    
    // speed boost variables
    private float speedMultiplier = 1f;
    private Coroutine speedBoostRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        initialSpeed = currentSpeed;
        
        for (int i = 0; i < segmentsOnScreen; i++)
        {
            SpawnTrack(Random.Range(0, trackPrefabs.Length));
        }
    }

    private void Update()
    {
        if (EndlessRunnerManager.Instance != null && 
           (EndlessRunnerManager.Instance.isGameOver || EndlessRunnerManager.Instance.isPaused))
            return;

        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }

        float effectiveSpeed = currentSpeed * speedMultiplier;

        foreach (GameObject track in activeTracks)
        {
            track.transform.position += Vector3.back * effectiveSpeed * Time.deltaTime;
        }

        if (activeTracks[0].transform.position.z < (startingZ - segmentLength))
        {
            RecycleTrack();
        }

        CalculateDistance(effectiveSpeed);
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostRoutine != null) StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(SpeedBoostSequence(multiplier, duration));
    }

    private IEnumerator SpeedBoostSequence(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f;
    }

    private void SpawnTrack(int trackIndex)
    {
        Vector3 spawnPosition = trackOffset;

        if (activeTracks.Count > 0)
        {
            spawnPosition.z = activeTracks[activeTracks.Count - 1].transform.position.z + segmentLength;
        }
        else
        {
            spawnPosition.z = startingZ;
        }

        GameObject newTrack = Instantiate(trackPrefabs[trackIndex], spawnPosition, Quaternion.identity);
        newTrack.transform.SetParent(transform); 
        activeTracks.Add(newTrack);
    }

    private void RecycleTrack()
    {
        GameObject oldTrack = activeTracks[0];
        activeTracks.RemoveAt(0);
        Destroy(oldTrack);

        SpawnTrack(Random.Range(0, trackPrefabs.Length));
    }

    private void CalculateDistance(float effectiveSpeed)
    {
        if (EndlessRunnerManager.Instance == null) return;

        distanceAccumulator += effectiveSpeed * Time.deltaTime;
        
        if (distanceAccumulator >= 10f)
        {
            EndlessRunnerManager.Instance.AddDistance(1);
            distanceAccumulator -= 10f;
        }
    }

    public float GetAnimationSpeedMultiplier()
    {
        if (initialSpeed <= 0) return 1f;
        return (currentSpeed * speedMultiplier) / initialSpeed;
    }
}