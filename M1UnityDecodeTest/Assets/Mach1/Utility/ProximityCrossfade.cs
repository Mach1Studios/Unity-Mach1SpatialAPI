using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProximityCrossfade : MonoBehaviour
{
    [SerializeField] private GameObject _referenceObject;
    [SerializeField] private M1SpatialDecode _spatialMix;
    [SerializeField] private AudioSource _proximityMix;
    private AudioListener _audioListener;
    public AnimationCurve crossfadeCurve;
    public bool useGainMultiplierOnDistantSpatialMix = true;
    public bool showDebug = false;
    
    private float _distance;
    private float _vol_proximity;
    private float _vol_distant;

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

    void Start()
    {
        _audioListener = GameObject.FindObjectOfType<AudioListener>();
        if (crossfadeCurve == null )
        {
            crossfadeCurve = generateCurve(10);
        }
    }

    void Update()
    {
        _distance = Vector3.Distance(_audioListener.transform.position, _referenceObject.transform.position);
        
        _vol_proximity = crossfadeCurve.Evaluate(_distance);
        if (_vol_proximity < 0) _vol_proximity = 0.0f;
        if (_vol_proximity > 1.0) _vol_proximity = 1.0f;
        _vol_distant = _vol_proximity - 1.0f; // inverse
        if (_vol_distant < 0) _vol_distant = 0.0f;
        if (_vol_distant > 1.0) _vol_distant = 1.0f;

        // set audio gain for both mixes relative to the Curve
        _proximityMix.volume = _vol_proximity;

        // apply inverse multiplier on spatial mix if `useGainMultiplierOnDistantSpatialMix`
        if (useGainMultiplierOnDistantSpatialMix) 
        {
            _spatialMix.setoutputGainMultiplier(_vol_distant);
            if (showDebug) Debug.Log("[AUDIO] Prox Vol = " + _vol_proximity + ", Dist Vol = " + _vol_distant + ", with Distance = " + _distance);
        } else {
            if (showDebug) Debug.Log("[AUDIO] Prox Vol = " + _vol_proximity + ", with Distance = " + _distance);
        }
    }
}
