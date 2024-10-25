using UnityEngine;
using System.Collections;
using System;

public static class AudioFade
{
    public static IEnumerator FadeAudioSource(AudioSource audioSource, float targetVolume, float duration)
    {
        if (audioSource == null)
            yield break;

        float currentTime = 0;
        float startVolume = audioSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    /*
     *  Example Usage:
     *
     *  StartCoroutine(AudioFade.FadeAudioSource(backgroundMusic, 0.0f, 2.0f, OnFadeOutComplete));
     *  void OnFadeOutComplete()
     *  {
     *      audioSource.Stop();
     *      Debug.Log("Fade out completed and audio stopped.");
     *  }
     *
     */
    public static IEnumerator FadeAudioSource(AudioSource audioSource, float targetVolume, float duration, Action onComplete = null)
    {
        if (audioSource == null)
            yield break;

        float currentTime = 0;
        float startVolume = audioSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;

        onComplete?.Invoke();
    }
}

/*
 *  Example Usage:
 *  StartCoroutine(audioSource.FadeTo(targetVolume, duration));
 */
public static class AudioSourceExtensions
{
    public static IEnumerator FadeTo(this AudioSource audioSource, float targetVolume, float duration)
    {
        if (audioSource == null)
            yield break;

        float currentTime = 0;
        float startVolume = audioSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}