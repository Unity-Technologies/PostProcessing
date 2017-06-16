using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.PostProcessing
{
    public enum KernelSize
    {
        Small,
        Medium,
        Large,
        VeryLarge
    }

    [Serializable]
    public sealed class KernelSizeParameter : ParameterOverride<KernelSize> {}

    [Serializable]
    [PostProcess(typeof(DepthOfFieldRenderer), "Unity/Depth of Field", false)]
    public sealed class DepthOfField : PostProcessEffectSettings
    {
        [Min(0.1f), Tooltip("Distance to the point of focus.")]
        public FloatParameter focusDistance = new FloatParameter { value = 10f };

        [Range(0.05f, 32f), Tooltip("Ratio of aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is.")]
        public FloatParameter aperture = new FloatParameter { value = 5.6f };

        [Range(1f, 300f), Tooltip("Distance between the lens and the film. The larger the value is, the shallower the depth of field is.")]
        public FloatParameter focalLength = new FloatParameter { value = 50f };

        [DisplayName("Max Blur Size"), Tooltip("Convolution kernel size of the bokeh filter, which determines the maximum radius of bokeh. It also affects performances (the larger the kernel is, the longer the GPU time is required).")]
        public KernelSizeParameter kernelSize = new KernelSizeParameter { value = KernelSize.Medium };

        public override bool IsEnabledAndSupported()
        {
            return enabled.value
                && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
        }

        public override void SetDisabledState()
        {
            aperture.value = 0.1f;
            focalLength.value = 0.1f;
        }
    }
    
    // TODO: Look into minimum blur amount in the distance, right now it's lerped until a point
    public sealed class DepthOfFieldRenderer : PostProcessEffectRenderer<DepthOfField>
    {
        enum Pass
        {
            CoCCalculation,
            CoCTemporalFilter,
            DownsampleAndPrefilter,
            BokehSmallKernel,
            BokehMediumKernel,
            BokehLargeKernel,
            BokehVeryLargeKernel,
            PostFilter,
            Combine
        }

        // Ping-pong between two history textures as we can't read & write the same target in the
        // same pass
        readonly RenderTexture[] m_CoCHistoryTextures = new RenderTexture[2];
        int m_HistoryPingPong;

        // Height of the 35mm full-frame format (36mm x 24mm)
        // TODO: Should be set by a physical camera
        const float k_FilmHeight = 0.024f;

        public override DepthTextureMode GetLegacyCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        RenderTextureFormat SelectFormat(RenderTextureFormat primary, RenderTextureFormat secondary)
        {
            if (SystemInfo.SupportsRenderTextureFormat(primary))
                return primary;

            if (SystemInfo.SupportsRenderTextureFormat(secondary))
                return secondary;

            return RenderTextureFormat.Default;
        }

        float CalculateMaxCoCRadius(int screenHeight)
        {
            // Estimate the allowable maximum radius of CoC from the kernel
            // size (the equation below was empirically derived).
            float radiusInPixels = (float)settings.kernelSize.value * 4f + 6f;

            // Applying a 5% limit to the CoC radius to keep the size of
            // TileMax/NeighborMax small enough.
            return Mathf.Min(0.05f, radiusInPixels / screenHeight);
        }

        RenderTexture CheckHistory(int id, int width, int height, RenderTextureFormat format)
        {
            var rt = m_CoCHistoryTextures[id];

            if (m_ResetHistory || rt == null || !rt.IsCreated() || rt.width != width || rt.height != height)
            {
                RenderTexture.ReleaseTemporary(rt);

                rt = RenderTexture.GetTemporary(width, height, 0, format);
                rt.name = "CoC History";
                rt.filterMode = FilterMode.Bilinear;
                rt.Create();
                m_CoCHistoryTextures[id] = rt;
            }

            return rt;
        }

        public override void Render(PostProcessRenderContext context)
        {
            var colorFormat = RenderTextureFormat.ARGBHalf;
            var cocFormat = SelectFormat(RenderTextureFormat.R8, RenderTextureFormat.RHalf);

            // Avoid using R8 on OSX with Metal. #896121, https://goo.gl/MgKqu6
            #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal)
                cocFormat = SelectFormat(RenderTextureFormat.RHalf, RenderTextureFormat.Default);
            #endif

            // Material setup
            var f = settings.focalLength.value / 1000f;
            var s1 = Mathf.Max(settings.focusDistance.value, f);
            var aspect = (float)context.width / (float)context.height;
            var coeff = f * f / (settings.aperture.value * (s1 - f) * k_FilmHeight * 2);
            var maxCoC = CalculateMaxCoCRadius(context.height);

            var sheet = context.propertySheets.Get(context.resources.shaders.depthOfField);
            sheet.properties.Clear();
            sheet.properties.SetFloat(Uniforms._Distance, s1);
            sheet.properties.SetFloat(Uniforms._LensCoeff, coeff);
            sheet.properties.SetFloat(Uniforms._MaxCoC, maxCoC);
            sheet.properties.SetFloat(Uniforms._RcpMaxCoC, 1f / maxCoC);
            sheet.properties.SetFloat(Uniforms._RcpAspect, 1f / aspect);

            var cmd = context.command;
            cmd.BeginSample("DepthOfField");

            // CoC calculation pass
            cmd.GetTemporaryRT(Uniforms._CoCTex, context.width, context.height, 0, FilterMode.Bilinear, cocFormat);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, Uniforms._CoCTex, sheet, (int)Pass.CoCCalculation);

            // CoC temporal filter pass when TAA is enabled
            if (context.IsTemporalAntialiasingActive())
            {
                float motionBlending = context.temporalAntialiasing.motionBlending;
                float blend = m_ResetHistory ? 0f : motionBlending; // Handles first frame blending
                var jitter = context.temporalAntialiasing.jitter;
                
                sheet.properties.SetVector(Uniforms._TaaParams, new Vector3(jitter.x, jitter.y, blend));

                int pp = m_HistoryPingPong;
                var historyRead = CheckHistory(++pp % 2, context.width, context.height, cocFormat);
                var historyWrite = CheckHistory(++pp % 2, context.width, context.height, cocFormat);
                m_HistoryPingPong = ++pp % 2;

                cmd.BlitFullscreenTriangle(historyRead, historyWrite, sheet, (int)Pass.CoCTemporalFilter);
                cmd.ReleaseTemporaryRT(Uniforms._CoCTex);
                cmd.SetGlobalTexture(Uniforms._CoCTex, historyWrite);
            }

            // Downsampling and prefiltering pass
            cmd.GetTemporaryRT(Uniforms._DepthOfFieldTex, context.width / 2, context.height / 2, 0, FilterMode.Bilinear, colorFormat);
            cmd.BlitFullscreenTriangle(context.source, Uniforms._DepthOfFieldTex, sheet, (int)Pass.DownsampleAndPrefilter);

            // Bokeh simulation pass
            cmd.GetTemporaryRT(Uniforms._DepthOfFieldTemp, context.width / 2, context.height / 2, 0, FilterMode.Bilinear, colorFormat);
            cmd.BlitFullscreenTriangle(Uniforms._DepthOfFieldTex, Uniforms._DepthOfFieldTemp, sheet, (int)Pass.BokehSmallKernel + (int)settings.kernelSize.value);

            // Postfilter pass
            cmd.BlitFullscreenTriangle(Uniforms._DepthOfFieldTemp, Uniforms._DepthOfFieldTex, sheet, (int)Pass.PostFilter);
            cmd.ReleaseTemporaryRT(Uniforms._DepthOfFieldTemp);

            // Combine pass
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Combine);
            cmd.ReleaseTemporaryRT(Uniforms._DepthOfFieldTex);

            if (!context.IsTemporalAntialiasingActive())
                cmd.ReleaseTemporaryRT(Uniforms._CoCTex);

            cmd.EndSample("DepthOfField");

            m_ResetHistory = false;
        }

        public override void Release()
        {
            for (int i = 0; i < m_CoCHistoryTextures.Length; i++)
            {
                RenderTexture.ReleaseTemporary(m_CoCHistoryTextures[i]);
                m_CoCHistoryTextures[i] = null;
            }

            m_HistoryPingPong = 0;
            ResetHistory();
        }
    }
}
