//  Mach1 SDK
//  Copyright © 2017 Mach1. All rights reserved.
//

using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.IO;

public class M1Base : MonoBehaviour
{
    public AudioMixerGroup m1SpatialAudioMixerGroup;

    [Header("Asset Source Settings")]
    public bool isFromAssets = true;
    public AudioClip[] audioClipMain;

    public string externalAudioPath = "file:///";
    public string[] externalAudioFilenameMain;

    [Header("Asset Load/Play Settings")]
    public bool autoPlay = false;
    public bool isLoop = false;
    public bool loadAudioOnStart = true;
    private bool isPlaying = false;

    [Header("Attenuation Settings")]
    public bool useAttenuation = false;
    public AnimationCurve attenuationCurve;
   
    private AudioSource[] audioSourceMain;

    private int MAX_SOUNDS_PER_CHANNEL;
    private Matrix4x4 matInternal;

    [Header("Point / Plane Setting")]
    public bool usePlaneCalculation = false;

    [Header("Advanced Settings")]
    public bool muteWhenInsideObject = false;
    public bool muteWhenOutsideObject = false;

    public bool useYawForRotation = true;
    public bool usePitchForRotation = true;
    public bool useRollForRotation = true;

    public bool drawHelpers = false;
    public bool debug = false;

    [Header("Experimental BlendMode")]
    public bool useBlendMode = false;
    public AudioClip[] audioClipBlend;
    public string[] externalAudioFilenameBlend;
    public AnimationCurve attenuationCurveBlendMode;
    public bool ignoreTopBottom = true;
    private AudioSource[] audioSourceBlend;

    private float[] coeffs;
    private float[] coeffsInterior;

    private AudioListener audiolistener;
    private bool needToPlay;

    protected Mach1.Mach1DecodePositional m1Positional = new Mach1.Mach1DecodePositional();

    static Mach1.Mach1Point3D ConvertToMach1Point3D(Vector3 vec)
    {
        return new Mach1.Mach1Point3D(vec.x, vec.y, vec.z);
    }

    static Mach1.Mach1Point4D ConvertToMach1Point4D(Vector4 vec)
    {
        return new Mach1.Mach1Point4D(vec.x, vec.y, vec.z, vec.w);
    }

    static Mach1.Mach1Point4D ConvertToMach1Point4D(Quaternion quat)
    {
        return new Mach1.Mach1Point4D(quat.x, quat.y, quat.z, quat.w);
    }

    static Vector3 ConvertFromMach1Point3D(Mach1.Mach1Point3D pnt)
    {
        return new Vector3(pnt.x, pnt.y, pnt.z);
    }

    AnimationCurve generateCurve(float length)
    {
        Keyframe[] keyframes = new Keyframe[3];
        for (int i = 0; i < keyframes.Length; i++)
        {
            keyframes[i] = new Keyframe(i * length / 2.0f, 1 - 1.0f * i / (keyframes.Length - 1));
        }

        AnimationCurve curve = new AnimationCurve(keyframes);
        for (int i = 0; i < keyframes.Length; i++)
        {
            curve.SmoothTangents(i, 0);
        }
        return curve;
    }

    public M1Base()
    {
        coeffs = new float[18];
        coeffsInterior = new float[18];

        m1Positional.setPlatformType(Mach1.Mach1PlatformType.Mach1PlatformUnity);
    }

    protected void InitComponents(int MAX_SOUNDS_PER_CHANNEL)
    {
        this.MAX_SOUNDS_PER_CHANNEL = MAX_SOUNDS_PER_CHANNEL;

        // Falloff
        attenuationCurve = generateCurve(10);
        attenuationCurveBlendMode = generateCurve(1);

        // Init filenames
        externalAudioFilenameMain = new string[MAX_SOUNDS_PER_CHANNEL];
        externalAudioFilenameBlend = new string[MAX_SOUNDS_PER_CHANNEL];
        for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL; i++)
        {
            externalAudioFilenameMain[i] = (i + 1) + ".wav";
            externalAudioFilenameBlend[i] = (i + 1) + ".wav";
        }

        // audioClip
        audioClipMain = new AudioClip[MAX_SOUNDS_PER_CHANNEL];
        audioClipBlend = new AudioClip[MAX_SOUNDS_PER_CHANNEL];
    }

    void Awake()
    {
    }

    void Start()
    {
        if (loadAudioOnStart)
        {
            LoadAudioData();
        }
        attachAudioListener();
    }

    public void LoadAudioData()
    {
        // Sounds
        audioSourceMain = new AudioSource[MAX_SOUNDS_PER_CHANNEL * 2];

        for (int i = 0; i < Mathf.Max(externalAudioFilenameMain.Length, audioClipMain.Length); i++)
        {
            StartCoroutine(LoadAudio(Path.Combine(externalAudioPath, i < externalAudioFilenameMain.Length ? externalAudioFilenameMain[i] : ""), false, i, isFromAssets));
        }

        if (useBlendMode)
        {
            // Sounds
            audioSourceBlend = new AudioSource[MAX_SOUNDS_PER_CHANNEL * 2];

            for (int i = 0; i < Mathf.Max(externalAudioFilenameBlend.Length, audioClipBlend.Length); i++)
            {
                StartCoroutine(LoadAudio(Path.Combine(externalAudioPath, i < externalAudioFilenameBlend.Length ? externalAudioFilenameBlend[i] : ""), true, i, isFromAssets));
            }
        }

        isPlaying = false;
    }

    public void UnloadAudioData()
    {
        if (isFromAssets)
        {
            for (int i = 0; i < audioClipMain.Length; i++)
            {
                audioClipMain[i].UnloadAudioData();
            }

            for (int i = 0; i < audioClipBlend.Length; i++)
            {
                audioClipBlend[i].UnloadAudioData();
            }
        }
        else
        {
            for (int i = 0; i < audioClipMain.Length; i++)
            {
                AudioClip.Destroy(audioClipMain[i]);
            }
            for (int i = 0; i < audioClipBlend.Length; i++)
            {
                AudioClip.Destroy(audioClipBlend[i]);
            }
        }

        isPlaying = false;
    }

    public void attachAudioListener() 
    {
        audiolistener = GameObject.FindObjectOfType<AudioListener>();
    }

    // Helper function to add audio clip to source, and add this to scene
    AudioSource AddAudio(AudioClip clip, bool loop, bool playAwake, float vol)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.hideFlags = HideFlags.HideInInspector;
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = playAwake;
        source.volume = vol;
        source.priority = 0;
        source.spatialize = false;
        source.outputAudioMixerGroup = m1SpatialAudioMixerGroup;
        return source;
    }

    // Draw gizmo in editor (you may display this also in game windows if set "Gizmo" button)
    void OnDrawGizmos()
    {
        if (drawHelpers)
        {
            //Gizmos.DrawIcon(transform.position, "sound_icon.png", true);

            Gizmos.color = Color.gray;
            Gizmos.matrix = gameObject.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1));

            Gizmos.color = Color.yellow;
            Gizmos.matrix = matInternal;
            Gizmos.DrawWireCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1));

            float radius = 0.1f;

            Vector3[] points = new Vector3[8] {
                new Vector3(-1, 1, 1),
                new Vector3(1, 1, 1),
                new Vector3(-1, -1, 1),
                new Vector3(1, -1, 1),
                new Vector3(-1, 1, -1),
                new Vector3(1, 1, -1),
                new Vector3(-1, -1, -1),
                new Vector3(1, -1, -1),
            };

            for (int i = 0; i < 8; i++)
            {
                Vector3 point = new Vector3(points[i].x, points[i].z, points[i].y); // convert to glm 

                Gizmos.color = Color.red;
                Gizmos.matrix = matInternal * (Matrix4x4.Translate(new Vector3(-radius, 0, 0)) * Matrix4x4.Translate(point * 0.5f));
                Gizmos.DrawSphere(new Vector3(0, 0, 0), radius * coeffs[2 * i]);

                Gizmos.color = Color.blue;
                Gizmos.matrix = matInternal * (Matrix4x4.Translate(new Vector3(radius, 0, 0)) * Matrix4x4.Translate(point * 0.5f));
                Gizmos.DrawSphere(new Vector3(0, 0, 0), radius * coeffs[2 * i + 1]);

                Gizmos.DrawIcon((matInternal * Matrix4x4.Translate(point * 0.5f)).MultiplyPoint(new Vector4(0, -2 * radius, 0)), "sound_icon_" + i + ".png", true);
            }

        }
    }

    string GetStreamingAssetsPath()
    {
        string path;
#if UNITY_EDITOR
        path = "file://" + Application.dataPath + "/StreamingAssets";
#elif UNITY_ANDROID
     path = "jar:file://"+ Application.dataPath + "!/assets";
#elif UNITY_IOS
     path = "file://" + Application.dataPath + "/Raw";
#else
     //Desktop (Mac OS or Windows)
     path = "file://"+ Application.dataPath + "/StreamingAssets";
#endif
        return path;
    }

    // Load audio
    IEnumerator LoadAudio(string url, bool room, int n, bool isFromAssets)
    {
        AudioClip clip = null;
        

        if (isFromAssets)
        {   
            if (!room)
            {
                clip = audioClipMain[n];// Resources.Load< AudioClip>(url);
            }
            else
            {
                clip = audioClipBlend[n];
            }

            if(clip != null)
            { 
                clip.LoadAudioData();
            }
        }
        else
        {
            url = url.Replace("$CURDIR", "file:///" + Directory.GetCurrentDirectory());
            url = url.Replace("$STREAMINGASSETS", GetStreamingAssetsPath());

            //Debug.Log ("load audio : " + url);

            WWW www = new WWW(url);
            yield return www;
            if (www.error == null)
            {
                clip = www.GetAudioClip(false, false);
            }
            else
            {
                Debug.Log("WWW Error: " + www.error + " (" + url + ")");
            }
        }

        if (clip != null)
        {
            if (!room && audioSourceMain != null && audioSourceMain.Length > n * 2 + 1)
            {
                audioSourceMain[n * 2] = AddAudio(clip, isLoop, true, 1.0f);
                audioSourceMain[n * 2].panStereo = -1;

                audioSourceMain[n * 2 + 1] = AddAudio(clip, isLoop, true, 1.0f);
                audioSourceMain[n * 2 + 1].panStereo = 1;
            }
            else if (audioSourceBlend != null && audioSourceBlend.Length > n * 2 + 1)
            {
                audioSourceBlend[n * 2] = AddAudio(clip, isLoop, true, 1.0f);
                audioSourceBlend[n * 2].panStereo = -1;

                audioSourceBlend[n * 2 + 1] = AddAudio(clip, isLoop, true, 1.0f);
                audioSourceBlend[n * 2 + 1].panStereo = 1;
            }
        }

        yield break;
    }

    public bool IsReady()
    {
        bool isLoadedMain = true;
        if(audioSourceMain != null)
        {
            for (int i = 0; i < audioSourceMain.Length; i++)
            {
                if (!audioSourceMain[i] || !audioSourceMain[i].clip || audioSourceMain[i].clip.loadState != AudioDataLoadState.Loaded)
                {
                    isLoadedMain = false;
                    break;
                }
            }
        }
        else isLoadedMain = false;


        bool isLoadedBlend = true;
        if (useBlendMode)
        {
            if (audioSourceBlend != null)
            {
                for (int i = 0; i < audioSourceBlend.Length; i++)
                {
                    if (!audioSourceBlend[i] || !audioSourceBlend[i].clip || audioSourceBlend[i].clip.loadState != AudioDataLoadState.Loaded)
                    {
                        isLoadedBlend = false;
                        break;
                    }
                }
            }
            else isLoadedBlend = false;
        }

        return isLoadedMain && (useBlendMode ? isLoadedBlend : true);
    }

    public void PlayAudio()
    {
        needToPlay = true;
    }

    public void StopAudio()
    {
        if (IsReady())
        {
            if (audioSourceMain != null)
            {
                for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL * 2; i++)
                {
                    audioSourceMain[i].Stop();
                }
            }

            if (useBlendMode && audioSourceBlend != null)
            {
                for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL * 2; i++)
                {
                    audioSourceBlend[i].Stop();
                }
            }
        }
    }

    public void PauseAudio()
    {
        if (IsReady())
        {
            if (audioSourceMain != null)
            {
                for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL * 2; i++)
                {
                    audioSourceMain[i].Pause();
                }
            }

            if (useBlendMode && audioSourceBlend != null)
            {
                for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL * 2; i++)
                {
                    audioSourceBlend[i].Pause();
                }
            }
        }
    }

    public void ResumeAudio()
    {
        if (IsReady())
        {
            if (audioSourceMain != null)
            {
                for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL * 2; i++)
                {
                    audioSourceMain[i].UnPause();
                }
            }

            if (useBlendMode && audioSourceBlend != null)
            {
                for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL * 2; i++)
                {
                    audioSourceBlend[i].UnPause();
                }
            }
        }
    }

    public void Seek(float timeInSeconds)
    {
        if (audioSourceBlend != null)
            foreach (AudioSource source in audioSourceBlend)
                if (source != null)
                    source.time = timeInSeconds;

        if (audioSourceMain != null)
            foreach (AudioSource source in audioSourceMain)
                if (source != null)
                    source.time = timeInSeconds;
    }

    public AudioSource[] GetAudioSourceMain()
    {
        return audioSourceMain;
    }

    public AudioSource[] GetAudioSourceBlend()
    {
        return audioSourceBlend;
    }

    public float GetPosition()
    {
        if (audioSourceBlend != null && audioSourceBlend.Length > 0) return audioSourceBlend[0].time;
        else if (audioSourceMain != null && audioSourceMain.Length > 0) return audioSourceMain[0].time;
        return 0;
    }

    public float GetDuration()
    {
        if (audioSourceBlend != null && audioSourceBlend.Length > 0) return audioSourceBlend[0].clip.length;
        else if (audioSourceMain != null && audioSourceMain.Length > 0) return audioSourceMain[0].clip.length;
        return 0;
    }

    public bool IsPlaying()
    {
        return (audioSourceBlend != null && audioSourceBlend.Length > 0 && audioSourceBlend[0].isPlaying) || (audioSourceMain != null && audioSourceMain[0].isPlaying);
    }


    public string ToStringFormat(Vector3 v)
    {
        string fmt = "0.0000";
        return "( " + v.x.ToString(fmt) + ", " + v.y.ToString(fmt) + ", " + v.z.ToString(fmt) + " )";
    }

    public string ToStringFormat(Quaternion q)
    {
        string fmt = "0.0000";
        return "( " + q.x.ToString(fmt) + ", " + q.y.ToString(fmt) + ", " + q.z.ToString(fmt) + ", " + q.w.ToString(fmt) + " )";
    }




    public static Vector3 QuaternionToEuler(Quaternion q)
    {
        Vector3 euler;

        // if the input quaternion is normalized, this is exactly one. Otherwise, this acts as a correction factor for the quaternion's not-normalizedness
        float unit = (q.x * q.x) + (q.y * q.y) + (q.z * q.z) + (q.w * q.w);

        // this will have a magnitude of 0.5 or greater if and only if this is a singularity case
        float test = q.x * q.w - q.y * q.z;

        if (test > 0.499999f * unit) // singularity at north pole
        {
            euler.y = -Mathf.PI / 2;
            euler.x = 2f * Mathf.Atan2(q.y, q.x);
            euler.z = 0;
        }
        else if (test < -0.499999f * unit) // singularity at south pole
        {
            euler.y = Mathf.PI / 2;
            euler.x = -2f * Mathf.Atan2(q.y, q.x);
            euler.z = 0;
        }
        else // no singularity - this is the majority of cases
        {
            euler.y = -Mathf.Asin(2f * (q.w * q.x - q.y * q.z));
            euler.x = Mathf.Atan2(2f * q.w * q.y + 2f * q.z * q.x, 1 - 2f * (q.x * q.x + q.y * q.y));
            euler.z = Mathf.Atan2(2f * q.w * q.z + 2f * q.x * q.y, 1 - 2f * (q.z * q.z + q.x * q.x));
        }

        // all the math so far has been done in radians. Before returning, we convert to degrees...
        euler *= Mathf.Rad2Deg;

        //...and then ensure the degree values are between 0 and 360
        euler.x %= 360;
        euler.y %= 360;
        euler.z %= 360;

        return euler;
    }

    public static Quaternion EulerToQuaternion(Vector3 euler)
    {
        float xOver2 = -euler.y * Mathf.Deg2Rad * 0.5f;
        float yOver2 = euler.x * Mathf.Deg2Rad * 0.5f;
        float zOver2 = euler.z * Mathf.Deg2Rad * 0.5f;

        float sinXOver2 = Mathf.Sin(xOver2);
        float cosXOver2 = Mathf.Cos(xOver2);
        float sinYOver2 = Mathf.Sin(yOver2);
        float cosYOver2 = Mathf.Cos(yOver2);
        float sinZOver2 = Mathf.Sin(zOver2);
        float cosZOver2 = Mathf.Cos(zOver2);

        Quaternion result;
        result.x = cosYOver2 * sinXOver2 * cosZOver2 + sinYOver2 * cosXOver2 * sinZOver2;
        result.y = sinYOver2 * cosXOver2 * cosZOver2 - cosYOver2 * sinXOver2 * sinZOver2;
        result.z = cosYOver2 * cosXOver2 * sinZOver2 - sinYOver2 * sinXOver2 * cosZOver2;
        result.w = cosYOver2 * cosXOver2 * cosZOver2 + sinYOver2 * sinXOver2 * sinZOver2;

        return result;
    }


    // Update is called once per frame
    void Update()
    {
        if (audiolistener == null)
        {
            Debug.LogError("Mach1: cannot find AudioListener!");
            attachAudioListener();
            return;
        }

        if (IsReady())
        {
            if ((autoPlay || needToPlay) && !isPlaying)
            {
                for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL * 2; i++)
                {
                    audioSourceMain[i].Play();
                }

                if (useBlendMode)
                {
                    for (int i = 0; i < MAX_SOUNDS_PER_CHANNEL * 2; i++)
                    {
                        audioSourceBlend[i].Play();
                    }
                }

                needToPlay = false;
                isPlaying = true;
            }

            // In order to use values set in Unity's object inspector, we have to put them into an
            // M1 Positional library instance. Here's an example:
            // /*
            m1Positional.setUseBlendMode(useBlendMode);
            m1Positional.setIgnoreTopBottom(ignoreTopBottom);
            m1Positional.setMuteWhenOutsideObject(muteWhenOutsideObject);
            m1Positional.setMuteWhenInsideObject(muteWhenInsideObject);
            m1Positional.setUseAttenuation(useAttenuation);
            m1Positional.setUsePlaneCalculation(usePlaneCalculation);
            m1Positional.setUseYawForRotation(useYawForRotation);
            m1Positional.setUsePitchForRotation(usePitchForRotation);
            m1Positional.setUseRollForRotation(useRollForRotation);
            m1Positional.setListenerPosition(ConvertToMach1Point3D(audiolistener.transform.position));
            
            m1Positional.setListenerRotationQuat(ConvertToMach1Point4D(audiolistener.transform.rotation));
            //m1Positional.setListenerRotation(ConvertToMach1Point3D(audiolistener.transform.rotation.eulerAngles));

            m1Positional.setDecoderAlgoPosition(ConvertToMach1Point3D(gameObject.transform.position));

            m1Positional.setDecoderAlgoRotationQuat(ConvertToMach1Point4D(gameObject.transform.rotation));
            //m1Positional.setDecoderAlgoRotation(ConvertToMach1Point3D(gameObject.transform.rotation.eulerAngles));

            m1Positional.setDecoderAlgoScale(ConvertToMach1Point3D(gameObject.transform.lossyScale));
            m1Positional.evaluatePositionResults();

            if (useAttenuation)
            {
                m1Positional.setAttenuationCurve(attenuationCurve.Evaluate(m1Positional.getDist()));
                m1Positional.setAttenuationCurveBlendMode(attenuationCurveBlendMode.Evaluate(m1Positional.getDist()));
            }

            m1Positional.getCoefficients(ref coeffs);
            for (int i = 0; i < audioSourceMain.Length; i++)
            {
                audioSourceMain[i].volume = coeffs[i];
            }

            if (useBlendMode)
            {
                m1Positional.getCoefficientsInterior(ref coeffsInterior);
                for (int i = 0; i < audioSourceBlend.Length; i++)
                {
                    audioSourceBlend[i].volume = coeffsInterior[i];
                }
            }

            if (debug)
            {
                Mach1.Mach1Point3D anglesCube = m1Positional.getPositionalRotation();
                matInternal = Matrix4x4.TRS(audiolistener.transform.position, Quaternion.Euler(anglesCube.x, anglesCube.y, anglesCube.z), new Vector3(1, 1, 1));

                Mach1.Mach1Point3D anglesInternal = m1Positional.getCurrentAngleInternal();
                Debug.Log("M1Obj Euler Rotation Angles: " + anglesInternal.x + " , " + anglesInternal.y + " , " + anglesInternal.z);
                Debug.Log("M1Obj Distance: " + m1Positional.getDist());

                string str = "Returned Coefficients: ";
                for (int i = 0; i < audioSourceMain.Length; i++)
                {
                    str += string.Format("{0:0.000}, ", audioSourceMain[i].volume);
                }
                if (useBlendMode)
                {
                    str += " , " + "Returned Coefficients Internal (BlendMode): ";
                    for (int i = 0; i < audioSourceBlend.Length; i++)
                    {
                        str += string.Format("{0:0.000}, ", audioSourceBlend[i].volume);
                    }
                }
                Debug.Log(str);
            }

            // Mach1.Mach1Point3D angles = m1Positional.getCoefficientsRotation();
            // Debug.Log("volumeWalls: " + coeffs + " , " + "volumeRoom" + coeffsInterior);
            // Debug.Log("d: " + dist + ", d2: " + m1Positional.getDist());

            if (drawHelpers) 
            {
                // Draw forward vector from audio listener
                Vector3 targetForward = audiolistener.transform.rotation * (Vector3.forward * 3);
                Debug.DrawLine(audiolistener.transform.position, audiolistener.transform.position + targetForward, Color.blue);

                // Draw direction from closest point to object
                Mach1.Mach1Point3D point = m1Positional.getClosestPointOnPlane();
                Debug.DrawLine(audiolistener.transform.position, new Vector3(point.x, point.y, point.z), Color.green);
            }
        }
    }
}