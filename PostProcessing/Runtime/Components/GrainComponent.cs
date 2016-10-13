namespace UnityEngine.PostProcessing
{
    public sealed class GrainComponent : PostProcessingComponentRenderTexture<GrainModel>
    {
        static class Uniforms
        {
            internal static readonly int _Grain_Params1 = Shader.PropertyToID("_Grain_Params1");
            internal static readonly int _Grain_Params2 = Shader.PropertyToID("_Grain_Params2");
            internal static readonly int _GrainTex = Shader.PropertyToID("_GrainTex");
            internal static readonly int _Params = Shader.PropertyToID("_Params");
        }

        public override bool active
        {
            get
            {
                return model.enabled
                       && model.settings.intensity > 0f
                       && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
            }
        }

        RenderTexture m_GrainLookupRT;

        public override void OnDisable()
        {
            GraphicsUtils.Destroy(m_GrainLookupRT);
            m_GrainLookupRT = null;
        }

        public override void Prepare(Material uberMaterial)
        {
            var settings = model.settings;

            uberMaterial.EnableKeyword("GRAIN");

#if POSTFX_DEBUG_STATIC_GRAIN
            // Chosen by a fair dice roll
            float time = 4f;
            float rndXOffset = 0f;
            float rndYOffset = 0f;
#else
            float time = Time.realtimeSinceStartup;
            float rndXOffset = Random.value;
            float rndYOffset = Random.value;
#endif

            // Used for sample rotation in Filmic mode and position offset in Fast mode
            const float kRotationOffset = 1.425f;
            float c = Mathf.Cos(time + kRotationOffset);
            float s = Mathf.Sin(time + kRotationOffset);

            if (m_GrainLookupRT == null || !m_GrainLookupRT.IsCreated())
            {
                GraphicsUtils.Destroy(m_GrainLookupRT);

                m_GrainLookupRT = new RenderTexture(192, 192, 0, RenderTextureFormat.ARGBHalf)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Repeat,
                    anisoLevel = 0,
                    name = "Grain Lookup Texture"
                };

                m_GrainLookupRT.Create();
            }

            var grainMaterial = context.materialFactory.Get("Hidden/Post FX/Grain Generator");
            grainMaterial.SetVector(Uniforms._Params, new Vector4(settings.size, time / 20f, c, s));

            Graphics.Blit((Texture)null, m_GrainLookupRT, grainMaterial, settings.colored ? 1 : 0);

            uberMaterial.SetTexture(Uniforms._GrainTex, m_GrainLookupRT);

            float intensity = settings.intensity * 0.25f;

            if (!settings.colored)
            {
                uberMaterial.SetVector(Uniforms._Grain_Params1, new Vector4(settings.luminanceContribution, intensity, intensity, intensity));
            }
            else
            {
                uberMaterial.SetVector(Uniforms._Grain_Params1, new Vector4(settings.luminanceContribution, settings.weightR * intensity, settings.weightG * intensity, settings.weightB * intensity));
            }

            uberMaterial.SetVector(Uniforms._Grain_Params2, new Vector4((float)context.width / (float)m_GrainLookupRT.width, (float)context.height / (float)m_GrainLookupRT.height, rndXOffset, rndYOffset));
        }
    }
}
