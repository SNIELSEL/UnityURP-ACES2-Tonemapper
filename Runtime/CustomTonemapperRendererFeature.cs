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
                var stack = VolumeManager.instance.stack;
                var vol = stack.GetComponent<CustomTonemapper>();

                var mat = (vol != null && vol.materialOverride.value != null)
                    ? vol.materialOverride.value
                    : _defaultMat; // <- use this, not settings.tonemapperMaterial

                if (mat == null) return;

                bool inactive = vol == null || !vol.active
                             || vol.mode.value != CustomTonemapperMode.ACES2
                             || vol.aces2LUT.value == null;

                if (inactive)
                {
                    mat.SetFloat("_Contribution", 0f);
                    mat.SetFloat("_Aces2LutSize", 0f);
                    return; // skip scheduling passes when off
                }

                mat.SetTexture("_Aces2Lut", vol.aces2LUT.value);
                mat.SetFloat("_Aces2LutSize", Mathf.Max(2, vol.lutSize.value));
                mat.SetFloat("_Contribution", Mathf.Clamp01(vol.contribution.value));
                mat.SetFloat("_LutIsSRGB", vol.lutOutputIsSRGB.value ? 1f : 0f);

                var urp = frameData.Get<UniversalResourceData>();
                var cameraColor = urp.activeColorTexture;

                var tmpDesc = rg.GetTextureDesc(cameraColor);
                tmpDesc.name = "CustomTonemapper_TempColor";
                var tempColor = rg.CreateTexture(in tmpDesc);

                using (var builder = rg.AddRasterRenderPass<PassData>(
                    "ACES2 Tonemap (Apply)", out var dataA, new ProfilingSampler("ACES2.Tonemap.Apply")))
                {
                    dataA.mat = mat;
                    dataA.matPass = _matPass;
                    dataA.src = cameraColor;
                    dataA.dst = tempColor;

                    builder.UseTexture(dataA.src, AccessFlags.Read);
                    builder.SetRenderAttachment(dataA.dst, 0);
                    builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, d.matPass);
                    });
                }

                using (var builder = rg.AddRasterRenderPass<PassData>(
                    "ACES2 Tonemap (CopyBack)", out var dataB, new ProfilingSampler("ACES2.Tonemap.CopyBack")))
                {
                    dataB.src = tempColor;
                    dataB.dst = cameraColor;

                    builder.UseTexture(dataB.src, AccessFlags.Read);
                    builder.SetRenderAttachment(dataB.dst, 0);
                    builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.src, Vector4.one, 0, false);
                    });
                }
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
