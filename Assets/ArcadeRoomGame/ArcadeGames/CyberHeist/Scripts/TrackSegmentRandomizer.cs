using System.Collections.Generic;
using UnityEngine;

// runs once when a track segment is enabled; randomly selects one variant from each
// environment and street pool and permanently destroys the unused ones on this instance
public class TrackSegmentRandomizer : MonoBehaviour
{
    [Header("Environment Pools")]
    [Tooltip("Drag your empty environment group objects directly into this list.")]
    public List<GameObject> leftWallEnvironment;  // groups of left-side decoration variants
    [Tooltip("Drag your empty environment group objects directly into this list.")]
    public List<GameObject> rightWallEnvironment; // groups of right-side decoration variants

    [Header("Street Pools")]
    [Tooltip("Drag your empty street group objects directly into this list.")]
    public List<GameObject> streetVariants; // groups of street decoration variants

    [Header("Spawn Chances")]
    [Range(0f, 1f)] public float streetSpawnChance = 0.6f; // probability that any street variant spawns at all

    private void OnEnable()
    {
        // randomize the segment as soon as it becomes active in the scene
        RandomizeSegment();
    }

    public void RandomizeSegment()
    {
        // walls always appear; pass 1f so the chance roll always succeeds for both sides
        EvaluatePool(leftWallEnvironment, 1f);
        EvaluatePool(rightWallEnvironment, 1f);

        // street decorations appear only when the random roll passes the configured chance
        EvaluatePool(streetVariants, streetSpawnChance);
    }

    // picks one variant from the list to keep and permanently destroys all others;
    // if the spawn chance roll fails, all variants in the list are destroyed
    private void EvaluatePool(List<GameObject> variantList, float spawnChance)
    {
        if (variantList == null || variantList.Count == 0) return;

        // roll against the spawn chance to decide whether anything in this pool should appear
        bool dynamicSpawnCheck = Random.value <= spawnChance;

        // choose the index to keep, or -1 to destroy everything in the pool
        int chosenIndex = dynamicSpawnCheck ? Random.Range(0, variantList.Count) : -1;

        for (int i = 0; i < variantList.Count; i++)
        {
            if (variantList[i] == null) continue;

            if (i == chosenIndex)
            {
                // activate the chosen variant so it is visible
                variantList[i].SetActive(true);
            }
            else
            {
                // permanently remove unchosen variants from this spawned prefab instance
                // so they don't consume memory or draw calls for the lifetime of the segment
                Destroy(variantList[i]);
            }
        }
    }
}