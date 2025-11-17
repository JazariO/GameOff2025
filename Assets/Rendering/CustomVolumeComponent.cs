using System;
using UnityEngine;
using UnityEngine.Rendering;


[Serializable]
public class CustomVolumeComponent : VolumeComponent
{
    [Header("General")]
    public ClampedIntParameter gridScale = new ClampedIntParameter(1, 1, 40);
    public Texture2DParameter thermalGradient = new Texture2DParameter(null);
}