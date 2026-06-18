using UnityEngine;

public class RandomRotation : MonoBehaviour
{
    // min and max degrees per frame for each axis, randomised on start
    [Header("Randomization")]
    [SerializeField] private float randomRotateXMin = 0.1f;
    [SerializeField] private float randomRotateXMax = 0.3f;
    [SerializeField] private float randomRotateYMin = 0.05f;
    [SerializeField] private float randomRotateYMax = 0.1f;
    [SerializeField] private float randomRotateZMin = 0.01f;
    [SerializeField] private float randomRotateZMax = 0.05f;

    // per-axis rotation amounts picked once and reused every frame
    private float rotateX;
    private float rotateY;
    private float rotateZ;

    void Start()
    {
        rotateX = Random.Range(randomRotateXMin, randomRotateXMax);
        rotateY = Random.Range(randomRotateYMin, randomRotateYMax);
        rotateZ = Random.Range(randomRotateZMin, randomRotateZMax);
    }

    void Update()
    {
        transform.Rotate(rotateX, rotateY, rotateZ);
    }
}