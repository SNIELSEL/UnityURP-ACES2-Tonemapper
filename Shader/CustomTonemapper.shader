Shader "Custom/TonemapperACES2"
{
    Properties
    {
        _Aces2Lut("ACES2 LUT", 3D) = "" {}
        _Aces2LutSize("LUT Size", Float) = 33
        _Contribution("LUT Contribution", Range(0,1)) = 1
        _LutIsSRGB("ACES2 LUT outputs sRGB", Float) = 1
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "TonemapACES2"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            //#define TEST_PASSTHROUGH 1   // Enable to debug full-screen pass

            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // 3D LUT
            TEXTURE3D(_Aces2Lut);
            SAMPLER(sampler_Aces2Lut);

            // Material parameters
            CBUFFER_START(UnityPerMaterial)
                float _Aces2LutSize;
                float _Contribution;
                float _LutIsSRGB; // 1 if LUT output is gamma/display (e.g. sRGB Display)
            CBUFFER_END

            // Avoid name conflict with Blit.hlsl
            struct TonemapVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TonemapVaryings Vert(uint id : SV_VertexID)
            {
                TonemapVaryings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(id);
                o.uv = GetFullScreenTriangleTexCoord(id);
                return o;
            }

            // Sample the 3D LUT
            float3 SampleLUT3D(float3 rgb, float size)
            {
                float3 uvw = saturate(rgb) * (size - 1.0) / size + 0.5 / size;
                return SAMPLE_TEXTURE3D(_Aces2Lut, sampler_Aces2Lut, uvw).rgb;
            }

            // Custom helper (renamed to avoid redefinition)
            float3 Aces_SRGBToLinear(float3 c)
            {
                return pow(saturate(c), 2.2);
            }

            float4 Frag(TonemapVaryings i) : SV_Target
            {
                // Source color from fullscreen blit
                float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.uv);

                // No-op if disabled or no valid LUT
                if (_Contribution <= 1e-5 || _Aces2LutSize < 2)
                    return src;



            #ifdef TEST_PASSTHROUGH
                // Debug: shows the input unmodified
                return src;
            #else
                float3 lin = src.rgb;                     // Input is linear HDR buffer
                float3 lutOut = SampleLUT3D(lin, _Aces2LutSize);

                // Convert LUT output back to linear if it's baked in display (sRGB) space
                if (_LutIsSRGB > 0.5)
                    lutOut = Aces_SRGBToLinear(lutOut);

                // Blend between original and tonemapped
                float3 outRgb = lerp(lin, lutOut, saturate(_Contribution));
                return float4(outRgb, src.a);
            #endif
            }

            ENDHLSL
        }
    }
}
