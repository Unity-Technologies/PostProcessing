using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Eye adaptation modes.
    /// </summary>
    public enum EyeAdaptation
    {
        /// <summary>
        /// Progressive (smooth) eye adaptation.
        /// </summary>
        Progressive,

        /// <summary>
        /// Fixed (instant) eye adaptation.
        /// </summary>
        Fixed
    }

    /// <summary>
    /// A volume parameter holding a <see cref="EyeAdaptation"/> value.
    /// </summary>
    [Serializable]
    public sealed class EyeAdaptationParameter : ParameterOverride<EyeAdaptation> {}

    /// <summary>
    /// This class holds settings for the Auto Exposure effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(AutoExposureRenderer), "Unity/Auto Exposure")]
    public sealed class AutoExposure : PostProcessEffectSettings
    {
        /// <summary>
        /// These values are the lower and upper percentages of the histogram that will be used to
        /// find a stable average luminance. Values outside of this range will be discarded and wont
        /// contribute to the average luminance.
        /// </summary>
        [MinMax(1f, 99f), DisplayName("Filtering (%)"), Tooltip("Filters the bright and dark parts of the histogram when computing the average luminance. This is to avoid very dark pixels and very bright pixels from contributing to the auto exposure. Unit is in percent.")]
        public Vector2Parameter filtering = new Vector2Parameter { value = new Vector2(50f, 95f) };

        /// <summary>
        /// Minimum average luminance to consider for auto exposure (in EV).
        /// </summary>
        [Range(LogHistogram.rangeMin, LogHistogram.rangeMax), DisplayName("Minimum (EV)"), Tooltip("Minimum average luminance to consider for auto exposure. Unit is EV.")]
        public FloatParameter minLuminance = new FloatParameter { value = 0f };

        /// <summary>
        /// Maximum average luminance to consider for auto exposure (in EV).
        /// </summary>
        [Range(LogHistogram.rangeMin, LogHistogram.rangeMax), DisplayName("Maximum (EV)"), Tooltip("Maximum average luminance to consider for auto exposure. Unit is EV.")]
        public FloatParameter maxLuminance = new FloatParameter { value = 0f };

        /// <summary>
        /// Middle-grey value. Use this to compensate the global exposure of the scene.
        /// </summary>
        [Min(0f), DisplayName("Exposure Compensation"), Tooltip("Use this to scale the global exposure of the scene.")]
        public FloatParameter keyValue = new FloatParameter { value = 1f };

        /// <summary>
        /// The type of eye adaptation to use.
        /// </summary>
        [DisplayName("Type"), Tooltip("Use \"Progressive\" if you want auto exposure to be animated. Use \"Fixed\" otherwise.")]
        public EyeAdaptationParameter eyeAdaptation = new EyeAdaptationParameter { value = EyeAdaptation.Progressive };

        /// <summary>
        /// The adaptation speed from a dark to a light environment.
        /// </summary>
        [Min(0f), Tooltip("Adaptation speed from a dark to a light environment.")]
        public FloatParameter speedUp = new FloatParameter { value = 2f };

        /// <summary>
        /// The adaptation speed from a light to a dark environment.
        /// </summary>
        [Min(0f), Tooltip("Adaptation speed from a light to a dark environment.")]
        public FloatParameter speedDown = new FloatParameter { value = 1f };

        /// <summary>
        /// Returns <c>true</c> if the effect is currently enabled and supported.
        /// </summary>
        /// <param name="context">The current post-processing render context</param>
        /// <returns><c>true</c> if the effect is currently enabled and supported</returns>
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && SystemInfo.supportsComputeShaders
                && !RuntimeUtilities.isAndroidOpenGL
                && RenderTextureFormat.RFloat.IsSupported()
                && context.resources.computeShaders.autoExposure
                && context.resources.computeShaders.exposureHistogram;
        }
    }
    
    [UnityEngine.Scripting.Preserve]
    internal sealed class AutoExposureRenderer : PostProcessEffectRenderer<AutoExposure>
    {
        const int k_NumEyes = 2;
        const int k_NumAutoExposureTextures = 2;

        readonly RenderTexture[][] m_AutoExposurePool = new RenderTexture[k_NumEyes][];
        int[] m_AutoExposurePingPong = new int[k_NumEyes];
        RenderTexture m_CurrentAutoExposure;

        public AutoExposureRenderer()
        {
            for (int eye = 0; eye < k_NumEyes; eye++)
            {
                m_AutoExposurePool[eye] = new RenderTexture[k_NumAutoExposureTextures];
                m_AutoExposurePingPong[eye] = 0;
            }
        }

        void CheckTexture(int eye, int id)
        {
            if (m_AutoExposurePool[eye][id] == null || !m_AutoExposurePool[eye][id].IsCreated())
            {
                m_AutoExposurePool[eye][id] = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat) { enableRandomWrite = true };
                m_AutoExposurePool[eye][id].Create();
            }
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("AutoExposureLookup");

            // Prepare autoExpo texture pool
            CheckTexture(context.xrActiveEye, 0);
            CheckTexture(context.xrActiveEye, 1);

            // Make sure filtering values are correct to avoid apocalyptic consequences
            float lowPercent = settings.filtering.value.x;
            float highPercent = settings.filtering.value.y;
            const float kMinDelta = 1e-2f;
            highPercent = Mathf.Clamp(highPercent, 1f + kMinDelta, 99f);
            lowPercent = Mathf.Clamp(lowPercent, 1f, highPercent - kMinDelta);

            // Clamp min/max adaptation values as well
            float minLum = settings.minLuminance.value;
            float maxLum = settings.maxLuminance.value;
            settings.minLuminance.value = Mathf.Min(minLum, maxLum);
            settings.maxLuminance.value = Mathf.Max(minLum, maxLum);

            // Compute average luminance & auto exposure
            bool firstFrame = m_ResetHistory || !Application.isPlaying;
            string adaptation = null;

            if (firstFrame || settings.eyeAdaptation.value == EyeAdaptation.Fixed)
                adaptation = "KAutoExposureAvgLuminance_fixed";
            else
                adaptation = "KAutoExposureAvgLuminance_progressive";

            var compute = context.resources.computeShaders.autoExposure;
            int kernel = compute.FindKernel(adaptation);
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", context.logHistogram.data);
            cmd.SetComputeVectorParam(compute, "_Params1", new Vector4(lowPercent * 0.01f, highPercent * 0.01f, RuntimeUtilities.Exp2(settings.minLuminance.value), RuntimeUtilities.Exp2(settings.maxLuminance.value)));
            cmd.SetComputeVectorParam(compute, "_Params2", new Vector4(settings.speedDown.value, settings.speedUp.value, settings.keyValue.value, Time.deltaTime));
            cmd.SetComputeVectorParam(compute, "_ScaleOffsetRes", context.logHistogram.GetHistogramScaleOffsetRes(context));

            if (firstFrame)
            {
                // We don't want eye adaptation when not in play mode because the GameView isn't
                // animated, thus making it harder to tweak. Just use the final audo exposure value.
                m_CurrentAutoExposure = m_AutoExposurePool[context.xrActiveEye][0];
                cmd.SetComputeTextureParam(compute, kernel, "_Destination", m_CurrentAutoExposure);
                cmd.DispatchCompute(compute, kernel, 1, 1, 1);

                // Copy current exposure to the other pingpong target to avoid adapting from black
                RuntimeUtilities.CopyTexture(cmd, m_AutoExposurePool[context.xrActiveEye][0], m_AutoExposurePool[context.xrActiveEye][1]);
                m_ResetHistory = false;
            }
            else
            {
                int pp = m_AutoExposurePingPong[context.xrActiveEye];
                var src = m_AutoExposurePool[context.xrActiveEye][++pp % 2];
                var dst = m_AutoExposurePool[context.xrActiveEye][++pp % 2];
                
                cmd.SetComputeTextureParam(compute, kernel, "_Source", src);
                cmd.SetComputeTextureParam(compute, kernel, "_Destination", dst);
                cmd.DispatchCompute(compute, kernel, 1, 1, 1);

                m_AutoExposurePingPong[context.xrActiveEye] = ++pp % 2;
                m_CurrentAutoExposure = dst;
            }

            cmd.EndSample("AutoExposureLookup");

            context.autoExposureTexture = m_CurrentAutoExposure;
            context.autoExposure = settings;
        }

        public override void Release()
        {
            foreach (var rtEyeSet in m_AutoExposurePool)
            {
                foreach (var rt in rtEyeSet)
                    RuntimeUtilities.Destroy(rt);
            }
        }
    }
}
