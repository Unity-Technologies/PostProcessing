using System;

namespace UnityEngine.Experimental.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(BloomRenderer), "Unity/Bloom")]
    public sealed class Bloom : PostProcessEffectSettings
    {
        [Min(0f), Tooltip("Strength of the bloom filter.")]
        public FloatParameter intensity = new FloatParameter { value = 0.25f };

        [Min(0f), Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public FloatParameter threshold = new FloatParameter { value = 0f };

        [Range(0f, 1f), Tooltip("Makes transition between under/over-threshold gradual (0 = hard threshold, 1 = soft threshold).")]
        public FloatParameter softKnee = new FloatParameter { value = 0.5f };

        [Tooltip("Weighting curve applied to the upsampling pyramid. It can be used to fine-tune the bloom radius and look.")]
        public CurveParameter responseCurve = new CurveParameter
        {
            value = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 1f),
                new Keyframe(1f, 1f, 1f, 0f)
            )
        };

        [Tooltip("Dirtiness texture to add smudges or dust to the lens."), DisplayName("Texture")]
        public TextureParameter lensTexture = new TextureParameter { value = null };

        [Min(0f), Tooltip("Amount of lens dirtiness."), DisplayName("Intensity")]
        public FloatParameter lensIntensity = new FloatParameter { value = 3f };

        public override bool IsEnabledAndSupported()
        {
            return enabled.value
                && intensity.value > 0f;
        }
    }

    public sealed class BloomRenderer : PostProcessEffectRenderer<Bloom>
    {
        enum Pass
        {
            Prefilter,
            Downsample,
            Upsample
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

            var sheet = context.propertySheets.Get("Hidden/PostProcessing/Bloom");

            // Apply auto exposure adjustment in the prefiltering pass
            sheet.properties.SetTexture(Uniforms._AutoExposureTex, context.autoExposureTexture);

            // Determine the iteration count
            float logh = Mathf.Log(context.height, 2f);
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
            sheet.properties.SetFloat(Uniforms._Threshold, lthresh);

            float knee = lthresh * settings.softKnee.value + 1e-5f;
            var curve = new Vector3(lthresh - knee, knee * 2f, 0.25f / knee);
            sheet.properties.SetVector(Uniforms._Curve, curve);

            // Downsample
            var last = context.source;
            for (int i = 0; i < iterations; i++)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                int pass = i == 0 ? (int)Pass.Prefilter : (int)Pass.Downsample;

                cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear, context.sourceFormat);
                cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear, context.sourceFormat);
                cmd.BlitFullscreenTriangle(last, mipDown, sheet, pass);

                last = mipDown;
                tw /= 2; th /= 2;
            }

            // Upsample
            float responseStep = 1f / (iterations - 2);
            last = m_Pyramid[iterations - 1].down;
            for (int i = iterations - 2; i >= 0; i--)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                float response = Mathf.Max(0f, settings.responseCurve.value.Evaluate((iterations - 2 - i) * responseStep));
                sheet.properties.SetFloat(Uniforms._Response, response);
                cmd.SetGlobalTexture(Uniforms._BloomTex, mipDown);
                cmd.BlitFullscreenTriangle(last, mipUp, sheet, (int)Pass.Upsample);
                last = mipUp;
            }

            var shaderSettings = new Vector3(sampleScale, settings.intensity.value, settings.lensIntensity.value);
            var dirtTexture = settings.lensTexture.value == null
                ? RuntimeUtilities.blackTexture
                : settings.lensTexture.value;

            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("BLOOM");
            uberSheet.properties.SetVector(Uniforms._Bloom_Settings, shaderSettings);
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
