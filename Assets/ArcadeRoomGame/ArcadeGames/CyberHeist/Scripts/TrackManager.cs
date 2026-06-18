using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// manages the infinite treadmill of track segments; spawns, moves, and recycles them,
// handles speed acceleration and temporary speed boosts, and reports distance to the manager
public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance { get; private set; }

    [Header("Track Setup")]
    [Tooltip("Drag your obstacle-free starting track here")]
    public GameObject starterTrackPrefab; // first segment spawned; guaranteed to have no obstacles
    public GameObject[] trackPrefabs;     // pool of random segments used for all subsequent spawns
    public float segmentLength = 30f;     // world-space length of a single track segment
    public int segmentsOnScreen = 5;      // total number of segments kept alive at once

    [Header("Platform Transform Alignment")]
    public Vector3 trackOffset = new Vector3(0f, -1f, 0f); // y offset applied to every spawned segment
    public float startingZ = 3.2f;                          // z position of the very first segment

    [Header("Treadmill Speed")]
    public float currentSpeed = 15f;  // units per second the track currently moves toward the player
    public float maxSpeed = 40f;      // speed cap; the track will never accelerate beyond this
    public float acceleration = 0.2f; // units per second added to current speed each second

    // list of all track segments currently in the scene, ordered front to back
    private List<GameObject> activeTracks = new List<GameObject>();

    // accumulates fractional distance units between whole-metre ticks
    private float distanceAccumulator = 0f;

    // the speed at the start of the run, used to calculate the animation speed multiplier
    private float initialSpeed;

    // current speed multiplier applied on top of currentspeed during a speed boost
    private float speedMultiplier = 1f;

    // reference kept so the speed boost coroutine can be cancelled and restarted on re-pickup
    private Coroutine speedBoostRoutine;

    private void Awake()
    {
        // enforce singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        initialSpeed = currentSpeed;

        // spawn the safe starter segment first so the player never appears in front of obstacles
        if (starterTrackPrefab != null)
        {
            SpawnSpecificTrack(starterTrackPrefab);
        }

        // fill the remainder of the visible track with random segments;
        // subtract 1 from the total if the starter segment already occupies a slot
        int segmentsToSpawn = (starterTrackPrefab != null) ? segmentsOnScreen - 1 : segmentsOnScreen;
        for (int i = 0; i < segmentsToSpawn; i++)
        {
            SpawnTrack(Random.Range(0, trackPrefabs.Length));
        }
    }

    private void Update()
    {
        // freeze the track while the game is over or paused
        if (EndlessRunnerManager.Instance != null &&
           (EndlessRunnerManager.Instance.isGameOver || EndlessRunnerManager.Instance.isPaused))
            return;

        // gradually increase the base speed up to the cap to make the run harder over time
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }

        // combine the base speed with any active boost multiplier
        float effectiveSpeed = currentSpeed * speedMultiplier;

        // scroll every active segment toward the player by moving them in the -z direction
        foreach (GameObject track in activeTracks)
        {
            track.transform.position += Vector3.back * effectiveSpeed * Time.deltaTime;
        }

        // when the oldest segment has scrolled fully behind the spawn point, recycle it
        if (activeTracks[0].transform.position.z < (startingZ - segmentLength))
        {
            RecycleTrack();
        }

        CalculateDistance(effectiveSpeed);
    }

    // starts or restarts a timed speed boost; restarting resets the duration
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostRoutine != null) StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(SpeedBoostSequence(multiplier, duration));
    }

    // applies the speed multiplier for the given duration, then resets it to 1
    private IEnumerator SpeedBoostSequence(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f;
    }

    // spawns a specific prefab at the end of the current track chain
    private void SpawnSpecificTrack(GameObject prefabToSpawn)
    {
        Vector3 spawnPosition = trackOffset;

        if (activeTracks.Count > 0)
        {
            // place the new segment directly after the last one in the chain
            spawnPosition.z = activeTracks[activeTracks.Count - 1].transform.position.z + segmentLength;
        }
        else
        {
            // the very first segment uses the configured starting z position
            spawnPosition.z = startingZ;
        }

        GameObject newTrack = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // parent to this transform to keep the hierarchy tidy
        newTrack.transform.SetParent(transform);
        activeTracks.Add(newTrack);
    }

    // looks up a prefab by index in the random pool and passes it to the base spawn method
    private void SpawnTrack(int trackIndex)
    {
        SpawnSpecificTrack(trackPrefabs[trackIndex]);
    }

    // destroys the oldest segment, removes it from the list, and spawns a new random one at the far end
    private void RecycleTrack()
    {
        GameObject oldTrack = activeTracks[0];
        activeTracks.RemoveAt(0);
        Destroy(oldTrack);

        // infinite generation always pulls from the random array, never the starter segment
        SpawnTrack(Random.Range(0, trackPrefabs.Length));
    }

    // accumulates movement and reports every 10 units of travel as 1 metre to the game manager
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

    // returns the ratio of current effective speed to initial speed, used to scale
    // the run animation and footstep pitch so they stay in sync with the track
    public float GetAnimationSpeedMultiplier()
    {
        if (initialSpeed <= 0) return 1f;
        return (currentSpeed * speedMultiplier) / initialSpeed;
    }
}