using UnityEngine;
using System.Collections;

public class AmbientAudio : MonoBehaviour
{
    public static AmbientAudio Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        // set up a scene-local singleton instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        
        // enforce loop settings for continuous background noise
        audioSource.loop = true;
    }

    /// smoothly linear-interpolates the volume down to 0 over a given timeframe
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