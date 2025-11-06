// File: Assets/Custom/ACES2/Runtime/CustomTonemapperRendererFeature.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Custom.ACES2
{
    public class CustomTonemapperRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Default material if the Volume doesn't override it.")]
            public Material tonemapperMaterial;

            [Tooltip("Which pass index on the material to use.")]
            public int materialPassIndex = 0;

            [Tooltip("Recommended: AfterRenderingPostProcessing")]
            public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public Settings settings = new Settings();

        TonemapPass _pass;

        class TonemapPass : ScriptableRenderPass
        {
            readonly Material _defaultMat;
            readonly int _matPass;

            public TonemapPass(Material defaultMat, int passIndex, RenderPassEvent evt)
            {
                _defaultMat = defaultMat;
                _matPass = passIndex;
                renderPassEvent = evt;
            }

            // Legacy API stub (required by base):
            [System.Obsolete]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

            class PassData
            {
                public Material mat;
                public int matPass;
                public TextureHandle src;
                public TextureHandle dst;
            }

            public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
            {
                // Get Volume
                var stack = VolumeManager.instance.stack;
                var vol = stack.GetComponent<CustomTonemapper>();

                // Choose material (Volume override wins)
                var mat = (vol != null && vol.materialOverride.value != null)
                    ? vol.materialOverride.value
                    : _defaultMat;

                if (mat == null)
                    return; // nothing to do

                // If component inactive or not ACES2, set contribution 0 and bail
                if (vol == null || !vol.active || vol.mode.value != CustomTonemapperMode.ACES2)
                {
                    mat.SetFloat("_Contribution", 0f);
                    return;
                }

                // Push Volume → Material
                mat.SetTexture("_Aces2Lut", vol.aces2LUT.value);
                mat.SetFloat("_Aces2LutSize", Mathf.Max(2, vol.lutSize.value));
                mat.SetFloat("_Contribution", Mathf.Clamp01(vol.contribution.value));
                mat.SetFloat("_LutIsSRGB", vol.lutOutputIsSRGB.value ? 1f : 0f);

                // Fetch camera color from URP RG resources
                var urp = frameData.Get<UniversalResourceData>();
                var cameraColor = urp.activeColorTexture;

                // Temp texture to avoid read/write hazards
                var tmpDesc = rg.GetTextureDesc(cameraColor);
                tmpDesc.name = "CustomTonemapper_TempColor";
                var tempColor = rg.CreateTexture(in tmpDesc);

                // -------- Pass A: cameraColor -> tempColor (apply tonemap) --------
                using (var builder = rg.AddRasterRenderPass<PassData>(
                    "ACES2 Tonemap (Apply)",
                    out var dataA,
                    new ProfilingSampler("ACES2.Tonemap.Apply")))
                {
                    dataA.mat = mat;
                    dataA.matPass = _matPass;
                    dataA.src = cameraColor;
                    dataA.dst = tempColor;

                    builder.UseTexture(dataA.src, AccessFlags.Read);
                    builder.SetRenderAttachment(dataA.dst, 0);
                    // builder.AllowPassCulling(false); // optional

                    builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                    {
                        // Fullscreen blit with material
                        Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, d.matPass);
                    });
                } // <- builder disposed here

                // -------- Pass B: tempColor -> cameraColor (copy back) --------
                using (var builder = rg.AddRasterRenderPass<PassData>(
                    "ACES2 Tonemap (CopyBack)",
                    out var dataB,
                    new ProfilingSampler("ACES2.Tonemap.CopyBack")))
                {
                    dataB.src = tempColor;
                    dataB.dst = cameraColor;

                    builder.UseTexture(dataB.src, AccessFlags.Read);
                    builder.SetRenderAttachment(dataB.dst, 0);

                    builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                    {
                        // Raw copy, no material
                        Blitter.BlitTexture(ctx.cmd, d.src, Vector4.one, 0, false);
                    });
                } // <- builder disposed here BEFORE adding anything else
            }
        }

        public override void Create()
        {
            _pass = new TonemapPass(settings.tonemapperMaterial, settings.materialPassIndex, settings.injectionPoint);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pass);
        }
    }
}
