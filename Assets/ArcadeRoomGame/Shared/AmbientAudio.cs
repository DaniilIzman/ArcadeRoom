using UnityEngine;
using System.Collections;

public class AmbientAudio : MonoBehaviour
{
    public static AmbientAudio Instance { get; private set; }
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
    }

    // Pause/Resume controls
    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying) audioSource.Pause();
    }

    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying) audioSource.UnPause();
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutSequence(duration));
    }

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

        audioSource.volume = 0f;
        audioSource.Stop();
    }
}