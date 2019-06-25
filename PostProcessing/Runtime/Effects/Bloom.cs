using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.PPSMobile
{
    // For now and by popular request, this bloom effect is geared toward artists so they have full
    // control over how it looks at the expense of physical correctness.
    // Eventually we will need a "true" natural bloom effect with proper energy conservation.

    /// <summary>
    /// This class holds settings for the Bloom effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(BloomRenderer), "EffectHall/Bloom")]
    public sealed class Bloom : PostProcessEffectSettings
    {
        /// <summary>
        /// The strength of the bloom filter.
        /// </summary>
        [Min(0f), Tooltip("[辉光渗透的强度]Strength of the bloom filter. Values higher than 1 will make bloom contribute more energy to the final render.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        /// <summary>
        /// Filters out pixels under this level of brightness. This value is expressed in
        /// gamma-space.
        /// </summary>
        [Min(0f), Tooltip("[亮度阈值]Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public FloatParameter threshold = new FloatParameter { value = 1f };

        /// <summary>
        /// Makes transition between under/over-threshold gradual (0 = hard threshold, 1 = soft
        /// threshold).
        /// </summary>
        [Range(0f, 1f), Tooltip("[阈值软拐点]Makes transitions between under/over-threshold gradual. 0 for a hard threshold, 1 for a soft threshold).")]
        public FloatParameter softKnee = new FloatParameter { value = 0.5f };

        /// <summary>
        /// Clamps pixels to control the bloom amount. This value is expressed in gamma-space.
        /// </summary>
        [Tooltip("[夹取像素色,可HDR]Clamps pixels to control the bloom amount. Value is in gamma-space.")]
        public FloatParameter clamp = new FloatParameter { value = 65472f };

        /// <summary>
        /// Changes extent of veiling effects in a screen resolution-independent fashion. For
        /// maximum quality stick to integer values. Because this value changes the internal
        /// iteration count, animating it isn't recommended as it may introduce small hiccups in
        /// the perceived radius.
        /// </summary>
        [Range(1f, 10f), Tooltip("[扩散]Changes the extent of veiling effects. For maximum quality, use integer values. Because this value changes the internal iteration count, You should not animating it as it may introduce issues with the perceived radius.")]
        public FloatParameter diffusion = new FloatParameter { value = 7f };

        /// <summary>
        /// Distorts the bloom to give an anamorphic look. Negative values distort vertically,
        /// positive values distort horizontally.
        /// </summary>
        [Range(-1f, 1f), Tooltip("[变形率]Distorts the bloom to give an anamorphic look. Negative values distort vertically, positive values distort horizontally.")]
        public FloatParameter anamorphicRatio = new FloatParameter { value = 0f };

        /// <summary>
        /// The tint of the Bloom filter.
        /// </summary>
#if UNITY_2018_1_OR_NEWER
        [ColorUsage(false, true), Tooltip("[颜色]Global tint of the bloom filter.")]
#else
        [ColorUsage(false, true, 0f, 8f, 0.125f, 3f), Tooltip("[颜色]Global tint of the bloom filter.")]
#endif
        public ColorParameter color = new ColorParameter { value = Color.white };

        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && intensity.value > 0f;
        }
    }

#if UNITY_2017_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    internal sealed class BloomRenderer : PostProcessEffectRenderer<Bloom>
    {
        enum Pass
        {
            Prefilter4,
            Downsample4,
            UpsampleBox,
            DebugOverlayThreshold,
            DebugOverlayBox
        }

        // [down,up]
        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 12; // Just to make sure we handle 4096 screens... Future-proof!

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
            cmd.BeginSample("PPSMobile_Bloom");

            var sheet = context.propertySheets.Get(context.resources.shaders.bloom);

            // Apply auto exposure adjustment in the prefiltering pass
            // 前置过滤,应用自动曝光 
            sheet.properties.SetTexture(ShaderIDs.AutoExposureTex, context.autoExposureTexture);

            // Negative anamorphic ratio values distort vertically - positive is horizontal
            // 变形比值为负:垂直扭曲;正:水平扭曲
            float ratio = Mathf.Clamp(settings.anamorphicRatio, -1, 1);
            float rw = ratio < 0 ? -ratio : 0f;
            float rh = ratio > 0 ?  ratio : 0f;

            // Do bloom on a half-res buffer, full-res doesn't bring much and kills performances on
            // fillrate limited platforms
            // 降采样一次，全采样带来不了多少效果，而且在带宽限制的平台上性能影响比较大
            // ratio为0时,RT最小
            int tw = Mathf.FloorToInt(context.screenWidth / (2f - rw));
            int th = Mathf.FloorToInt(context.screenHeight / (2f - rh));
            int tw_temp = tw; 

            // Determine the iteration count
            // 决定迭代次数
            int s = Mathf.Max(tw, th);
            float logs = Mathf.Log(s, 2f) + Mathf.Min(settings.diffusion.value, 10f) - 10f;
            int logs_i = Mathf.FloorToInt(logs);
            int iterations = Mathf.Clamp(logs_i, 1, k_MaxPyramidSize);
            float sampleScale = 0.5f + logs - logs_i;
            sheet.properties.SetFloat(ShaderIDs.SampleScale, sampleScale);

            // Prefiltering parameters
            // 前置过滤参数
            float lthresh = Mathf.GammaToLinearSpace(settings.threshold.value);
            float knee = lthresh * settings.softKnee.value + 1e-5f;
            var threshold = new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
            sheet.properties.SetVector(ShaderIDs.Threshold, threshold);
            float lclamp = Mathf.GammaToLinearSpace(settings.clamp.value);
            sheet.properties.SetVector(ShaderIDs.Params, new Vector4(lclamp, 0f, 0f, 0f));

            // Downsample 下采样
            var lastDown = context.source;
            for (int i = 0; i < iterations; i++)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                int pass = i == 0 ? (int)Pass.Prefilter4 : (int)Pass.Downsample4;

                context.GetScreenSpaceTemporaryRT(cmd, mipDown, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_temp, th);
                context.GetScreenSpaceTemporaryRT(cmd, mipUp, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_temp, th);
                cmd.BlitFullscreenTriangle(lastDown, mipDown, sheet, pass);

                lastDown = mipDown;
                tw_temp = tw_temp / 2;
                tw_temp = Mathf.Max(tw_temp, 1);
                th = Mathf.Max(th / 2, 1);
            }

            // Upsample 上采样
            int lastUp = m_Pyramid[iterations - 1].down;
            for (int i = iterations - 2; i >= 0; i--)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                cmd.SetGlobalTexture(ShaderIDs.BloomTex, mipDown);
                cmd.BlitFullscreenTriangle(lastUp, mipUp, sheet, (int)Pass.UpsampleBox);
                lastUp = mipUp;
            }

            var linearColor = settings.color.value.linear;
            float intensity = RuntimeUtilities.Exp2(settings.intensity.value / 10f) - 1f;
            var shaderSettings = new Vector4(sampleScale, intensity, 0.0f, iterations);

            // Debug overlays
            if (context.IsDebugOverlayEnabled(DebugOverlay.BloomThreshold))
            {
                context.PushDebugOverlay(cmd, context.source, sheet, (int)Pass.DebugOverlayThreshold);
            }
            else if (context.IsDebugOverlayEnabled(DebugOverlay.BloomBuffer))
            {
                sheet.properties.SetVector(ShaderIDs.ColorIntensity, new Vector4(linearColor.r, linearColor.g, linearColor.b, intensity));
                context.PushDebugOverlay(cmd, m_Pyramid[0].up, sheet, (int)Pass.DebugOverlayBox);
            }

            // Shader properties
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("BLOOM_LOW");
            uberSheet.properties.SetVector(ShaderIDs.Bloom_Settings, shaderSettings);
            uberSheet.properties.SetColor(ShaderIDs.Bloom_Color, linearColor);
            cmd.SetGlobalTexture(ShaderIDs.BloomTex, lastUp);

            // Cleanup
            for (int i = 0; i < iterations; i++)
            {
                if (m_Pyramid[i].down != lastUp)
                    cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                if (m_Pyramid[i].up != lastUp)
                    cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
            }

            cmd.EndSample("PPSMobile_Bloom");

            context.bloomBufferNameID = lastUp;
        }
    }
}
