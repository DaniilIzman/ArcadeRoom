using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SpatialAudioEmitter : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("The looping ambient sound for this object.")]
    public AudioClip ambientSound;

    [Header("3D Spatial Settings")]
    [Tooltip("Distance where sound is at maximum volume.")]
    public float minDistance = 1.5f;
    [Tooltip("Distance where sound becomes completely silent.")]
    public float maxDistance = 7f;

    private AudioSource audioSource;
    private float defaultVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        defaultVolume = audioSource.volume;

        // Automatically configure the Area of Hearing (3D Spatial Audio)
        audioSource.spatialBlend = 1f; // 1 = completely 3D space
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;

        if (ambientSound != null)
        {
            audioSource.clip = ambientSound;
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.Play();
        }
        else
        {
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Smoothly fades out the localized audio over a given duration.
    /// </summary>
    public void FadeOut(float duration)
    {
        if (gameObject.activeInHierarchy && audioSource.isPlaying)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(defaultVolume, 0f, elapsedTime / duration);
            yield return null;
        }
        audioSource.volume = 0f;
    }
}