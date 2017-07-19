using System;

namespace UnityEngine.Rendering.PostProcessing
{
    // TODO: VR support
    [Serializable]
    public sealed class TemporalAntialiasing
    {
        [Tooltip("The diameter (in texels) inside which jitter samples are spread. Smaller values result in crisper but more aliased output, while larger values result in more stable but blurrier output.")]
        [Range(0.1f, 1f)]
        public float jitterSpread = 0.75f;

        [Tooltip("Controls the amount of sharpening applied to the color buffer.")]
        [Range(0f, 3f)]
        public float sharpen = 0.25f;

        [Tooltip("The blend coefficient for a stationary fragment. Controls the percentage of history sample blended into the final color.")]
        [Range(0f, 0.99f)]
        public float stationaryBlending = 0.95f;

        [Tooltip("The blend coefficient for a fragment with significant motion. Controls the percentage of history sample blended into the final color.")]
        [Range(0f, 0.99f)]
        public float motionBlending = 0.85f;

        // For custom jittered matrices - use at your own risks
        public Func<Camera, Vector2, Matrix4x4> jitteredMatrixFunc;

        public Vector2 jitter { get; private set; }

        enum Pass
        {
            SolverDilate,
            SolverNoDilate
        }

        readonly RenderTargetIdentifier[] m_Mrt = new RenderTargetIdentifier[2];
        bool m_ResetHistory = true;

        const int k_SampleCount = 8;
        int m_SampleIndex;

        // Ping-pong between two history textures as we can't read & write the same target in the
        // same pass
        readonly RenderTexture[] m_HistoryTextures = new RenderTexture[2];
        int m_HistoryPingPong;

        public bool IsSupported()
        {
            return SystemInfo.supportedRenderTargetCount >= 2
                && SystemInfo.supportsMotionVectors
                && !RuntimeUtilities.isSinglePassStereoEnabled;
        }

        internal DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        internal void ResetHistory()
        {
            m_ResetHistory = true;
        }

        Vector2 GenerateRandomOffset()
        {
            var offset = new Vector2(
                    HaltonSeq.Get(m_SampleIndex & 1023, 2),
                    HaltonSeq.Get(m_SampleIndex & 1023, 3)
                );

            if (++m_SampleIndex >= k_SampleCount)
                m_SampleIndex = 0;

            return offset;
        }

        public Matrix4x4 GetJitteredProjectionMatrix(Camera camera)
        {
            Matrix4x4 cameraProj;
            jitter = GenerateRandomOffset();
            jitter *= jitterSpread;

            if (jitteredMatrixFunc != null)
            {
                cameraProj = jitteredMatrixFunc(camera, jitter);
            }
            else
            {
                cameraProj = camera.orthographic
                    ? RuntimeUtilities.GetJitteredOrthographicProjectionMatrix(camera, jitter)
                    : RuntimeUtilities.GetJitteredPerspectiveProjectionMatrix(camera, jitter);
            }

            jitter = new Vector2(jitter.x / camera.pixelWidth, jitter.y / camera.pixelHeight);
            return cameraProj;
        }

        RenderTexture CheckHistory(int id, PostProcessRenderContext context)
        {
            var rt = m_HistoryTextures[id];

            if (m_ResetHistory || rt == null || !rt.IsCreated())
            {
                RenderTexture.ReleaseTemporary(rt);

                rt = RenderTexture.GetTemporary(context.width, context.height, 0, context.sourceFormat);
                rt.name = "Temporal Anti-aliasing History";
                rt.filterMode = FilterMode.Bilinear;
                m_HistoryTextures[id] = rt;

                context.command.BlitFullscreenTriangle(context.source, rt);
            }
            else if (rt.width != context.width || rt.height != context.height)
            {
                // On size change, simply copy the old history to the new one. This looks better
                // than completely discarding the history and seeing a few aliased frames.
                var rt2 = RenderTexture.GetTemporary(context.width, context.height, 0, context.sourceFormat);
                rt2.name = "Temporal Anti-aliasing History";
                rt2.filterMode = FilterMode.Bilinear;
                m_HistoryTextures[id] = rt2;

                context.command.BlitFullscreenTriangle(rt, rt2);
                RenderTexture.ReleaseTemporary(rt);
            }

            return m_HistoryTextures[id];
        }

        internal void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(context.resources.shaders.temporalAntialiasing);

            var cmd = context.command;
            cmd.BeginSample("TemporalAntialiasing");

            int pp = m_HistoryPingPong;
            var historyRead = CheckHistory(++pp % 2, context);
            var historyWrite = CheckHistory(++pp % 2, context);
            m_HistoryPingPong = ++pp % 2;

            const float kMotionAmplification = 100f * 60f;
            sheet.properties.SetVector(ShaderIDs.Jitter, jitter);
            sheet.properties.SetFloat(ShaderIDs.SharpenParameters, sharpen);
            sheet.properties.SetVector(ShaderIDs.FinalBlendParameters, new Vector4(stationaryBlending, motionBlending, kMotionAmplification, 0f));
            sheet.properties.SetTexture(ShaderIDs.HistoryTex, historyRead);

            int pass = context.camera.orthographic ? (int)Pass.SolverNoDilate : (int)Pass.SolverDilate;
            m_Mrt[0] = context.destination;
            m_Mrt[1] = historyWrite;

            cmd.BlitFullscreenTriangle(context.source, m_Mrt, context.source, sheet, pass);
            cmd.EndSample("TemporalAntialiasing");

            m_ResetHistory = false;
        }

        internal void Release()
        {
            for (int i = 0; i < m_HistoryTextures.Length; i++)
            {
                RenderTexture.ReleaseTemporary(m_HistoryTextures[i]);
                m_HistoryTextures[i] = null;
            }

            m_SampleIndex = 0;
            m_HistoryPingPong = 0;
            
            ResetHistory();
        }
    }
}
