//  Mach1 SDK
//  Copyright © 2017 Mach1. All rights reserved.
//

//#define LEGACY_POSITIONAL

using UnityEngine;
using System.Collections;
using System.IO;

public class M1SpatialDecode : M1Base
{
    public M1SpatialDecode()
    {
        // TODO: Allow selectable usage of all Mach1DecodeMode
        InitComponents(8);
        m1Positional.setDecodeMode(Mach1.Mach1DecodeMode.M1DecodeSpatial_8);
    }
}
