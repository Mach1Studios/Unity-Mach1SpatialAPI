using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTrigger : MonoBehaviour
{
    public M1SpatialDecode spatialMix;

    // Detects if the Enter key was pressed
    void OnGUI()
    {
        if (Event.current.Equals(Event.KeyboardEvent("space")))
        {
            if (spatialMix != null)
            {
                if (spatialMix.IsPlaying())
                {
                    Debug.Log("[AUDIO] Spatial Mix Stopped");
                    spatialMix.StopAudio();
                } else
                {
                    Debug.Log("[AUDIO] Spatial Mix Playing");
                    spatialMix.PlayAudio();
                }
            }        
        }

        if (Event.current.Equals(Event.KeyboardEvent("return")))
        {
            if (spatialMix != null)
            {
                if (spatialMix.IsPlaying())
                {
                    Debug.Log("[AUDIO] Spatial Mix Stopped");
                    spatialMix.StopAudio();
                }
            }
        }
    }
}
