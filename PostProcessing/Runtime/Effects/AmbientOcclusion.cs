using System;

namespace UnityEngine.Rendering.PostProcessing
{
    public enum AmbientOcclusionMode
    {
        ScalableAmbientObscurance,
        MultiScaleVolumetricObscurance
    }

    public enum AmbientOcclusionQuality
    {
        Lowest,
        Low,
        Medium,
        High,
        Ultra
    }

    [Serializable]
    public sealed class AmbientOcclusionModeParameter : ParameterOverride<AmbientOcclusionMode> {}

    [Serializable]
    public sealed class AmbientOcclusionQualityParameter : ParameterOverride<AmbientOcclusionQuality> {}

    [Serializable]
    [PostProcess(typeof(AmbientOcclusionRenderer), "Unity/Ambient Occlusion")]
    public sealed class AmbientOcclusion : PostProcessEffectSettings
    {
        // Shared parameters
        [Tooltip("The ambient occlusion method to use. \"Modern\" is higher quality and faster on desktop & console platforms but requires compute shader support.")]
        public AmbientOcclusionModeParameter mode = new AmbientOcclusionModeParameter { value = AmbientOcclusionMode.MultiScaleVolumetricObscurance };

        [Range(0f, 4f), Tooltip("Degree of darkness added by ambient occlusion.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        [ColorUsage(false), Tooltip("Custom color to use for the ambient occlusion.")]
        public ColorParameter color = new ColorParameter { value = Color.black };

        [Tooltip("Only affects ambient lighting. This mode is only available with the Deferred rendering path and HDR rendering. Objects rendered with the Forward rendering path won't get any ambient occlusion.")]
        public BoolParameter ambientOnly = new BoolParameter { value = true };

        // MSVO-only parameters
        [Range(-8f, 0f)]
        public FloatParameter noiseFilterTolerance = new FloatParameter { value = 0f }; // Hidden

        [Range(-8f, -1f)]
        public FloatParameter blurTolerance = new FloatParameter { value = -4.6f }; // Hidden

        [Range(-12f, -1f)]
        public FloatParameter upsampleTolerance = new FloatParameter { value = -12f }; // Hidden

        [Range(1f, 10f), Tooltip("Modifies thickness of occluders. This increases dark areas but also introduces dark halo around objects.")]
        public FloatParameter thicknessModifier = new FloatParameter { value = 1f };

        // SAO-only parameters
        [Tooltip("Radius of sample points, which affects extent of darkened areas.")]
        public FloatParameter radius = new FloatParameter { value = 0.25f };

        [Tooltip("Number of sample points, which affects quality and performance. Lowest, Low & Medium passes are downsampled. High and Ultra are not and should only be used on high-end hardware.")]
        public AmbientOcclusionQualityParameter quality = new AmbientOcclusionQualityParameter { value = AmbientOcclusionQuality.Medium };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            bool state = enabled.value
                && intensity.value > 0f
                && !RuntimeUtilities.scriptableRenderPipelineActive;

            if (mode.value == AmbientOcclusionMode.MultiScaleVolumetricObscurance)
                state &= SystemInfo.supportsComputeShaders && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat);

            return state;
        }
    }

    public interface IAmbientOcclusionMethod
    {
        DepthTextureMode GetCameraFlags();
        void RenderAfterOpaque(PostProcessRenderContext context);
        void RenderAmbientOnly(PostProcessRenderContext context);
        void CompositeAmbientOnly(PostProcessRenderContext context);
        void Release();
    }

    public sealed class AmbientOcclusionRenderer : PostProcessEffectRenderer<AmbientOcclusion>
    {
        IAmbientOcclusionMethod[] m_Methods;

        public override void Init()
        {
            if (m_Methods == null)
            {
                m_Methods = new IAmbientOcclusionMethod[]
                {
                    new ScalableAO(settings),
                    new MultiScaleVO(settings),
                };
            }
        }

        public bool IsAmbientOnly(PostProcessRenderContext context)
        {
            var camera = context.camera;
            return settings.ambientOnly.value
                && camera.actualRenderingPath == RenderingPath.DeferredShading
                && camera.allowHDR;
        }

        public IAmbientOcclusionMethod Get()
        {
            return m_Methods[(int)settings.mode.value];
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return Get().GetCameraFlags();
        }

        public override void Release()
        {
            foreach (var m in m_Methods)
                m.Release();
        }

        // Unused
        public override void Render(PostProcessRenderContext context)
        {
        }
    }
}
