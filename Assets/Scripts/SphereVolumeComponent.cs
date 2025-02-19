using System;
using UnityEngine.Rendering;

[Serializable]
[VolumeComponentMenu("Custom/SphereVolumeComponent")]
public class SphereVolumeComponent : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f, true);
    public bool IsActive() => intensity.value > 0f;
}