using System.Collections.Generic;
using UnityEngine;

public class TrackSegmentRandomizer : MonoBehaviour
{
    [Header("Environment Pools")]
    [Tooltip("Drag your empty environment group objects directly into this list.")]
    public List<GameObject> leftWallEnvironment;
    [Tooltip("Drag your empty environment group objects directly into this list.")]
    public List<GameObject> rightWallEnvironment;

    [Header("Street Pools")]
    [Tooltip("Drag your empty street group objects directly into this list.")]
    public List<GameObject> streetVariants;

    [Header("Spawn Chances")]
    [Range(0f, 1f)] public float streetSpawnChance = 0.6f; 

    private void OnEnable()
    {
        RandomizeSegment();
    }

    public void RandomizeSegment()
    {
        // Walls always spawn (100% chance), they just pick a random layout from their lists
        EvaluatePool(leftWallEnvironment, 1f);
        EvaluatePool(rightWallEnvironment, 1f);
        
        // Street variations spawn based on your set chance percentage
        EvaluatePool(streetVariants, streetSpawnChance);
    }

    private void EvaluatePool(List<GameObject> variantList, float spawnChance)
    {
        if (variantList == null || variantList.Count == 0) return;

        // Roll the dice to see if this category should spawn anything at all
        bool dynamicSpawnCheck = Random.value <= spawnChance;

        // If true, pick a random index. If false, set to -1 so ALL variants in this pool get destroyed.
        int chosenIndex = dynamicSpawnCheck ? Random.Range(0, variantList.Count) : -1;

        // Loop through the layout options assigned in the inspector
        for (int i = 0; i < variantList.Count; i++)
        {
            if (variantList[i] == null) continue;

            if (i == chosenIndex)
            {
                // Keep the chosen variant group intact and ensure it is visible
                variantList[i].SetActive(true);
            }
            else
            {
                // Permanently delete the unused variant groups 
                // from this specific spawned instance of the road prefab.
                Destroy(variantList[i]);
            }
        }
    }
}