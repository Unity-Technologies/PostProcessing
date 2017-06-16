using System;
using UnityEngine.Rendering;

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
        [MinMax(1f, 99f), DisplayName("Filtering (%)"), Tooltip("Filters the bright & dark part of the histogram when computing the average luminance to avoid very dark pixels & very bright pixels from contributing to the auto exposure. Unit is in percent.")]
        public Vector2Parameter filtering = new Vector2Parameter { value = new Vector2(50f, 95f) };

        [DisplayName("Minimum (EV)"), Tooltip("Minimum average luminance to consider for auto exposure (in EV).")]
        public FloatParameter minLuminance = new FloatParameter { value = -5f };

        [DisplayName("Maximum (EV)"), Tooltip("Maximum average luminance to consider for auto exposure (in EV).")]
        public FloatParameter maxLuminance = new FloatParameter { value = 1f };

        [Min(0f), Tooltip("Exposure bias. Use this to offset the global exposure of the scene.")]
        public FloatParameter keyValue = new FloatParameter { value = 0.25f };

        [DisplayName("Type"), Tooltip("Use \"Progressive\" if you want auto exposure to be animated. Use \"Fixed\" otherwise.")]
        public EyeAdaptationParameter eyeAdaptation = new EyeAdaptationParameter { value = EyeAdaptation.Progressive };

        [Min(0f), Tooltip("Adaptation speed from a dark to a light environment.")]
        public FloatParameter speedUp = new FloatParameter { value = 2f };

        [Min(0f), Tooltip("Adaptation speed from a light to a dark environment.")]
        public FloatParameter speedDown = new FloatParameter { value = 1f };

        public override bool IsEnabledAndSupported()
        {
            return enabled.value
                && SystemInfo.supportsComputeShaders
                && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat);
        }

        public override void SetDisabledState()
        {
            filtering.value = new Vector2(1f, 99f);
            minLuminance.value = 0f;
            maxLuminance.value = 0f;
            keyValue.value = 1f;
        }
    }

    public sealed class AutoExposureRenderer : PostProcessEffectRenderer<AutoExposure>
    {
        readonly RenderTexture[] m_AutoExposurePool = new RenderTexture[2];
        int m_AutoExposurePingPong;
        RenderTexture m_CurrentAutoExposure;

        bool m_FirstFrame = true;

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
            var cmd = context.command;
            cmd.BeginSample("AutoExposureLookup");

            var sheet = context.propertySheets.Get(context.resources.shaders.autoExposure);
            sheet.ClearKeywords();

            // Prepare autoExpo texture pool
            CheckTexture(0);
            CheckTexture(1);

            // Make sure filtering values are correct to avoid apocalyptic consequences
            float lowPercent = settings.filtering.value.x;
            float highPercent = settings.filtering.value.y;
            const float kMinDelta = 1e-2f;
            highPercent = Mathf.Clamp(highPercent, 1f + kMinDelta, 99f);
            lowPercent = Mathf.Clamp(lowPercent, 1f, highPercent - kMinDelta);

            // Compute auto exposure
            sheet.properties.SetBuffer(Uniforms._HistogramBuffer, context.logHistogram.data);
            sheet.properties.SetVector(Uniforms._Params, new Vector4(lowPercent * 0.01f, highPercent * 0.01f, RuntimeUtilities.Exp2(settings.minLuminance.value), RuntimeUtilities.Exp2(settings.maxLuminance.value)));
            sheet.properties.SetVector(Uniforms._Speed, new Vector2(settings.speedDown.value, settings.speedUp.value));
            sheet.properties.SetVector(Uniforms._ScaleOffsetRes, context.logHistogram.GetHistogramScaleOffsetRes(context));
            sheet.properties.SetFloat(Uniforms._ExposureCompensation, settings.keyValue.value);

            if (m_FirstFrame || !Application.isPlaying)
            {
                // We don't want eye adaptation when not in play mode because the GameView isn't
                // animated, thus making it harder to tweak. Just use the final audo exposure value.
                m_CurrentAutoExposure = m_AutoExposurePool[0];
                cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_CurrentAutoExposure, sheet, (int)EyeAdaptation.Fixed);

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
            context.autoExposure = settings;
            m_FirstFrame = false;
        }

        public override void Release()
        {
            foreach (var rt in m_AutoExposurePool)
                RuntimeUtilities.Destroy(rt);
        }
    }
}
