
#ifndef __CUSTOM_ACES2__
#define __CUSTOM_ACES2__

/*
    ACES 2.0 integration helper for Unity (URP/HDRP HLSL include).

    Strategy
    --------
    - Prefer *exact* ACES 2 (RRT+ODT v2) by sampling a 3D LUT exported from an ACES 2 / OCIO config.
      The LUT should be authored for input space = ACEScg (AP1, scene-linear, D60), output space = display (e.g., sRGB/Rec.709 ODT v2).
    - If no LUT is provided, fall back to the legacy ACES 1.0 tonemapper (if included) or to a Neutral Reinhard-ish curve.
      (The fallback is only a temporary preview and won't match ACES 2 reference.)
    
    Expected binding
    ----------------
    UNITY_DECLARE_TEX3D(_ACES2LUT);
    SamplerState sampler_ACES2LUT;
    float _LutSize;               // e.g., 33 or 65. Trilinear sampling needs exact size.
    float _LutContribution;       // 0..1 blend (1 = full ACES 2)
    float _UseACES2;              // >0.5 to enable
    float _ACES2_OutputIsSRGB;    // 1 if LUT outputs gamma encoded sRGB, 0 if linear (rare).

    Color spaces
    ------------
    - Unity linear working space here is assumed to be Linear sRGB (D65).
    - ACES 2 LUT input expects ACEScg (AP1, D60). We provide conversion helpers.

    How to generate a LUT (one-liner example with OCIO CLI)
    -------------------------------------------------------
    # Assuming you have an ACES 2 OCIO config (aces-2.0.0/config.ocio):
    # Scene-linear ACEScg -> Output - sRGB (ODT v2), 33^3 LUT
    ociobakelut --inputcolorspace ACEScg --outputcolorspace "sRGB - Display" --format spi3d --shaperspace lin --size 33 aces2_srgb_33.spi3d

    Then convert the .spi3d into a Unity Texture3D (via importer or small editor script).

    NOTE: For accuracy, always use a LUT generated from the official ACES 2 config matching your display.
*/

// ----------------------- Matrices for color space conversion -----------------------
// sRGB (D65) linear <-> XYZ (D65)
static const float3x3 sRGB_2_XYZ_D65 = float3x3(
    0.4124564, 0.3575761, 0.1804375,
    0.2126729, 0.7151522, 0.0721750,
    0.0193339, 0.1191920, 0.9503041
);

static const float3x3 XYZ_D65_2_sRGB = float3x3(
     3.2404542, -1.5371385, -0.4985314,
    -0.9692660,  1.8760108,  0.0415560,
     0.0556434, -0.2040259,  1.0572252
);

// ACEScg (AP1, D60) <-> XYZ (D60)
static const float3x3 AP1_2_XYZ_D60 = float3x3(
    0.6624541811, 0.1340042065, 0.1561876870,
    0.2722287168, 0.6740817658, 0.0536895174,
   -0.0055746495, 0.0040607335, 1.0103391003
);

static const float3x3 XYZ_D60_2_AP1 = float3x3(
    1.6410233797, -0.3248032942, -0.2364246952,
   -0.6636628587,  1.6153315917,  0.0167563477,
    0.0117218943, -0.0082844420,  0.9883948585
);

// Chromatic adaptation D65 <-> D60 via CAT (Bradford-like)
static const float3x3 D65_2_D60_CAT = float3x3(
    1.014466, -0.014353, -0.000113,
    0.000000,  1.000000,  0.000000,
    0.000000, -0.000000,  1.007936
);
static const float3x3 D60_2_D65_CAT = float3x3(
    0.985741,  0.014269,  0.000110,
    0.000000,  1.000000,  0.000000,
    0.000000,  0.000000,  0.992121
);

// sRGB (lin, D65) -> ACEScg (lin, D60)
float3 LinearSRGB_to_ACEScg(float3 c)
{
    float3 XYZd65 = mul(sRGB_2_XYZ_D65, c);
    float3 XYZd60 = mul(D65_2_D60_CAT, XYZd65);
    float3 ap1    = mul(XYZ_D60_2_AP1, XYZd60);
    return ap1;
}

// ACEScg (lin, D60) -> sRGB (lin, D65)
float3 ACEScg_to_LinearSRGB(float3 c)
{
    float3 XYZd60 = mul(AP1_2_XYZ_D60, c);
    float3 XYZd65 = mul(D60_2_D65_CAT, XYZd60);
    float3 srgb   = mul(XYZ_D65_2_sRGB, XYZd65);
    return srgb;
}

// ----------------------- Utility -----------------------
float3 SafeSaturate(float3 x) { return clamp(x, 0.0, 1.0); }
float  SafeSaturate1(float  x) { return clamp(x, 0.0, 1.0); }

// 3D LUT sampling (expects normalized [0..1] input, trilinear)
float3 SampleLUT3D(Texture3D lutTex, SamplerState lutSamp, float3 uvw, float lutSize)
{
    // Unity packs LUTs so exact addressing is required to hit texel centers for trilinear
    float oneOverSize = 1.0 / lutSize;
    float halfStep = 0.5 * oneOverSize;
    uvw = saturate(uvw);
    // Scale to [halfStep .. 1 - halfStep]
    uvw = uvw * (1.0 - oneOverSize) + halfStep;
    return lutTex.SampleLevel(lutSamp, uvw, 0).rgb;
}

// Simple neutral fallback curve (only used when no LUT provided)
float3 NeutralFallbackTonemap(float3 x)
{
    // Reinhard w/ slight shoulder
    float3 a = 1.0 + x;
    float3 y = x / a;
    return y;
}

// ----------------------- Public entry point -----------------------
// Applies ACES 2 via LUT if available. Input: linear sRGB (D65), Output: sRGB OETF-encoded if _ACES2_OutputIsSRGB=1 else linear.
// Applies ACES 2 via LUT if available. Input: linear sRGB (D65), Output: sRGB OETF-encoded if _ACES2_OutputIsSRGB=1 else linear.
float3 ApplyACES2_Tonemap(
    float3 linearSRGB,
    Texture3D _ACES2LUT,
    SamplerState sampler_ACES2LUT,
    float _LutSize,
    float _LutContribution,
    float _ACES2_OutputIsSRGB,
    float _UseACES2)
{
    if (_UseACES2 < 0.5)
        return linearSRGB;

    float3 c_ap1 = LinearSRGB_to_ACEScg(max(linearSRGB, 0.0));

    // ACEScg scene-linear assumed normalized into [0..1] by the baked LUT.
    float3 lutIn = saturate(c_ap1);

    float3 aces2Out = SampleLUT3D(_ACES2LUT, sampler_ACES2LUT, lutIn, _LutSize);

    // If LUT outputs sRGB gamma, convert to linear for blending; otherwise assume linear already.
    if (_ACES2_OutputIsSRGB <= 0.5)
    {
        // Already linear output (preferred)
        float3 linearOut = aces2Out;
        return lerp(linearSRGB, linearOut, SafeSaturate1(_LutContribution));
    }
    else
    {
        // LUT output is sRGB OETF; convert to linear for blend.
        float3 gammaOut = aces2Out;
        float3 linearOutTm = pow(max(gammaOut, 0.0), 2.2); // approximate inverse OETF
        return lerp(linearSRGB, linearOutTm, SafeSaturate1(_LutContribution));
    }
}


// Convenience wrapper when calling from a full-screen pass expecting linear in/linear out:
float3 Tonemap_ACES2_Linear(float3 linearSRGB,
                            Texture3D _ACES2LUT, SamplerState sampler_ACES2LUT,
                            float _LutSize, float _LutContribution, float _ACES2_OutputIsSRGB, float _UseACES2)
{
    return ApplyACES2_Tonemap(linearSRGB, _ACES2LUT, sampler_ACES2LUT, _LutSize, _LutContribution, _ACES2_OutputIsSRGB, _UseACES2);
}

#endif // __CUSTOM_ACES2__
