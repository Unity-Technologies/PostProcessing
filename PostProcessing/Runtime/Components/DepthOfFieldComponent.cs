using UnityEngine.Rendering;

namespace UnityEngine.PostProcessing
{
    public sealed class DepthOfFieldComponent : PostProcessingComponentRenderTexture<DepthOfFieldModel>
    {
        static class Uniforms
        {
            internal static readonly int _DepthOfFieldTex = Shader.PropertyToID("_DepthOfFieldTex");
            internal static readonly int _Distance = Shader.PropertyToID("_Distance");
            internal static readonly int _LensCoeff = Shader.PropertyToID("_LensCoeff");
            internal static readonly int _MaxCoC = Shader.PropertyToID("_MaxCoC");
            internal static readonly int _RcpMaxCoC = Shader.PropertyToID("_RcpMaxCoC");
            internal static readonly int _RcpAspect = Shader.PropertyToID("_RcpAspect");
            internal static readonly int _DejitteredDepth = Shader.PropertyToID("_DejitteredDepth");
        }

        const string k_ShaderString = "Hidden/Post FX/Depth Of Field";

        public override bool active
        {
            get
            {
                return model.enabled
                       && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                       && !context.interrupted;
            }
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        // Height of the 35mm full-frame format (36mm x 24mm)
        const float k_FilmHeight = 0.024f;

        float CalculateFocalLength()
        {
            var settings = model.settings;

            if (!settings.useCameraFov)
                return settings.focalLength / 1000f;

            float fov = context.camera.fieldOfView * Mathf.Deg2Rad;
            return 0.5f * k_FilmHeight / Mathf.Tan(0.5f * fov);
        }

        float CalculateMaxCoCRadius(int screenHeight)
        {
            // Estimate the allowable maximum radius of CoC from the kernel
            // size (the equation below was empirically derived).
            float radiusInPixels = (float)model.settings.kernelSize * 4f + 10f;

            // Applying a 5% limit to the CoC radius to keep the size of
            // TileMax/NeighborMax small enough.
            return Mathf.Min(0.05f, radiusInPixels / screenHeight);
        }

        public void Prepare(RenderTexture source, Material uberMaterial, RenderTexture dejitteredDepth)
        {
            var settings = model.settings;

            // Material setup
            var material = context.materialFactory.Get(k_ShaderString);
            material.shaderKeywords = null;

            var s1 = settings.focusDistance;
            var f = CalculateFocalLength();
            s1 = Mathf.Max(s1, f);
            material.SetFloat(Uniforms._Distance, s1);

            var coeff = f * f / (settings.aperture * (s1 - f) * k_FilmHeight * 2);
            material.SetFloat(Uniforms._LensCoeff, coeff);

            var maxCoC = CalculateMaxCoCRadius(source.height);
            material.SetFloat(Uniforms._MaxCoC, maxCoC);
            material.SetFloat(Uniforms._RcpMaxCoC, 1f / maxCoC);

            var rcpAspect = (float)source.height / source.width;
            material.SetFloat(Uniforms._RcpAspect, rcpAspect);

            if (dejitteredDepth != null)
            {
                material.SetTexture(Uniforms._DejitteredDepth, dejitteredDepth);
                material.EnableKeyword("DEJITTER_DEPTH");
            }

            // Pass #1 - Downsampling, prefiltering and CoC calculation
            var rt1 = context.renderTextureFactory.Get(context.width / 2, context.height / 2, 0, RenderTextureFormat.ARGBHalf, filterMode: FilterMode.Point);
            Graphics.Blit(source, rt1, material, 0);

            // Pass #2 - Bokeh simulation
            var rt2 = context.renderTextureFactory.Get(context.width / 2, context.height / 2, 0, RenderTextureFormat.ARGBHalf, filterMode: FilterMode.Bilinear);
            Graphics.Blit(rt1, rt2, material, 1 + (int)settings.kernelSize);

            context.renderTextureFactory.Release(rt1);

            uberMaterial.SetTexture(Uniforms._DepthOfFieldTex, rt2);
            uberMaterial.EnableKeyword("DEPTH_OF_FIELD");
        }
    }
}
