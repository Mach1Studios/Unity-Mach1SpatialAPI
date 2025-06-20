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
    public bool playOnAwake = false;
    [Range(0.0f, 1.0f)] public float outputGain = 1.0f;

    [Header("Debug Inspector:")]
    public float _currentTime;

    [Header("Override Audio Sources (Optional):")]
    [SerializeField] private AudioSource mono_source;
    [Range(0.0f, 1.0f)][SerializeField] private float mono_spatialBlend = 1.0f;
    [SerializeField] private AudioSource stereo_source;
    [Range(0.0f, 1.0f)][SerializeField] private float stereo_spatialBlend = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        if (mono_source == null)
        {
            mono_source = new GameObject("MonoSource").AddComponent<AudioSource>();
            if (showDebug) Debug.Log($"[AUDIO] Spawned MonoSource {gameObject.name} at {_currentTime}");
        }
        if (stereo_source == null)
        {
            stereo_source = new GameObject("StereoSource").AddComponent<AudioSource>();
            if (showDebug) Debug.Log($"[AUDIO] Spawned StereoSource {gameObject.name} at {_currentTime}");
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
        mono_source.spatialBlend = mono_spatialBlend;
        mono_source.playOnAwake = playOnAwake;
        mono_source.loop = isLooping;
        // stereo settings
        stereo_source.spatialize = false;
        stereo_source.spatialBlend = stereo_spatialBlend;
        stereo_source.playOnAwake = playOnAwake;
        stereo_source.loop = isLooping;
    }

    // Update is called once per frame
    void Update()
    {
        if (mono_source != null)
        {
            _currentTime = mono_source.time;
            mono_source.volume = outputGain;
            mono_source.spatialBlend = mono_spatialBlend; 
        }
        if (stereo_source != null)
        {
            stereo_source.volume = outputGain;
            stereo_source.spatialBlend = stereo_spatialBlend;
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
