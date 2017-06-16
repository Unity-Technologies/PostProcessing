using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.PostProcessing
{
    public enum GradingMode
    {
        LowDefinitionRange,
        HighDefinitionRange,
        External
    }

    public enum Tonemapper
    {
        None,

        // Neutral tonemapper (based off John Hable's & Jim Hejl's work)
        Neutral,

        // ACES Filmic reference tonemapper (custom approximation)
        ACES,

        // Custom artist-friendly curve
        Custom
    }

    [Serializable]
    public sealed class GradingModeParameter : ParameterOverride<GradingMode> {}

    [Serializable]
    public sealed class TonemapperParameter : ParameterOverride<Tonemapper> {}

    [Serializable]
    [PostProcess(typeof(ColorGradingRenderer), "Unity/Color Grading")]
    public sealed class ColorGrading : PostProcessEffectSettings
    {
        [DisplayName("Mode"), Tooltip("Select a color grading mode that fits your dynamic range and workflow. Use HDR if your camera is set to render in HDR and your target platform supports it. Use LDR for low-end mobiles or devices that don't support HDR. Use Custom HDR if you prefer authoring a Log LUT in external softwares.")]
        public GradingModeParameter gradingMode = new GradingModeParameter { value = GradingMode.HighDefinitionRange };

        [DisplayName("Lookup Texture"), Tooltip("")]
        public TextureParameter externalLut = new TextureParameter { value = null };

        [DisplayName("Mode"), Tooltip("Select a tonemapping algorithm to use at the end of the color grading process.")]
        public TonemapperParameter tonemapper = new TonemapperParameter { value = Tonemapper.None };

        [DisplayName("Toe Strength"), Range(0f, 1f), Tooltip("Affects the transition between the toe and the mid section of the curve. A value of 0 means no toe, a value of 1 means a very hard transition.")]
        public FloatParameter toneCurveToeStrength = new FloatParameter { value = 0f };

        [DisplayName("Toe Length"), Range(0f, 1f), Tooltip("Affects how much of the dynamic range is in the toe. With a small value, the toe will be very short and quickly transition into the linear section, and with a longer value having a longer toe.")]
        public FloatParameter toneCurveToeLength = new FloatParameter { value = 0.5f };

        [DisplayName("Shoulder Strength"), Range(0f, 1f), Tooltip("Affects the transition between the mid section and the shoulder of the curve. A value of 0 means no shoulder, a value of 1 means a very hard transition.")]
        public FloatParameter toneCurveShoulderStrength = new FloatParameter { value = 0f };

        [DisplayName("Shoulder Length"), Min(0f), Tooltip("Affects how many F-stops (EV) to add to the dynamic range of the curve.")]
        public FloatParameter toneCurveShoulderLength = new FloatParameter { value = 0.5f };

        [DisplayName("Shoulder Angle"), Range(0f, 1f), Tooltip("Affects how much overshoot to add to the shoulder.")]
        public FloatParameter toneCurveShoulderAngle = new FloatParameter { value = 0f };

        [DisplayName("Gamma"), Min(0.001f), Tooltip("")]
        public FloatParameter toneCurveGamma = new FloatParameter { value = 1f };

        [DisplayName("Lookup Texture"), Tooltip("Custom log-space lookup texture (strip format, e.g. 1024x32). EXR format is highly recommended or precision will be heavily degraded. Refer to the documentation for more information about how to create such a Lut.")]
        public TextureParameter logLut = new TextureParameter { value = null };

        [DisplayName("Lookup Texture"), Tooltip("Custom lookup texture (strip format, e.g. 256x16) to apply before the rest of the color grading operators. If none is provided, a neutral one will be generated internally.")]
        public TextureParameter ldrLut = new TextureParameter { value = null }; // LDR only

        [DisplayName("Temperature"), Range(-100f, 100f), Tooltip("Sets the white balance to a custom color temperature.")]
        public FloatParameter temperature = new FloatParameter { value = 0f };

        [DisplayName("Tint"), Range(-100f, 100f), Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
        public FloatParameter tint = new FloatParameter { value = 0f };

        [DisplayName("Color Filter"), ColorUsage(false, true, 0f, 8f, 0.125f, 3f), Tooltip("Tint the render by multiplying a color.")]
        public ColorParameter colorFilter = new ColorParameter { value = Color.white };

        [DisplayName("Hue Shift"), Range(-180f, 180f), Tooltip("Shift the hue of all colors.")]
        public FloatParameter hueShift = new FloatParameter { value = 0f };

        [DisplayName("Saturation"), Range(-100f, 100f), Tooltip("Pushes the intensity of all colors.")]
        public FloatParameter saturation = new FloatParameter { value = 0f };

        [DisplayName("Brightness"), Range(-100f, 100f), Tooltip("Makes the image brighter or darker.")]
        public FloatParameter brightness = new FloatParameter { value = 0f }; // LDR only

        [DisplayName("Post-exposure (EV)"), Tooltip("Adjusts the overall exposure of the scene in EV units. This is applied after HDR effect and right before tonemapping so it won't affect previous effects in the chain.")]
        public FloatParameter postExposure = new FloatParameter { value = 0f }; // HDR only

        [DisplayName("Contrast"), Range(-100f, 100f), Tooltip("Expands or shrinks the overall range of tonal values.")]
        public FloatParameter contrast = new FloatParameter { value = 0f };

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("Modify influence of the red channel in the overall mix.")]
        public FloatParameter mixerRedOutRedIn = new FloatParameter { value = 100f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("Modify influence of the green channel in the overall mix.")]
        public FloatParameter mixerRedOutGreenIn = new FloatParameter { value = 0f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("Modify influence of the blue channel in the overall mix.")]
        public FloatParameter mixerRedOutBlueIn = new FloatParameter { value = 0f };

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("Modify influence of the red channel in the overall mix.")]
        public FloatParameter mixerGreenOutRedIn = new FloatParameter { value = 0f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("Modify influence of the green channel in the overall mix.")]
        public FloatParameter mixerGreenOutGreenIn = new FloatParameter { value = 100f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("Modify influence of the blue channel in the overall mix.")]
        public FloatParameter mixerGreenOutBlueIn = new FloatParameter { value = 0f };

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("Modify influence of the red channel in the overall mix.")]
        public FloatParameter mixerBlueOutRedIn = new FloatParameter { value = 0f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("Modify influence of the green channel in the overall mix.")]
        public FloatParameter mixerBlueOutGreenIn = new FloatParameter { value = 0f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("Modify influence of the blue channel in the overall mix.")]
        public FloatParameter mixerBlueOutBlueIn = new FloatParameter { value = 100f };
        
        [DisplayName("Lift"), Tooltip("Controls the darkest portions of the render."), Trackball(TrackballAttribute.Mode.Lift)]
        public Vector4Parameter lift = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };
        
        [DisplayName("Gamma"), Tooltip("Power function that controls midrange tones."), Trackball(TrackballAttribute.Mode.Gamma)]
        public Vector4Parameter gamma = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };
        
        [DisplayName("Gain"), Tooltip("Controls the lightest portions of the render."), Trackball(TrackballAttribute.Mode.Gain)]
        public Vector4Parameter gain = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };

        public SplineParameter masterCurve   = new SplineParameter { value = new Spline(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public SplineParameter redCurve      = new SplineParameter { value = new Spline(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public SplineParameter greenCurve    = new SplineParameter { value = new Spline(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public SplineParameter blueCurve     = new SplineParameter { value = new Spline(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public SplineParameter hueVsHueCurve = new SplineParameter { value = new Spline(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };
        public SplineParameter hueVsSatCurve = new SplineParameter { value = new Spline(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };
        public SplineParameter satVsSatCurve = new SplineParameter { value = new Spline(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };
        public SplineParameter lumVsSatCurve = new SplineParameter { value = new Spline(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };

        public override bool IsEnabledAndSupported()
        {
            if (gradingMode.value == GradingMode.HighDefinitionRange || gradingMode.value == GradingMode.External)
            {
                if (!SystemInfo.supports3DRenderTextures || !SystemInfo.supportsComputeShaders)
                    return false;
            }

            return enabled.value;
        }
    }

    public sealed class ColorGradingRenderer : PostProcessEffectRenderer<ColorGrading>
    {
        enum Pass
        {
            LutGenLDRFromScratch,
            LutGenLDR,
        }

        Texture2D m_GradingCurves;
        readonly Color[] m_Pixels = new Color[Spline.k_Precision * 2]; // Avoids GC stress

        RenderTexture m_InternalLdrLut;
        RenderTexture m_InternalLogLut;
        const int k_Lut2DSize = 32;
        const int k_Lut3DSize = 33;

        readonly HableCurve m_HableCurve = new HableCurve();

        public override void Render(PostProcessRenderContext context)
        {
            switch (settings.gradingMode.value)
            {
                case GradingMode.External: RenderExternalPipeline(context);
                    break;
                case GradingMode.LowDefinitionRange: RenderLDRPipeline(context);
                    break;
                case GradingMode.HighDefinitionRange: RenderHDRPipeline(context);
                    break;
            }
        }

        // Do color grading using an externally authored 3D lut; it requires Texture3D support and
        // compute shaders in case blending is required - Desktop / Consoles / Some high-end mobiles
        void RenderExternalPipeline(PostProcessRenderContext context)
        {
            var lut = settings.externalLut.value;

            if (lut == null)
                return;

            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_HDR");
            uberSheet.properties.SetTexture(Uniforms._Lut3D, lut);
            uberSheet.properties.SetVector(Uniforms._Lut3D_Params, new Vector2(1f / lut.width, lut.width - 1f));
            uberSheet.properties.SetFloat(Uniforms._PostExposure, RuntimeUtilities.Exp2(settings.postExposure.value));
            context.logLut = lut;
        }

        // HDR color pipeline is rendered to a 3D lut; it requires Texture3D & compute shaders
        // support - Desktop / Consoles / Some high-end mobiles
        void RenderHDRPipeline(PostProcessRenderContext context)
        {
            // Unfortunately because AnimationCurve doesn't implement GetHashCode and we don't have
            // any reliable way to figure out if a curve data is different from another one we can't
            // skip regenerating the Lut if nothing has changed. So it has to be done on every
            // frame...
            // It's not a very expensive operation anyway (we're talking about filling a 33x33x33
            // Lut on the GPU) but every little thing helps, especially on mobile.
            {
                CheckInternalLogLut();

                // Lut setup
                var compute = context.resources.computeShaders.lut3DBaker;
                int kernel = 0;

                switch (settings.tonemapper.value)
                {
                    case Tonemapper.None: kernel = compute.FindKernel("KGenLut3D_NoTonemap");
                        break;
                    case Tonemapper.Neutral: kernel = compute.FindKernel("KGenLut3D_NeutralTonemap");
                        break;
                    case Tonemapper.ACES: kernel = compute.FindKernel("KGenLut3D_AcesTonemap");
                        break;
                    case Tonemapper.Custom: kernel = compute.FindKernel("KGenLut3D_CustomTonemap");
                        break;
                }

                int groupSize = Mathf.CeilToInt(k_Lut3DSize / 8f);
                var cmd = context.command;
                cmd.SetComputeTextureParam(compute, kernel, "_Output", m_InternalLogLut);
                cmd.SetComputeVectorParam(compute, "_Size", new Vector4(k_Lut3DSize, 1f / (k_Lut3DSize - 1f), 0f, 0f));

                var colorBalance = ColorUtilities.ComputeColorBalance(settings.temperature.value, settings.tint.value);
                cmd.SetComputeVectorParam(compute, "_ColorBalance", colorBalance);
                cmd.SetComputeVectorParam(compute, "_ColorFilter", settings.colorFilter.value);

                float hue = settings.hueShift.value / 360f;         // Remap to [-0.5;0.5]
                float sat = settings.saturation.value / 100f + 1f;  // Remap to [0;2]
                float con = settings.contrast.value / 100f + 1f;    // Remap to [0;2]
                cmd.SetComputeVectorParam(compute, "_HueSatCon", new Vector4(hue, sat, con, 0f));

                var channelMixerR = new Vector4(settings.mixerRedOutRedIn, settings.mixerRedOutGreenIn, settings.mixerRedOutBlueIn, 0f);
                var channelMixerG = new Vector4(settings.mixerGreenOutRedIn, settings.mixerGreenOutGreenIn, settings.mixerGreenOutBlueIn, 0f);
                var channelMixerB = new Vector4(settings.mixerBlueOutRedIn, settings.mixerBlueOutGreenIn, settings.mixerBlueOutBlueIn, 0f);
                cmd.SetComputeVectorParam(compute, "_ChannelMixerRed", channelMixerR / 100f); // Remap to [-2;2]
                cmd.SetComputeVectorParam(compute, "_ChannelMixerGreen", channelMixerG / 100f);
                cmd.SetComputeVectorParam(compute, "_ChannelMixerBlue", channelMixerB / 100f);

                var lift = ColorUtilities.ColorToLift(settings.lift.value * 0.2f);
                var gain = ColorUtilities.ColorToGain(settings.gain.value * 0.8f);
                var invgamma = ColorUtilities.ColorToInverseGamma(settings.gamma.value * 0.8f);
                cmd.SetComputeVectorParam(compute, "_Lift", new Vector4(lift.x, lift.y, lift.z, 0f));
                cmd.SetComputeVectorParam(compute, "_InvGamma", new Vector4(invgamma.x, invgamma.y, invgamma.z, 0f));
                cmd.SetComputeVectorParam(compute, "_Gain", new Vector4(gain.x, gain.y, gain.z, 0f));

                cmd.SetComputeTextureParam(compute, kernel, "_Curves", GetCurveTexture(true));

                if (settings.tonemapper.value == Tonemapper.Custom)
                {
                    m_HableCurve.Init(
                        settings.toneCurveToeStrength.value,
                        settings.toneCurveToeLength.value,
                        settings.toneCurveShoulderStrength.value,
                        settings.toneCurveShoulderLength.value,
                        settings.toneCurveShoulderAngle.value,
                        settings.toneCurveGamma.value
                    );

                    var curve = new Vector4(m_HableCurve.inverseWhitePoint, m_HableCurve.x0, m_HableCurve.x1, 0f);
                    cmd.SetComputeVectorParam(compute, "_CustomToneCurve", curve);
                    
                    var toe = m_HableCurve.segments[0];
                    var mid = m_HableCurve.segments[1];
                    var sho = m_HableCurve.segments[2];
                    var toeSegmentA = new Vector4(toe.offsetX, toe.offsetY, toe.scaleX, toe.scaleY);
                    var toeSegmentB = new Vector4(toe.lnA, toe.B, 0f, 0f);
                    var midSegmentA = new Vector4(mid.offsetX, mid.offsetY, mid.scaleX, mid.scaleY);
                    var midSegmentB = new Vector4(mid.lnA, mid.B, 0f, 0f);
                    var shoSegmentA = new Vector4(sho.offsetX, sho.offsetY, sho.scaleX, sho.scaleY);
                    var shoSegmentB = new Vector4(sho.lnA, sho.B, 0f, 0f);
                    cmd.SetComputeVectorParam(compute, "_ToeSegmentA", toeSegmentA);
                    cmd.SetComputeVectorParam(compute, "_ToeSegmentB", toeSegmentB);
                    cmd.SetComputeVectorParam(compute, "_MidSegmentA", midSegmentA);
                    cmd.SetComputeVectorParam(compute, "_MidSegmentB", midSegmentB);
                    cmd.SetComputeVectorParam(compute, "_ShoSegmentA", shoSegmentA);
                    cmd.SetComputeVectorParam(compute, "_ShoSegmentB", shoSegmentB);
                }

                // Generate the lut
                context.command.BeginSample("HdrColorGradingLut");
                cmd.DispatchCompute(compute, kernel, groupSize, groupSize, groupSize);
                context.command.EndSample("HdrColorGradingLut");
            }

            var lut = m_InternalLogLut;
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_HDR");
            uberSheet.properties.SetTexture(Uniforms._Lut3D, lut);
            uberSheet.properties.SetVector(Uniforms._Lut3D_Params, new Vector2(1f / lut.width, lut.width - 1f));
            uberSheet.properties.SetFloat(Uniforms._PostExposure, RuntimeUtilities.Exp2(settings.postExposure.value));

            context.logLut = lut;
        }

        // LDR color pipeline is rendered to a 2D strip lut (works on every platform)
        void RenderLDRPipeline(PostProcessRenderContext context)
        {
            // For the same reasons as in RenderHDRPipeline, regen LUT on evey frame
            {
                CheckInternalLdrLut();

                // Lut setup
                var lutSheet = context.propertySheets.Get(context.resources.shaders.lut2DBaker);
                lutSheet.ClearKeywords();

                lutSheet.properties.SetVector(Uniforms._Lut2D_Params, new Vector4(k_Lut2DSize, 0.5f / (k_Lut2DSize * k_Lut2DSize), 0.5f / k_Lut2DSize, k_Lut2DSize / (k_Lut2DSize - 1f)));

                var colorBalance = ColorUtilities.ComputeColorBalance(settings.temperature.value, settings.tint.value);
                lutSheet.properties.SetVector(Uniforms._ColorBalance, colorBalance);
                lutSheet.properties.SetVector(Uniforms._ColorFilter, settings.colorFilter.value);

                float hue = settings.hueShift.value / 360f;         // Remap to [-0.5;0.5]
                float sat = settings.saturation.value / 100f + 1f;  // Remap to [0;2]
                float con = settings.contrast.value / 100f + 1f;    // Remap to [0;2]
                lutSheet.properties.SetVector(Uniforms._HueSatCon, new Vector3(hue, sat, con));

                var channelMixerR = new Vector3(settings.mixerRedOutRedIn, settings.mixerRedOutGreenIn, settings.mixerRedOutBlueIn);
                var channelMixerG = new Vector3(settings.mixerGreenOutRedIn, settings.mixerGreenOutGreenIn, settings.mixerGreenOutBlueIn);
                var channelMixerB = new Vector3(settings.mixerBlueOutRedIn, settings.mixerBlueOutGreenIn, settings.mixerBlueOutBlueIn);
                lutSheet.properties.SetVector(Uniforms._ChannelMixerRed, channelMixerR / 100f);            // Remap to [-2;2]
                lutSheet.properties.SetVector(Uniforms._ChannelMixerGreen, channelMixerG / 100f);
                lutSheet.properties.SetVector(Uniforms._ChannelMixerBlue, channelMixerB / 100f);

                var lift = ColorUtilities.ColorToLift(settings.lift.value);
                var gain = ColorUtilities.ColorToGain(settings.gain.value);
                var invgamma = ColorUtilities.ColorToInverseGamma(settings.gamma.value);
                lutSheet.properties.SetVector(Uniforms._Lift, lift);
                lutSheet.properties.SetVector(Uniforms._InvGamma, invgamma);
                lutSheet.properties.SetVector(Uniforms._Gain, gain);

                lutSheet.properties.SetFloat(Uniforms._Brightness, (settings.brightness.value + 100f) / 100f);
                lutSheet.properties.SetTexture(Uniforms._Curves, GetCurveTexture(false));

                // Generate the lut
                context.command.BeginSample("LdrColorGradingLut");
                var userLut = settings.ldrLut.value;
                if (userLut == null)
                    context.command.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_InternalLdrLut, lutSheet, (int)Pass.LutGenLDRFromScratch);
                else
                    context.command.BlitFullscreenTriangle(userLut, m_InternalLdrLut, lutSheet, (int)Pass.LutGenLDR);
                context.command.EndSample("LdrColorGradingLut");
            }

            var lut = m_InternalLdrLut;
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_LDR");
            uberSheet.properties.SetVector(Uniforms._Lut2D_Params, new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
            uberSheet.properties.SetTexture(Uniforms._Lut2D, lut);
        }

        void CheckInternalLogLut()
        {
            // Check internal lut state, (re)create it if needed
            if (m_InternalLogLut == null || !m_InternalLogLut.IsCreated())
            {
                RuntimeUtilities.Destroy(m_InternalLogLut);

                var format = GetLutFormat();
                m_InternalLogLut = new RenderTexture(k_Lut3DSize, k_Lut3DSize, 0, format, RenderTextureReadWrite.Linear)
                {
                    name = "Color Grading Log Lut",
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    enableRandomWrite = true,
                    volumeDepth = k_Lut3DSize,
                    dimension = TextureDimension.Tex3D,
                    autoGenerateMips = false,
                    useMipMap = false
                };
                m_InternalLogLut.Create();
            }
        }

        void CheckInternalLdrLut()
        {
            // Check internal lut state, (re)create it if needed
            if (m_InternalLdrLut == null || !m_InternalLdrLut.IsCreated())
            {
                RuntimeUtilities.Destroy(m_InternalLdrLut);

                var format = GetLutFormat();
                m_InternalLdrLut = new RenderTexture(k_Lut2DSize * k_Lut2DSize, k_Lut2DSize, 0, format, RenderTextureReadWrite.Linear)
                {
                    name = "Color Grading Ldr Lut",
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    autoGenerateMips = false,
                    useMipMap = false
                };
                m_InternalLdrLut.Create();
            }
        }

        Texture2D GetCurveTexture(bool hdr)
        {
            if (m_GradingCurves == null)
            {
                var format = GetCurveFormat();
                m_GradingCurves = new Texture2D(Spline.k_Precision, 2, format, false, true)
                {
                    name = "Internal Curves Texture",
                    hideFlags = HideFlags.DontSave,
                    anisoLevel = 0,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            var hueVsHueCurve = settings.hueVsHueCurve.value;
            var hueVsSatCurve = settings.hueVsSatCurve.value;
            var satVsSatCurve = settings.satVsSatCurve.value;
            var lumVsSatCurve = settings.lumVsSatCurve.value;
            var masterCurve = settings.masterCurve.value;
            var redCurve = settings.redCurve.value;
            var greenCurve = settings.greenCurve.value;
            var blueCurve = settings.blueCurve.value;
            
            var pixels = m_Pixels;

            for (int i = 0; i < Spline.k_Precision; i++)
            {
                // Secondary/VS curves
                float x = hueVsHueCurve.cachedData[i];
                float y = hueVsSatCurve.cachedData[i];
                float z = satVsSatCurve.cachedData[i];
                float w = lumVsSatCurve.cachedData[i];
                pixels[i] = new Color(x, y, z, w);

                // YRGB
                if (!hdr)
                {
                    float m = masterCurve.cachedData[i];
                    float r = redCurve.cachedData[i];
                    float g = greenCurve.cachedData[i];
                    float b = blueCurve.cachedData[i];
                    pixels[i + Spline.k_Precision] = new Color(r, g, b, m);
                }
            }

            m_GradingCurves.SetPixels(pixels);
            m_GradingCurves.Apply(false, false);

            return m_GradingCurves;
        }

        static RenderTextureFormat GetLutFormat()
        {
            // Use ARGBHalf if possible, fallback on ARGB2101010 and ARGB32 otherwise
            var format = RenderTextureFormat.ARGBHalf;

            if (!SystemInfo.SupportsRenderTextureFormat(format))
            {
                format = RenderTextureFormat.ARGB2101010;

                // Note that using a log lut in ARGB32 is a *very* bad idea but we need it for
                // compatibility reasons (else if a platform doesn't support one of the previous
                // format it'll output a black screen, or worse will segfault on the user).
                if (!SystemInfo.SupportsRenderTextureFormat(format))
                    format = RenderTextureFormat.ARGB32;
            }

            return format;
        }

        static TextureFormat GetCurveFormat()
        {
            // Use RGBAHalf if possible, fallback on ARGB32 otherwise
            var format = TextureFormat.RGBAHalf;

            if (!SystemInfo.SupportsTextureFormat(format))
                format = TextureFormat.ARGB32;

            return format;
        }

        public override void Release()
        {
            RuntimeUtilities.Destroy(m_InternalLdrLut);
            m_InternalLdrLut = null;

            RuntimeUtilities.Destroy(m_InternalLogLut);
            m_InternalLogLut = null;

            RuntimeUtilities.Destroy(m_GradingCurves);
            m_GradingCurves = null;
        }
    }
}
