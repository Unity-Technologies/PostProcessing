using System;

namespace UnityEngine.Experimental.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(ChromaticAberrationRenderer), "Unity/Chromatic Aberration")]
    public sealed class ChromaticAberration : PostProcessEffectSettings
    {
        [Tooltip("Shift the hue of chromatic aberrations.")]
        public TextureParameter spectralLut = new TextureParameter { value = null };

        [Range(0f, 1f), Tooltip("Amount of tangential distortion.")]
        public FloatParameter intensity = new FloatParameter { value = 0.1f };

        [Tooltip("Boost performances by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms.")]
        public BoolParameter mobileOptimized = new BoolParameter { value = false };

        public override bool IsEnabledAndSupported()
        {
            return enabled.value
                && intensity.value > 0f;
        }

        public override void SetDisabledState()
        {
            intensity.value = 0f;
        }
    }

    public sealed class ChromaticAberrationRenderer : PostProcessEffectRenderer<ChromaticAberration>
    {
        Texture2D m_InternalSpectralLut;

        public override void Render(PostProcessRenderContext context)
        {
            var spectralLut = settings.spectralLut.value;

            if (spectralLut == null)
            {
                if (m_InternalSpectralLut == null)
                {
                    m_InternalSpectralLut = new Texture2D(3, 1, TextureFormat.RGB24, false)
                    {
                        name = "Chromatic Aberration Spectrum Lookup",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };

                    m_InternalSpectralLut.SetPixels(new []
                    {
                        new Color(1f, 0f, 0f),
                        new Color(0f, 1f, 0f),
                        new Color(0f, 0f, 1f)
                    });

                    m_InternalSpectralLut.Apply();
                }

                spectralLut = m_InternalSpectralLut;
            }
            
            var sheet = context.uberSheet;
            sheet.EnableKeyword(settings.mobileOptimized
                ? "CHROMATIC_ABERRATION_LOW"
                : "CHROMATIC_ABERRATION"
            );
            sheet.properties.SetFloat(Uniforms._ChromaticAberration_Amount, settings.intensity * 0.05f);
            sheet.properties.SetTexture(Uniforms._ChromaticAberration_SpectralLut, spectralLut);
        }

        public override void Release()
        {
            RuntimeUtilities.Destroy(m_InternalSpectralLut);
            m_InternalSpectralLut = null;
        }
    }
}
