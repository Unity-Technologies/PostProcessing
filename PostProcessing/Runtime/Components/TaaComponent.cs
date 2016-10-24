using UnityEngine.Rendering;

namespace UnityEngine.PostProcessing
{
    public sealed class TaaComponent : PostProcessingComponentRenderTexture<AntialiasingModel>
    {
        static class Uniforms
        {
            internal static int _Jitter               = Shader.PropertyToID("_Jitter");
            internal static int _SharpenParameters    = Shader.PropertyToID("_SharpenParameters");
            internal static int _FinalBlendParameters = Shader.PropertyToID("_FinalBlendParameters");
            internal static int _HistoryTex           = Shader.PropertyToID("_HistoryTex");
            internal static int _MainTex              = Shader.PropertyToID("_MainTex");
            internal static int _DepthHistory1Tex     = Shader.PropertyToID("_DepthHistory1Tex");
            internal static int _DepthHistory2Tex     = Shader.PropertyToID("_DepthHistory2Tex");
        }

        const string k_ShaderString = "Hidden/Post FX/Temporal Anti-aliasing";
        const int k_SampleCount = 7; // Don't touch this as it'll break depth dejittering

        readonly RenderBuffer[] m_MRT2 = new RenderBuffer[2];
        readonly RenderBuffer[] m_MRT4 = new RenderBuffer[4];

        int m_SampleIndex;
        bool m_ResetHistory;

        RenderTexture m_HistoryTexture;

        RenderTexture m_DepthHistory1;
        RenderTexture m_DepthHistory2;

        public RenderTexture dejitteredDepth { get { return m_DepthHistory1; } }

        public override bool active
        {
            get
            {
                return model.enabled
                       && model.settings.method == AntialiasingModel.Method.Taa
                       && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                       && SystemInfo.supportsMotionVectors
                       && !context.interrupted;
            }
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        public void ResetHistory()
        {
            m_ResetHistory = true;
        }

        public void SetProjectionMatrix()
        {
            var settings = model.settings.taaSettings;

            var jitter = GenerateRandomOffset();
            jitter *= settings.jitterSpread;

            context.camera.nonJitteredProjectionMatrix = context.camera.projectionMatrix;
            context.camera.projectionMatrix = context.camera.orthographic
                ? GetOrthographicProjectionMatrix(jitter)
                : GetPerspectiveProjectionMatrix(jitter);

#if UNITY_5_5_OR_NEWER
            context.camera.useJitteredProjectionMatrixForTransparentRendering = true;
#endif

            jitter.x /= context.width;
            jitter.y /= context.height;

            var material = context.materialFactory.Get(k_ShaderString);
            material.SetVector(Uniforms._Jitter, jitter);
        }

        public void Render(RenderTexture source, RenderTexture destination, bool dejitterDepth)
        {
            var material = context.materialFactory.Get(k_ShaderString);
            material.shaderKeywords = null;

            var settings = model.settings.taaSettings;

            if (m_ResetHistory || m_HistoryTexture == null || m_HistoryTexture.width != source.width || m_HistoryTexture.height != source.height)
            {
                if (m_HistoryTexture)
                    RenderTexture.ReleaseTemporary(m_HistoryTexture);

                m_HistoryTexture = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
                m_HistoryTexture.name = "TAA History";

                Graphics.Blit(source, m_HistoryTexture, material, 2);
            }

            const float kMotionAmplification = 100f * 60f;
            material.SetVector(Uniforms._SharpenParameters, new Vector4(settings.sharpen, 0f, 0f, 0f));
            material.SetVector(Uniforms._FinalBlendParameters, new Vector4(settings.stationaryBlending, settings.motionBlending, kMotionAmplification, 0f));
            material.SetTexture(Uniforms._MainTex, source);
            material.SetTexture(Uniforms._HistoryTex, m_HistoryTexture);

            var tempHistory = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            tempHistory.name = "TAA History";

            var mrt = dejitterDepth ? m_MRT4 : m_MRT2;

            mrt[0] = destination.colorBuffer;
            mrt[1] = tempHistory.colorBuffer;

            RenderTexture tempDepthHistory1 = null, tempDepthHistory2 = null;
            if (dejitterDepth)
            {
                if (m_ResetHistory || m_DepthHistory1 == null || m_DepthHistory1.width != source.width || m_DepthHistory1.height != source.height)
                {
                    if (m_DepthHistory1)
                        RenderTexture.ReleaseTemporary(m_DepthHistory1);

                    m_DepthHistory1 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
                    m_DepthHistory1.name = "Depth History 2";

                    Graphics.Blit(source, m_DepthHistory1, material, 3);
                }

                if (m_ResetHistory || m_DepthHistory2 == null || m_DepthHistory2.width != source.width || m_DepthHistory2.height != source.height)
                {
                    if (m_DepthHistory2)
                        RenderTexture.ReleaseTemporary(m_DepthHistory2);

                    m_DepthHistory2 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
                    m_DepthHistory2.name = "Depth History 1";

                    Graphics.Blit(source, m_DepthHistory2, material, 3);
                }

                material.SetTexture(Uniforms._DepthHistory1Tex, m_DepthHistory1);
                material.SetTexture(Uniforms._DepthHistory2Tex, m_DepthHistory2);
                material.EnableKeyword("DEJITTER_DEPTH");

                tempDepthHistory1 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
                tempDepthHistory1.name = "Depth History 1";
                tempDepthHistory2 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
                tempDepthHistory2.name = "Depth History 2";

                mrt[2] = tempDepthHistory1.colorBuffer;
                mrt[3] = tempDepthHistory2.colorBuffer;
            }

            Graphics.SetRenderTarget(mrt, destination.depthBuffer);
            GraphicsUtils.Blit(material, context.camera.orthographic ? 1 : 0);

            RenderTexture.ReleaseTemporary(m_HistoryTexture);
            m_HistoryTexture = tempHistory;

            if (dejitterDepth)
            {
                RenderTexture.ReleaseTemporary(m_DepthHistory1);
                RenderTexture.ReleaseTemporary(m_DepthHistory2);
                m_DepthHistory1 = tempDepthHistory1;
                m_DepthHistory2 = tempDepthHistory2;
            }

            if (!dejitterDepth)
            {
                if (m_DepthHistory1)
                    RenderTexture.ReleaseTemporary(m_DepthHistory1);
                if (m_DepthHistory2)
                    RenderTexture.ReleaseTemporary(m_DepthHistory2);

                m_DepthHistory1 = null;
                m_DepthHistory2 = null;
            }

            m_ResetHistory = false;
        }

        float GetHaltonValue(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / (float)radix;

            while (index > 0)
            {
                result += (float)(index % radix) * fraction;

                index /= radix;
                fraction /= (float)radix;
            }

            return result;
        }

        Vector2 GenerateRandomOffset()
        {
            var offset = new Vector2(
                    GetHaltonValue(m_SampleIndex & 1023, 2),
                    GetHaltonValue(m_SampleIndex & 1023, 3));

            if (++m_SampleIndex >= k_SampleCount)
                m_SampleIndex = 0;

            return offset;
        }

        // Adapted heavily from PlayDead's TAA code
        // https://github.com/playdeadgames/temporal/blob/master/Assets/Scripts/Extensions.cs
        Matrix4x4 GetPerspectiveProjectionMatrix(Vector2 offset)
        {
            float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * context.camera.fieldOfView);
            float horizontal = vertical * context.camera.aspect;

            offset.x *= horizontal / (0.5f * context.width);
            offset.y *= vertical / (0.5f * context.height);

            float left = (offset.x - horizontal) * context.camera.nearClipPlane;
            float right = (offset.x + horizontal) * context.camera.nearClipPlane;
            float top = (offset.y + vertical) * context.camera.nearClipPlane;
            float bottom = (offset.y - vertical) * context.camera.nearClipPlane;

            var matrix = new Matrix4x4();

            matrix[0, 0] = (2f * context.camera.nearClipPlane) / (right - left);
            matrix[0, 1] = 0f;
            matrix[0, 2] = (right + left) / (right - left);
            matrix[0, 3] = 0f;

            matrix[1, 0] = 0f;
            matrix[1, 1] = (2f * context.camera.nearClipPlane) / (top - bottom);
            matrix[1, 2] = (top + bottom) / (top - bottom);
            matrix[1, 3] = 0f;

            matrix[2, 0] = 0f;
            matrix[2, 1] = 0f;
            matrix[2, 2] = -(context.camera.farClipPlane + context.camera.nearClipPlane) / (context.camera.farClipPlane - context.camera.nearClipPlane);
            matrix[2, 3] = -(2f * context.camera.farClipPlane * context.camera.nearClipPlane) / (context.camera.farClipPlane - context.camera.nearClipPlane);

            matrix[3, 0] = 0f;
            matrix[3, 1] = 0f;
            matrix[3, 2] = -1f;
            matrix[3, 3] = 0f;

            return matrix;
        }

        Matrix4x4 GetOrthographicProjectionMatrix(Vector2 offset)
        {
            float vertical = context.camera.orthographicSize;
            float horizontal = vertical * context.camera.aspect;

            offset.x *= horizontal / (0.5f * context.width);
            offset.y *= vertical / (0.5f * context.height);

            float left = offset.x - horizontal;
            float right = offset.x + horizontal;
            float top = offset.y + vertical;
            float bottom = offset.y - vertical;

            return Matrix4x4.Ortho(left, right, bottom, top, context.camera.nearClipPlane, context.camera.farClipPlane);
        }

        public override void OnDisable()
        {
            if (m_HistoryTexture != null)
                RenderTexture.ReleaseTemporary(m_HistoryTexture);

            if (m_DepthHistory1 != null)
                RenderTexture.ReleaseTemporary(m_DepthHistory1);

            if (m_DepthHistory2 != null)
                RenderTexture.ReleaseTemporary(m_DepthHistory2);

            m_HistoryTexture = null;
            m_DepthHistory1 = null;
            m_DepthHistory2 = null;
            m_SampleIndex = 0;
        }
    }
}
