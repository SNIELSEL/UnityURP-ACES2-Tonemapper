// File: Assets/Custom/ACES2/Runtime/CustomTonemapper.cs
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Custom.ACES2
{
    public enum CustomTonemapperMode { None = 0, ACES2 = 1 }

    [Serializable]
    public sealed class TonemapModeParameter : VolumeParameter<CustomTonemapperMode>
    { public TonemapModeParameter(CustomTonemapperMode v, bool o = false) : base(v, o) { } }

    [Serializable]
    public sealed class MaterialParameter : VolumeParameter<Material>
    { public MaterialParameter(Material v, bool o = false) : base(v, o) { } }

    [Serializable, VolumeComponentMenu("Post-processing/Custom Tonemapper")]
    public sealed class CustomTonemapper : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("None or ACES2.")]
        public TonemapModeParameter mode = new TonemapModeParameter(CustomTonemapperMode.ACES2);

        [Tooltip("3D LUT baked from ACES 2 config.")]
        public Texture3DParameter aces2LUT = new Texture3DParameter(null);

        [Tooltip("LUT cube size (e.g. 33 or 65).")]
        public ClampedIntParameter lutSize = new ClampedIntParameter(33, 2, 256);

        [Tooltip("Blend between source and tonemapped output.")]
        public ClampedFloatParameter contribution = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Enable if the LUT output is display-referred (e.g. sRGB Display).")]
        public BoolParameter lutOutputIsSRGB = new BoolParameter(true);

        [Tooltip("(Optional) Override the tonemapper material used by the renderer feature.")]
        public MaterialParameter materialOverride = new MaterialParameter(null);

        public bool IsActive()
            => mode.value == CustomTonemapperMode.ACES2
               && aces2LUT.value != null
               && contribution.value > 0f;

        public bool IsTileCompatible() => true;
    }
}
