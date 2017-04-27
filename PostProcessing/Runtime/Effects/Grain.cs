using System;

namespace UnityEngine.Experimental.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(GrainRenderer), "Unity/Grain")]
    public sealed class Grain : PostProcessEffectSettings
    {
        [Tooltip("Enable the use of colored grain.")]
        public BoolParameter colored = new BoolParameter { value = true };

        [Range(0f, 1f), Tooltip("Grain strength. Higher means more visible grain.")]
        public FloatParameter intensity = new FloatParameter { value = 0.5f };

        [Range(0.3f, 3f), Tooltip("Grain particle size.")]
        public FloatParameter size = new FloatParameter { value = 1f };

        [Range(0f, 1f), Tooltip("Controls the noisiness response curve based on scene luminance. Lower values mean less noise in dark areas.")]
        public FloatParameter lumContrib = new FloatParameter { value = 0.8f };

        public override bool IsEnabledAndSupported()
        {
            return enabled.value
                && intensity.value > 0f
                && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
        }
    }
    
    public sealed class GrainRenderer : PostProcessEffectRenderer<Grain>
    {
        RenderTexture m_GrainLookupRT;

        public override void Render(PostProcessRenderContext context)
        {
#if POSTFX_DEBUG_STATIC_GRAIN
            // Chosen by a fair dice roll
            float time = 4f;
            float rndOffsetX = 0f;
            float rndOffsetY = 0f;
#else
            float time = Time.realtimeSinceStartup;
            float rndOffsetX = Random.value;
            float rndOffsetY = Random.value;
#endif

            // Generate the grain lut for the current frame first
            if (m_GrainLookupRT == null || !m_GrainLookupRT.IsCreated())
            {
                RuntimeUtilities.Destroy(m_GrainLookupRT);

                m_GrainLookupRT = new RenderTexture(192, 192, 0, RenderTextureFormat.ARGBHalf)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Repeat,
                    anisoLevel = 0,
                    name = "Grain Lookup Texture"
                };

                m_GrainLookupRT.Create();
            }
            
            var sheet = context.propertySheets.Get("Hidden/PostProcessing/GrainBaker");
            sheet.properties.Clear();
            sheet.properties.SetFloat(Uniforms._Phase, time / 20f);

            context.command.BeginSample("GrainLookup");
            context.command.BlitFullscreenTriangle((Texture)null, m_GrainLookupRT, sheet, settings.colored.value ? 1 : 0);
            context.command.EndSample("GrainLookup");

            // Send everything to the uber shader
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("GRAIN");
            uberSheet.properties.SetTexture(Uniforms._GrainTex, m_GrainLookupRT);
            uberSheet.properties.SetVector(Uniforms._Grain_Params1, new Vector2(settings.lumContrib.value, settings.intensity.value * 20f));
            uberSheet.properties.SetVector(Uniforms._Grain_Params2, new Vector4((float)context.width / (float)m_GrainLookupRT.width / settings.size.value, (float)context.height / (float)m_GrainLookupRT.height / settings.size.value, rndOffsetX, rndOffsetY));
        }

        public override void Release()
        {
            RuntimeUtilities.Destroy(m_GrainLookupRT);
            m_GrainLookupRT = null;
        }
    }
}
