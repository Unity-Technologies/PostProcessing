using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class AmbientOcclusion
    {
        // Unity sorts enums by value in the editor (and doesn't handle same-values enums very well
        // so we won't use enum values as sample counts this time
        public enum Quality
        {
            Lowest,
            Low,
            Medium,
            High,
            Ultra
        }

        [Tooltip("Enables ambient occlusion.")]
        public bool enabled = false;

        [Range(0f, 4f), Tooltip("Degree of darkness produced by the effect.")]
        public float intensity = 0.5f;

        [Tooltip("Radius of sample points, which affects extent of darkened areas.")]
        public float radius = 0.25f;

        [Tooltip("Number of sample points, which affects quality and performance. Lowest, Low & Medium passes are downsampled. High and Ultra are not and should only be used on high-end hardware.")]
        public Quality quality = Quality.Medium;

        [Tooltip("Only affects ambient lighting. This mode is only available with the Deferred rendering path and HDR rendering. Objects rendered with the Forward rendering path won't get any ambient occlusion.")]
        public bool ambientOnly = false;

        readonly RenderTargetIdentifier[] m_MRT =
        {
            BuiltinRenderTextureType.GBuffer0, // Albedo, Occ
            BuiltinRenderTextureType.CameraTarget // Ambient
        };

        readonly int[] m_SampleCount = { 4, 6, 10, 8, 12 };

        enum Pass
        {
            OcclusionEstimationForward,
            OcclusionEstimationDeferred,
            HorizontalBlurForward,
            HorizontalBlurDeferred,
            VerticalBlur,
            CompositionForward,
            CompositionDeferred
        }

        internal DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
        }

        internal bool IsAmbientOnly(PostProcessRenderContext context)
        {
            var camera = context.camera;
            return ambientOnly
                && camera.actualRenderingPath == RenderingPath.DeferredShading
                && camera.allowHDR;
        }

        internal bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled
                && intensity > 0f
                && !RuntimeUtilities.scriptableRenderPipelineActive;
        }

        PropertySheet PreRender(PostProcessRenderContext context, int occlusionSource)
        {
            radius = Mathf.Max(radius, 1e-4f);
            var cmd = context.command;

            // Material setup
            // Always use a quater-res AO buffer unless High/Ultra quality is set.
            bool downsampling = (int)quality < (int)Quality.High;
            float px = intensity;
            float py = radius;
            float pz = downsampling ? 0.5f : 1f;
            float pw = m_SampleCount[(int)quality];

            var sheet = context.propertySheets.Get(context.resources.shaders.ambientOcclusion);
            sheet.ClearKeywords();
            sheet.properties.SetVector(ShaderIDs.AOParams, new Vector4(px, py, pz, pw));

            // In forward fog is applied at the object level in the grometry pass so we need to
            // apply it to AO as well or it'll drawn on top of the fog effect.
            // Not needed in Deferred.
            if (context.camera.actualRenderingPath == RenderingPath.Forward && RenderSettings.fog)
            {
                sheet.properties.SetVector(ShaderIDs.FogParams, new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));

                switch (RenderSettings.fogMode)
                {
                    case FogMode.Linear:
                        sheet.EnableKeyword("FOG_LINEAR");
                        break;
                    case FogMode.Exponential:
                        sheet.EnableKeyword("FOG_EXP");
                        break;
                    case FogMode.ExponentialSquared:
                        sheet.EnableKeyword("FOG_EXP2");
                        break;
                }
            }

            // Texture setup
            int tw = context.width;
            int th = context.height;
            int ts = downsampling ? 2 : 1;
            const RenderTextureFormat kFormat = RenderTextureFormat.ARGB32;
            const RenderTextureReadWrite kRWMode = RenderTextureReadWrite.Linear;
            const FilterMode kFilter = FilterMode.Bilinear;

            // AO buffer
            var rtMask = ShaderIDs.OcclusionTexture1;
            cmd.GetTemporaryRT(rtMask, tw / ts, th / ts, 0, kFilter, kFormat, kRWMode);

            // AO estimation
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, rtMask, sheet, (int)Pass.OcclusionEstimationForward + occlusionSource);

            // Blur buffer
            var rtBlur = ShaderIDs.OcclusionTexture2;

            // Separable blur (horizontal pass)
            cmd.GetTemporaryRT(rtBlur, tw, th, 0, kFilter, kFormat, kRWMode);
            cmd.BlitFullscreenTriangle(rtMask, rtBlur, sheet, (int)Pass.HorizontalBlurForward + occlusionSource);
            cmd.ReleaseTemporaryRT(rtMask);

            // Separable blur (vertical pass)
            rtMask = ShaderIDs.OcclusionTexture;
            cmd.GetTemporaryRT(rtMask, tw, th, 0, kFilter, kFormat, kRWMode);
            cmd.BlitFullscreenTriangle(rtBlur, rtMask, sheet, (int)Pass.VerticalBlur);
            cmd.ReleaseTemporaryRT(rtBlur);

            return sheet;
        }

        internal void RenderAfterOpaque(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion");
            var sheet = PreRender(context, 0); // Forward
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.CompositionForward);
            cmd.EndSample("Ambient Occlusion");
        }

        internal void RenderAmbientOnly(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion");
            var sheet = PreRender(context, 1); // Deferred
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_MRT, BuiltinRenderTextureType.CameraTarget, sheet, (int)Pass.CompositionDeferred);
            cmd.EndSample("Ambient Occlusion");
        }
    }
}
