using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Convolution kernel size for the Depth of Field effect.
    /// </summary>
    public enum KernelSize
    {
        /// <summary>
        /// Small filter.
        /// </summary>
        Small,

        /// <summary>
        /// Medium filter.
        /// </summary>
        Medium,

        /// <summary>
        /// Large filter.
        /// </summary>
        Large,

        /// <summary>
        /// Very large filter.
        /// </summary>
        VeryLarge
    }

    /// <summary>
    /// A volume parameter holding a <see cref="KernelSize"/> value.
    /// </summary>
    [Serializable]
    public sealed class KernelSizeParameter : ParameterOverride<KernelSize> {}

    /// <summary>
    /// This class holds settings for the Depth of Field effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(DepthOfFieldRenderer), "Unity/Depth of Field", false)]
    public sealed class DepthOfField : PostProcessEffectSettings
    {
        /// <summary>
        /// The distance to the point of focus.
        /// </summary>
        [Min(0.1f), Tooltip("Distance to the point of focus.")]
        public FloatParameter focusDistance = new FloatParameter { value = 10f };

        /// <summary>
        /// The ratio of the aperture (known as f-stop or f-number). The smaller the value is, the
        /// shallower the depth of field is.
        /// </summary>
        [Range(0.05f, 32f), Tooltip("Ratio of aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is.")]
        public FloatParameter aperture = new FloatParameter { value = 5.6f };

        /// <summary>
        /// The distance between the lens and the film. The larger the value is, the shallower the
        /// depth of field is.
        /// </summary>
        [Range(1f, 300f), Tooltip("Distance between the lens and the film. The larger the value is, the shallower the depth of field is.")]
        public FloatParameter focalLength = new FloatParameter { value = 50f };

        /// <summary>
        /// The convolution kernel size of the bokeh filter, which determines the maximum radius of
        /// bokeh. It also affects the performance (the larger the kernel is, the longer the GPU
        /// time is required).
        /// </summary>
        [DisplayName("Max Blur Size"), Tooltip("Convolution kernel size of the bokeh filter, which determines the maximum radius of bokeh. It also affects performances (the larger the kernel is, the longer the GPU time is required).")]
        public KernelSizeParameter kernelSize = new KernelSizeParameter { value = KernelSize.Medium };

        /// <summary>
        /// Returns <c>true</c> if the effect is currently enabled and supported.
        /// </summary>
        /// <param name="context">The current post-processing render context</param>
        /// <returns><c>true</c> if the effect is currently enabled and supported</returns>
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && SystemInfo.graphicsShaderLevel >= 35;
        }
    }

    [UnityEngine.Scripting.Preserve]
    // TODO: Doesn't play nice with alpha propagation, see if it can be fixed without killing performances
    internal sealed class DepthOfFieldRenderer : PostProcessEffectRenderer<DepthOfField>
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
            Combine,
            DebugOverlay
        }

        // Ping-pong between two history textures as we can't read & write the same target in the
        // same pass
        const int k_NumEyes = 2;
        const int k_NumCoCHistoryTextures = 2;
        readonly RenderTexture[][] m_CoCHistoryTextures = new RenderTexture[k_NumEyes][];
        int[] m_HistoryPingPong = new int[k_NumEyes];

        // Height of the 35mm full-frame format (36mm x 24mm)
        // TODO: Should be set by a physical camera
        const float k_FilmHeight = 0.024f;

        public DepthOfFieldRenderer()
        {
            for (int eye = 0; eye < k_NumEyes; eye++)
            {
                m_CoCHistoryTextures[eye] = new RenderTexture[k_NumCoCHistoryTextures];
                m_HistoryPingPong[eye] = 0;
            }
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        RenderTextureFormat SelectFormat(RenderTextureFormat primary, RenderTextureFormat secondary)
        {
            if (primary.IsSupported())
                return primary;

            if (secondary.IsSupported())
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

        RenderTexture CheckHistory(int eye, int id, PostProcessRenderContext context, RenderTextureFormat format)
        {
            var rt = m_CoCHistoryTextures[eye][id];

            if (m_ResetHistory || rt == null || !rt.IsCreated() || rt.width != context.width || rt.height != context.height)
            {
                RenderTexture.ReleaseTemporary(rt);

                rt = context.GetScreenSpaceTemporaryRT(0, format, RenderTextureReadWrite.Linear);
                rt.name = "CoC History, Eye: " + eye + ", ID: " + id;
                rt.filterMode = FilterMode.Bilinear;
                rt.Create();
                m_CoCHistoryTextures[eye][id] = rt;
            }

            return rt;
        }

        public override void Render(PostProcessRenderContext context)
        {
            // The coc is stored in alpha so we need a 4 channels target. Note that using ARGB32
            // will result in a very weak near-blur.
            var colorFormat = context.camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            var cocFormat = SelectFormat(RenderTextureFormat.R8, RenderTextureFormat.RHalf);

            // Material setup
            float scaledFilmHeight = k_FilmHeight * (context.height / 1080f);
            var f = settings.focalLength.value / 1000f;
            var s1 = Mathf.Max(settings.focusDistance.value, f);
            var aspect = (float)context.screenWidth / (float)context.screenHeight;
            var coeff = f * f / (settings.aperture.value * (s1 - f) * scaledFilmHeight * 2f);
            var maxCoC = CalculateMaxCoCRadius(context.screenHeight);

            var sheet = context.propertySheets.Get(context.resources.shaders.depthOfField);
            sheet.properties.Clear();
            sheet.properties.SetFloat(ShaderIDs.Distance, s1);
            sheet.properties.SetFloat(ShaderIDs.LensCoeff, coeff);
            sheet.properties.SetFloat(ShaderIDs.MaxCoC, maxCoC);
            sheet.properties.SetFloat(ShaderIDs.RcpMaxCoC, 1f / maxCoC);
            sheet.properties.SetFloat(ShaderIDs.RcpAspect, 1f / aspect);

            var cmd = context.command;
            cmd.BeginSample("DepthOfField");

            // CoC calculation pass
            context.GetScreenSpaceTemporaryRT(cmd, ShaderIDs.CoCTex, 0, cocFormat, RenderTextureReadWrite.Linear);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, ShaderIDs.CoCTex, sheet, (int)Pass.CoCCalculation);

            // CoC temporal filter pass when TAA is enabled
            if (context.IsTemporalAntialiasingActive())
            {
                float motionBlending = context.temporalAntialiasing.motionBlending;
                float blend = m_ResetHistory ? 0f : motionBlending; // Handles first frame blending
                var jitter = context.temporalAntialiasing.jitter;

                sheet.properties.SetVector(ShaderIDs.TaaParams, new Vector3(jitter.x, jitter.y, blend));

                int pp = m_HistoryPingPong[context.xrActiveEye];
                var historyRead = CheckHistory(context.xrActiveEye, ++pp % 2, context, cocFormat);
                var historyWrite = CheckHistory(context.xrActiveEye, ++pp % 2, context, cocFormat);
                m_HistoryPingPong[context.xrActiveEye] = ++pp % 2;

                cmd.BlitFullscreenTriangle(historyRead, historyWrite, sheet, (int)Pass.CoCTemporalFilter);
                cmd.ReleaseTemporaryRT(ShaderIDs.CoCTex);
                cmd.SetGlobalTexture(ShaderIDs.CoCTex, historyWrite);
            }

            // Downsampling and prefiltering pass
            context.GetScreenSpaceTemporaryRT(cmd, ShaderIDs.DepthOfFieldTex, 0, colorFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, context.width / 2, context.height / 2);
            cmd.BlitFullscreenTriangle(context.source, ShaderIDs.DepthOfFieldTex, sheet, (int)Pass.DownsampleAndPrefilter);

            // Bokeh simulation pass
            context.GetScreenSpaceTemporaryRT(cmd, ShaderIDs.DepthOfFieldTemp, 0, colorFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, context.width / 2, context.height / 2);
            cmd.BlitFullscreenTriangle(ShaderIDs.DepthOfFieldTex, ShaderIDs.DepthOfFieldTemp, sheet, (int)Pass.BokehSmallKernel + (int)settings.kernelSize.value);

            // Postfilter pass
            cmd.BlitFullscreenTriangle(ShaderIDs.DepthOfFieldTemp, ShaderIDs.DepthOfFieldTex, sheet, (int)Pass.PostFilter);
            cmd.ReleaseTemporaryRT(ShaderIDs.DepthOfFieldTemp);

            // Debug overlay pass
            if (context.IsDebugOverlayEnabled(DebugOverlay.DepthOfField))
                context.PushDebugOverlay(cmd, context.source, sheet, (int)Pass.DebugOverlay);

            // Combine pass
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Combine);
            cmd.ReleaseTemporaryRT(ShaderIDs.DepthOfFieldTex);

            if (!context.IsTemporalAntialiasingActive())
                cmd.ReleaseTemporaryRT(ShaderIDs.CoCTex);

            cmd.EndSample("DepthOfField");

            m_ResetHistory = false;
        }

        public override void Release()
        {
            for (int eye = 0; eye < k_NumEyes; eye++)
            {
                for (int i = 0; i < m_CoCHistoryTextures[eye].Length; i++)
                {
                    RenderTexture.ReleaseTemporary(m_CoCHistoryTextures[eye][i]);
                    m_CoCHistoryTextures[eye][i] = null;
                }
                m_HistoryPingPong[eye] = 0;
            }

            ResetHistory();
        }
    }
}
