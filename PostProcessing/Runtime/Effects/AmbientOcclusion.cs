using System;

namespace UnityEngine.Rendering.PostProcessing
{
    public interface IAmbientOcclusionMethod
    {
        DepthTextureMode GetCameraFlags();
        bool IsSupported(PostProcessRenderContext context);
        void RenderAfterOpaque(PostProcessRenderContext context);
        void RenderAmbientOnly(PostProcessRenderContext context);
        void CompositeAmbientOnly(PostProcessRenderContext context);
        void Release();
    }

    [Serializable]
    public sealed class AmbientOcclusion
    {
        public enum Mode
        {
            SAO,
            MSVO
        }

        [Tooltip("Enables ambient occlusion.")]
        public bool enabled = false;

        public Mode mode = Mode.MSVO;

        [Tooltip("Only affects ambient lighting. This mode is only available with the Deferred rendering path and HDR rendering. Objects rendered with the Forward rendering path won't get any ambient occlusion.")]
        public bool ambientOnly = false;

        // Polymorphism doesn't play well with serialization in Unity so we have to keep explicit
        // references... Would be nice to have this more dynamic to allow user-custom AO methods.
        public ScalableAO scalableAO;
        public MultiScaleVO multiScaleVO;

        IAmbientOcclusionMethod[] m_Methods;

        public AmbientOcclusion()
        {
            if (scalableAO == null) scalableAO = new ScalableAO();
            if (multiScaleVO == null) multiScaleVO = new MultiScaleVO();

            m_Methods = new IAmbientOcclusionMethod[] { scalableAO, multiScaleVO };
        }

        public bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled && Get().IsSupported(context);
        }

        public bool IsAmbientOnly(PostProcessRenderContext context)
        {
            var camera = context.camera;
            return ambientOnly
                && camera.actualRenderingPath == RenderingPath.DeferredShading
                && camera.allowHDR;
        }

        public IAmbientOcclusionMethod Get()
        {
            return m_Methods[(int)mode];
        }

        public void Release()
        {
            foreach (var m in m_Methods)
                m.Release();
        }
    }
}
