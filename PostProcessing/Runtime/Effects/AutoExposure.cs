using System;

namespace UnityEngine.Experimental.PostProcessing
{
    public enum EyeAdaptation
    {
        Progressive,
        Fixed
    }

    [Serializable]
    public sealed class EyeAdaptationParameter : ParameterOverride<EyeAdaptation> {}

    [Serializable]
    [PostProcess(typeof(AutoExposureRenderer), "Unity/Auto Exposure")]
    public sealed class AutoExposure : PostProcessEffectSettings
    {
        public static readonly int histogramLogMin = -9;
        public static readonly int histogramLogMax =  9;

        [MinMax(1f, 99f), DisplayName("Filtering (%)"), Tooltip("Filters the bright & dark part of the histogram when computing the average luminance to avoid very dark pixels & very bright pixels from contributing to the auto exposure. Unit is in percent.")]
        public Vector2Parameter filtering = new Vector2Parameter { value = new Vector2(50f, 95f) };

        [DisplayName("Minimum (EV)"), Tooltip("Minimum average luminance to consider for auto exposure (in EV).")]
        public FloatParameter minLuminance = new FloatParameter { value = -5f };

        [DisplayName("Maximum (EV)"), Tooltip("Maximum average luminance to consider for auto exposure (in EV).")]
        public FloatParameter maxLuminance = new FloatParameter { value = 1f };

        [Tooltip("Set this to true to let Unity handle the key value automatically based on average luminance.")]
        public BoolParameter dynamicKeyValue = new BoolParameter { value = true };

        [Min(0f), Tooltip("Exposure bias. Use this to offset the global exposure of the scene.")]
        public FloatParameter keyValue = new FloatParameter { value = 0.25f };

        [DisplayName("Type")]
        public EyeAdaptationParameter eyeAdaptation = new EyeAdaptationParameter { value = EyeAdaptation.Progressive };

        [Min(0f), Tooltip("Adaptation speed from a dark to a light environment.")]
        public FloatParameter speedUp = new FloatParameter { value = 2f };

        [Min(0f), Tooltip("Adaptation speed from a light to a dark environment.")]
        public FloatParameter speedDown = new FloatParameter { value = 1f };

        public override bool IsEnabledAndSupported()
        {
            return enabled.value
                && SystemInfo.supportsComputeShaders
                && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf);
        }
    }

    public sealed class AutoExposureRenderer : PostProcessEffectRenderer<AutoExposure>
    {
        ComputeShader m_EyeCompute;
        ComputeBuffer m_HistogramBuffer;

        readonly RenderTexture[] m_AutoExposurePool = new RenderTexture[2];
        int m_AutoExposurePingPong;
        RenderTexture m_CurrentAutoExposure;

        static readonly uint[] s_EmptyHistogramBuffer = new uint[k_HistogramBins];

        bool m_FirstFrame = true;

        // Don't forget to update 'ExposureHistogram.hlsl' if you change these values !
        const int k_HistogramBins = 64;
        const int k_HistogramThreadX = 16;
        const int k_HistogramThreadY = 16;

        internal Vector4 GetHistogramScaleOffsetRes(PostProcessRenderContext context)
        {
            float diff = AutoExposure.histogramLogMax - AutoExposure.histogramLogMin;
            float scale = 1f / diff;
            float offset = -AutoExposure.histogramLogMin * scale;
            return new Vector4(scale, offset, Mathf.Floor(context.width / 2f), Mathf.Floor(context.height / 2f));
        }

        void CheckTexture(int id)
        {
            if (m_AutoExposurePool[id] == null || !m_AutoExposurePool[id].IsCreated())
            {
                m_AutoExposurePool[id] = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat);
                m_AutoExposurePool[id].Create();
            }
        }

        public override void Render(PostProcessRenderContext context)
        {
            // Setup compute
            if (m_EyeCompute == null)
                m_EyeCompute = Resources.Load<ComputeShader>("Shaders/Builtins/ExposureHistogram");

            var cmd = context.command;
            cmd.BeginSample("AutoExposureLookup");

            var sheet = context.propertySheets.Get("Hidden/PostProcessing/AutoExposure");
            sheet.ClearKeywords();

            if (m_HistogramBuffer == null)
                m_HistogramBuffer = new ComputeBuffer(k_HistogramBins, sizeof(uint));

            // Downscale the framebuffer, we don't need an absolute precision for auto exposure and it
            // helps making it more stable
            var scaleOffsetRes = GetHistogramScaleOffsetRes(context);

            cmd.GetTemporaryRT(Uniforms._AutoExposureCopyTex,
                (int)scaleOffsetRes.z,
                (int)scaleOffsetRes.w,
                0,
                FilterMode.Bilinear,
                context.sourceFormat);
            cmd.BlitFullscreenTriangle(context.source, Uniforms._AutoExposureCopyTex);

            // Prepare autoExpo texture pool
            CheckTexture(0);
            CheckTexture(1);

            // Clear the buffer on every frame as we use it to accumulate luminance values on each frame
            m_HistogramBuffer.SetData(s_EmptyHistogramBuffer);

            // Get a log histogram
            int kernel = m_EyeCompute.FindKernel("KEyeHistogram");
            cmd.SetComputeBufferParam(m_EyeCompute, kernel, "_HistogramBuffer", m_HistogramBuffer);
            cmd.SetComputeTextureParam(m_EyeCompute, kernel, "_Source", Uniforms._AutoExposureCopyTex);
            cmd.SetComputeVectorParam(m_EyeCompute, "_ScaleOffsetRes", scaleOffsetRes);
            cmd.DispatchCompute(m_EyeCompute, kernel,
                Mathf.CeilToInt(scaleOffsetRes.z / (float)k_HistogramThreadX),
                Mathf.CeilToInt(scaleOffsetRes.w / (float)k_HistogramThreadY),
                1);

            // Cleanup
            cmd.ReleaseTemporaryRT(Uniforms._AutoExposureCopyTex);

            // Make sure filtering values are correct to avoid apocalyptic consequences
            float lowPercent = settings.filtering.value.x;
            float highPercent = settings.filtering.value.y;
            const float kMinDelta = 1e-2f;
            highPercent = Mathf.Clamp(highPercent, 1f + kMinDelta, 99f);
            lowPercent = Mathf.Clamp(lowPercent, 1f, highPercent - kMinDelta);

            // Compute auto exposure
            sheet.properties.SetBuffer(Uniforms._HistogramBuffer, m_HistogramBuffer);
            sheet.properties.SetVector(Uniforms._Params, new Vector4(lowPercent * 0.01f, highPercent * 0.01f, RuntimeUtilities.Exp2(settings.minLuminance.value), RuntimeUtilities.Exp2(settings.maxLuminance.value)));
            sheet.properties.SetVector(Uniforms._Speed, new Vector2(settings.speedDown.value, settings.speedUp.value));
            sheet.properties.SetVector(Uniforms._ScaleOffsetRes, scaleOffsetRes);
            sheet.properties.SetFloat(Uniforms._ExposureCompensation, settings.keyValue.value);

            if (settings.dynamicKeyValue)
                sheet.EnableKeyword("AUTO_KEY_VALUE");

            if (m_FirstFrame || !Application.isPlaying)
            {
                // We don't want eye adaptation when not in play mode because the GameView isn't
                // animated, thus making it harder to tweak. Just use the final audo exposure value.
                m_CurrentAutoExposure = m_AutoExposurePool[0];
                cmd.BlitFullscreenTriangle((Texture)null, m_CurrentAutoExposure, sheet, (int)EyeAdaptation.Fixed);

                // Copy current exposure to the other pingpong target to avoid adapting from black
                RuntimeUtilities.CopyTexture(cmd, m_AutoExposurePool[0], m_AutoExposurePool[1]);
            }
            else
            {
                int pp = m_AutoExposurePingPong;
                var src = m_AutoExposurePool[++pp % 2];
                var dst = m_AutoExposurePool[++pp % 2];
                cmd.BlitFullscreenTriangle(src, dst, sheet, (int)settings.eyeAdaptation.value);
                m_AutoExposurePingPong = ++pp % 2;
                m_CurrentAutoExposure = dst;
            }
            
            cmd.EndSample("AutoExposureLookup");

            context.autoExposureTexture = m_CurrentAutoExposure;
            m_FirstFrame = false;
        }

        public override void Release()
        {
            foreach (var rt in m_AutoExposurePool)
                RuntimeUtilities.Destroy(rt);

            if (m_HistogramBuffer != null)
                m_HistogramBuffer.Release();

            m_HistogramBuffer = null;
        }
    }
}
