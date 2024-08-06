using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class StSP_Player : MonoBehaviour
{
    [Header("Player Assignment:")]
	public AudioMixerGroup audioMixerGroup;
    [Space]
    public AudioClip monoSpatialClip;
    public AudioClip stereoStaticClip;

	[Header("Player Settings:")]
    public bool showDebug = false;
    public bool useDistanceAttenuation = true;
    public bool isLooping = false;

    [Header("Debug Inspector:")]
    public float _currentTime;

    [Header("Override Audio Sources:")]
    public AudioSource mono_source;
    public AudioSource stereo_source;

    // Start is called before the first frame update
    void Start()
    {
        // assign private generated audio sources
        AudioSource[] audioSources = GetComponents<AudioSource>();
        if (mono_source != null )
        {
            mono_source = audioSources[0];
        }
        if (stereo_source != null )
        {
            stereo_source = audioSources[1];
        }

        // assign mixer groups
        if (audioMixerGroup != null)
        {
            mono_source.outputAudioMixerGroup = audioMixerGroup;
            stereo_source.outputAudioMixerGroup = audioMixerGroup;
        }

        // assign audio clips to sources
        if (monoSpatialClip != null)
        {
            mono_source.clip = monoSpatialClip;
        }
        if (stereoStaticClip != null)
        {
            stereo_source.clip = stereoStaticClip;
        }

        // mono settings
        mono_source.spatialize = true;
        mono_source.spatialBlend = 1.0f;
        mono_source.loop = isLooping;
        // stereo settings
        stereo_source.spatialize = false;
        stereo_source.spatialBlend = 0.0f;
        stereo_source.loop = isLooping;
    }

    // Update is called once per frame
    void Update()
    {
        if (mono_source != null)
        {
            _currentTime = mono_source.time;
        }
    }

    public void Play()
    {        
        if (monoSpatialClip != null && stereoStaticClip != null)
        {
            mono_source.Play();
            stereo_source.Play();
            if (showDebug) Debug.Log($"[AUDIO] Played on {gameObject.name}");
        }
    }

    public void Stop()
    {        
        if (mono_source.isPlaying)
        {
            mono_source.Stop();
            stereo_source.Stop();
            if (showDebug) Debug.Log($"[AUDIO] Stopped on {gameObject.name}");
        }
    }
 
    public void Pause()
    {
        if (mono_source.isPlaying) 
        {
            _currentTime = mono_source.time;
            mono_source.Pause();
            stereo_source.Pause();
            if (showDebug) Debug.Log($"[AUDIO] Paused on {gameObject.name} at {_currentTime}");
        }
    }
 
    public void UnPause()
    {
        if (monoSpatialClip != null && !mono_source.isPlaying) 
        {
            mono_source.UnPause();
            stereo_source.UnPause();
            if (showDebug) Debug.Log($"[AUDIO] UnPaused on {gameObject.name} at {_currentTime}");
        }
    }

    public void Seek(float time_in_seconds)
    {
        if (monoSpatialClip != null && !mono_source.isPlaying) 
        {
            mono_source.time = time_in_seconds;
            stereo_source.time = time_in_seconds;
            mono_source.Play();
            stereo_source.Play();
            if (showDebug) Debug.Log("[AUDIO] Played at "+_currentTime);
        }
    }
}
