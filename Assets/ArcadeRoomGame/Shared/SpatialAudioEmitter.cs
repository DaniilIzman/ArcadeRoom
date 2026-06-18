using UnityEngine;
using System.Collections;

// requires an audiosource component on the same gameobject
[RequireComponent(typeof(AudioSource))]
public class SpatialAudioEmitter : MonoBehaviour
{
    // inspector-exposed audio clip to play as the looping ambient sound
    [Header("Audio Settings")]
    [Tooltip("The looping ambient sound for this object.")]
    public AudioClip ambientSound;

    // min/max distances that control how quickly the sound falls off in 3d space
    [Header("3D Spatial Settings")]
    [Tooltip("Distance where sound is at maximum volume.")]
    public float minDistance = 1.5f;
    [Tooltip("Distance where sound becomes completely silent.")]
    public float maxDistance = 7f;

    private AudioSource audioSource;

    // stores the original volume so fadeout can lerp from it
    private float defaultVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        defaultVolume = audioSource.volume;

        // configure the audiosource for fully positional 3d audio with linear rolloff
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;

        // assign and autoplay the clip if one has been provided
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

    // public entry point for fading out this emitter over a given duration
    public void FadeOut(float duration)
    {
        // only start the fade if the object is active and the source is currently playing
        if (gameObject.activeInHierarchy && audioSource.isPlaying)
        {
            StartCoroutine(FadeOutCoroutine(duration));
        }
    }

    // gradually reduces volume to zero over the specified duration
    private IEnumerator FadeOutCoroutine(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(defaultVolume, 0f, elapsedTime / duration);
            yield return null;
        }

        // ensure volume lands exactly at zero after the loop finishes
        audioSource.volume = 0f;
    }
}