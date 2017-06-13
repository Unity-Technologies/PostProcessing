using System;

namespace UnityEngine.Experimental.PostProcessing
{
    // For now and by popular request, this bloom effect is geared toward artists so they have full
    // control over how it looks at the expense of physical correctness.
    // Eventually we will need a "true" natural bloom effect with proper energy conservation.

    [Serializable]
    [PostProcess(typeof(BloomRenderer), "Unity/Bloom")]
    public sealed class Bloom : PostProcessEffectSettings
    {
        [Min(0f), Tooltip("Strength of the bloom filter. Values higher than 1 will make bloom contribute more energy to the final render. Keep this under or equal to 1 if you want energy conservation.")]
        public FloatParameter intensity = new FloatParameter { value = 1f };

        [Min(0f), Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public FloatParameter threshold = new FloatParameter { value = 1f };

        [Range(0f, 1f), Tooltip("Makes transition between under/over-threshold gradual (0 = hard threshold, 1 = soft threshold).")]
        public FloatParameter softKnee = new FloatParameter { value = 0.5f };

        [Range(1f, 10f), Tooltip("Changes the extent of veiling effects. For maximum quality stick to integer values. Because this value changes the internal iteration count, animating it isn't recommended as it may introduce small hiccups in the perceived radius.")]
        public FloatParameter diffusion = new FloatParameter { value = 6f };

        [ColorUsage(false, true, 0f, 8f, 0.125f, 3f), Tooltip("Global tint of the bloom filter.")]
        public ColorParameter color = new ColorParameter { value = Color.white };

        [Tooltip("Boost performances by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms.")]
        public BoolParameter mobileOptimized = new BoolParameter { value = false };

        [Tooltip("Dirtiness texture to add smudges or dust to the lens."), DisplayName("Texture")]
        public TextureParameter lensTexture = new TextureParameter { value = null };

        [Min(0f), Tooltip("Amount of lens dirtiness."), DisplayName("Intensity")]
        public FloatParameter lensIntensity = new FloatParameter { value = 1f };

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

    public sealed class BloomRenderer : PostProcessEffectRenderer<Bloom>
    {
        enum Pass
        {
            Prefilter13,
            Prefilter4,
            Downsample13,
            Downsample4,
            UpsampleTent,
            UpsampleBox
        }

        // [down,up]
        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16; // Just to make sure we handle 64k screens... Future-proof!

        struct Level
        {
            internal int down;
            internal int up;
        }

        public override void Init()
        {
            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down = Shader.PropertyToID("_BloomMipDown" + i),
                    up = Shader.PropertyToID("_BloomMipUp" + i)
                };
            }
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("BloomPyramid");

            var sheet = context.propertySheets.Get(context.resources.shaders.bloom);

            // Apply auto exposure adjustment in the prefiltering pass
            sheet.properties.SetTexture(Uniforms._AutoExposureTex, context.autoExposureTexture);

            // Determine the iteration count
            float logh = Mathf.Log(context.height, 2f) + settings.diffusion.value - 10f;
            int logh_i = Mathf.FloorToInt(logh);
            int iterations = Mathf.Clamp(logh_i, 1, k_MaxPyramidSize);
            float sampleScale = 0.5f + logh - logh_i;
            sheet.properties.SetFloat(Uniforms._SampleScale, sampleScale);

            // Do bloom on a half-res buffer, full-res doesn't bring much and kills performances on
            // fillrate limited platforms
            int tw = context.width / 2;
            int th = context.height / 2;

            // Prefiltering parameters
            float lthresh = Mathf.GammaToLinearSpace(settings.threshold.value);
            float knee = lthresh * settings.softKnee.value + 1e-5f;
            var threshold = new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
            sheet.properties.SetVector(Uniforms._Threshold, threshold);
            
            int qualityOffset = settings.mobileOptimized ? 1 : 0;

            // Downsample
            var last = context.source;
            for (int i = 0; i < iterations; i++)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                int pass = i == 0
                    ? (int)Pass.Prefilter13 + qualityOffset
                    : (int)Pass.Downsample13 + qualityOffset;

                cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear, context.sourceFormat);
                cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear, context.sourceFormat);
                cmd.BlitFullscreenTriangle(last, mipDown, sheet, pass);

                last = mipDown;
                tw /= 2; th /= 2;
            }

            // Upsample
            last = m_Pyramid[iterations - 1].down;
            for (int i = iterations - 2; i >= 0; i--)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                cmd.SetGlobalTexture(Uniforms._BloomTex, mipDown);
                cmd.BlitFullscreenTriangle(last, mipUp, sheet, (int)Pass.UpsampleTent + qualityOffset);
                last = mipUp;
            }

            var shaderSettings = new Vector4(
                sampleScale,
                RuntimeUtilities.Exp2(settings.intensity.value / 10f) - 1f,
                settings.lensIntensity.value,
                iterations
            );

            var dirtTexture = settings.lensTexture.value == null
                ? RuntimeUtilities.blackTexture
                : settings.lensTexture.value;

            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("BLOOM");
            uberSheet.properties.SetVector(Uniforms._Bloom_Settings, shaderSettings);
            uberSheet.properties.SetColor(Uniforms._Bloom_Color, settings.color.value.linear);
            uberSheet.properties.SetTexture(Uniforms._Bloom_DirtTex, dirtTexture);
            cmd.SetGlobalTexture(Uniforms._BloomTex, m_Pyramid[0].up);

            // Cleanup
            for (int i = 0; i < iterations; i++)
            {
                cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
            }

            cmd.EndSample("BloomPyramid");
        }
    }
}
