using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    [Header("Track Setup")]
    public GameObject[] trackPrefabs;
    public float segmentLength = 30f;
    public int segmentsOnScreen = 5;

    [Header("Your Platform Transform Alignment")]
    [Tooltip("Matches your platform's X and Y coordinates exactly.")]
    public Vector3 trackOffset = new Vector3(0f, -1f, 0f);
    [Tooltip("The starting Z position of your very first platform piece.")]
    public float startingZ = 3.2f;

    [Header("Treadmill Speed")]
    public float currentSpeed = 15f;
    public float maxSpeed = 40f;
    public float acceleration = 0.2f;

    private List<GameObject> activeTracks = new List<GameObject>();
    private float distanceAccumulator = 0f;

    private void Start()
    {
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

        // move the track pieces backward along the Z axis
        foreach (GameObject track in activeTracks)
        {
            track.transform.position += Vector3.back * currentSpeed * Time.deltaTime;
        }

        // recycle the piece once it has fully cleared the player's view behind Z = 0
        // factor in startingZ to ensure it doesn't vanish too early or too late
        if (activeTracks[0].transform.position.z < (startingZ - segmentLength))
        {
            RecycleTrack();
        }

        CalculateDistance();
    }

    private void SpawnTrack(int trackIndex)
    {
        Vector3 spawnPosition = trackOffset;

        if (activeTracks.Count > 0)
        {
            // chain directly off the end of the previous piece
            spawnPosition.z = activeTracks[activeTracks.Count - 1].transform.position.z + segmentLength;
        }
        else
        {
            // this is the very first piece, start it exactly at your local Z coordinate
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

    private void CalculateDistance()
    {
        if (EndlessRunnerManager.Instance == null) return;

        distanceAccumulator += currentSpeed * Time.deltaTime;
        
        if (distanceAccumulator >= 10f)
        {
            EndlessRunnerManager.Instance.AddDistance(1);
            distanceAccumulator -= 10f;
        }
    }
}