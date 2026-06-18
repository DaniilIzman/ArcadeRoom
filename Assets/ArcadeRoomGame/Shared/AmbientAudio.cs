using UnityEngine;
using System.Collections;

// singleton that manages the global background music track across scenes
public class AmbientAudio : MonoBehaviour
{
    public static AmbientAudio Instance { get; private set; }
    private AudioSource audioSource;

    private void Awake()
    {
        // destroy any duplicate instances to enforce the singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();

        // ensure the music loops continuously without interruption
        audioSource.loop = true;
    }

    // pauses playback without losing the current position in the track
    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying) audioSource.Pause();
    }

    // resumes from where the track was paused
    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying) audioSource.UnPause();
    }

    // public entry point to begin a fade-out over the given duration
    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutSequence(duration));
    }

    // smoothly reduces volume to zero then stops the audiosource completely
    private IEnumerator FadeOutSequence(float duration)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / duration);
            yield return null;
        }

        // clamp volume to exactly zero and stop playback once the fade is complete
        audioSource.volume = 0f;
        audioSource.Stop();
    }
}