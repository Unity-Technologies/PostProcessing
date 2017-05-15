using System;

namespace UnityEngine.Experimental.PostProcessing
{
    public enum GradingMode
    {
        LowDefinitionRange,
        HighDefinitionRange,
        CustomLogLUT
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

    // TODO: use 33x33 Texture3D when supported
    [Serializable]
    [PostProcess(typeof(ColorGradingRenderer), "Unity/Color Grading")]
    public sealed class ColorGrading : PostProcessEffectSettings
    {
        [DisplayName("Mode"), Tooltip("Select a color grading mode that fits your dynamic range and workflow. Use HDR if your camera is set to render in HDR and your target platform supports it. Use LDR for low-end mobiles or devices that don't support HDR. Use Custom HDR if you prefer authoring a Log LUT in external softwares.")]
        public GradingModeParameter gradingMode = new GradingModeParameter { value = GradingMode.HighDefinitionRange };

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

        public GradingCurveParameter masterCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter redCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter greenCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter blueCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter hueVsHueCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };
        public GradingCurveParameter hueVsSatCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };
        public GradingCurveParameter satVsSatCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter lumVsSatCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };
    }

    public sealed class ColorGradingRenderer : PostProcessEffectRenderer<ColorGrading>
    {
        enum Pass
        {
            LutGenNoGradingHDR,
            LutGenHDR,
            LutGenLDRFromScratch,
            LutGenLDR,
        }

        Texture2D m_GradingCurves;
        const int k_CurvePrecision = 128; // If you change this don't forget to update Colors.hlsl:YrgbCurve()
        const float k_CurveStep = 1f / k_CurvePrecision;
        readonly Color[] m_Pixels = new Color[k_CurvePrecision * 2]; // Avoids GC stress

        RenderTexture m_InternalLdrLut;
        RenderTexture m_InternalLogLut;
        const int k_Lut2DSize = 32;

        readonly HableCurve m_HableCurve = new HableCurve();

        public override void Render(PostProcessRenderContext context)
        {
            switch (settings.gradingMode.value)
            {
                case GradingMode.LowDefinitionRange: RenderLDRPipeline(context);
                    break;
                case GradingMode.HighDefinitionRange: RenderHDRPipeline(context);
                    break;
                case GradingMode.CustomLogLUT: RenderCustomLogLUT(context);
                    break;
            }
        }

        void RenderCustomLogLUT(PostProcessRenderContext context)
        {
            var lut = settings.logLut.value;

            // Generate a default, non-graded Lut if none is set so we can lerp properly between
            // volume with & without Luts set.
            if (lut == null)
            {
                CheckInternalLogLut();
                lut = m_InternalLogLut;

                var lutSheet = context.propertySheets.Get(context.resources.shaders.lutBaker);
                lutSheet.ClearKeywords();

                lutSheet.properties.SetVector(Uniforms._Lut2D_Params, new Vector4(
                    k_Lut2DSize,
                    0.5f / (k_Lut2DSize * k_Lut2DSize),
                    0.5f / k_Lut2DSize,
                    k_Lut2DSize / (k_Lut2DSize - 1f))
                );

                context.command.BlitFullscreenTriangle((Texture)null, lut, lutSheet, (int)Pass.LutGenNoGradingHDR);
            }
            
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_HDR");
            uberSheet.properties.SetVector(Uniforms._Lut2D_Params, new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
            uberSheet.properties.SetFloat(Uniforms._PostExposure, RuntimeUtilities.Exp2(settings.postExposure.value));
            uberSheet.properties.SetTexture(Uniforms._Lut2D, lut);
        }

        void RenderHDRPipeline(PostProcessRenderContext context)
        {
            // Unfortunatele because AnimationCurve doesn't implement GetHashCode and we don't have
            // any reliable way to figure out if a curve data is different from another one we can't
            // skip regenerating the Lut if nothing has changed. So it has to be done on every
            // frame...
            // It's not a very expensive operation anyway (we're talking about filling a 1024x32
            // Lut on the GPU) but every little thing helps, especially on mobile.
            {
                CheckInternalLogLut();

                // Lut setup
                var lutSheet = context.propertySheets.Get(context.resources.shaders.lutBaker);
                lutSheet.ClearKeywords();

                SetupCommonSettings(lutSheet);
                
                var lift = ColorUtilities.ColorToLift(settings.lift.value * 0.2f);
                var gain = ColorUtilities.ColorToGain(settings.gain.value * 0.5f);
                var invgamma = ColorUtilities.ColorToInverseGamma(settings.gamma.value * 0.7f);
                lutSheet.properties.SetVector(Uniforms._Lift, lift);
                lutSheet.properties.SetVector(Uniforms._InvGamma, invgamma);
                lutSheet.properties.SetVector(Uniforms._Gain, gain);

                lutSheet.properties.SetTexture(Uniforms._Curves, GetCurveTexture(true));

                SetupTonemapping(lutSheet);

                // Generate the lut
                context.command.BlitFullscreenTriangle((Texture)null, m_InternalLogLut, lutSheet, (int)Pass.LutGenHDR);
            }
            
            var lut = m_InternalLogLut;
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_HDR");
            uberSheet.properties.SetVector(Uniforms._Lut2D_Params, new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
            uberSheet.properties.SetTexture(Uniforms._Lut2D, lut);
            uberSheet.properties.SetFloat(Uniforms._PostExposure, RuntimeUtilities.Exp2(settings.postExposure.value));
        }

        void RenderLDRPipeline(PostProcessRenderContext context)
        {
            // For the same reasons as in RenderHDRPipeline, regen LUT on evey frame
            {
                CheckInternalLdrLut();

                // Lut setup
                var lutSheet = context.propertySheets.Get(context.resources.shaders.lutBaker);
                lutSheet.ClearKeywords();

                SetupCommonSettings(lutSheet);

                var lift = ColorUtilities.ColorToLift(settings.lift.value);
                var gain = ColorUtilities.ColorToGain(settings.gain.value);
                var invgamma = ColorUtilities.ColorToInverseGamma(settings.gamma.value);
                lutSheet.properties.SetVector(Uniforms._Lift, lift);
                lutSheet.properties.SetVector(Uniforms._InvGamma, invgamma);
                lutSheet.properties.SetVector(Uniforms._Gain, gain);

                lutSheet.properties.SetFloat(Uniforms._Brightness, (settings.brightness.value + 100f) / 100f);
                lutSheet.properties.SetTexture(Uniforms._Curves, GetCurveTexture(false));
                
                // Generate the lut
                context.command.BlitFullscreenTriangle((Texture)null, m_InternalLdrLut, lutSheet, (int)Pass.LutGenLDRFromScratch);
            }
            
            var lut = m_InternalLdrLut;
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_LDR");
            uberSheet.properties.SetVector(Uniforms._Lut2D_Params, new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
            uberSheet.properties.SetTexture(Uniforms._Lut2D, lut);
        }

        void SetupCommonSettings(PropertySheet sheet)
        {
            sheet.properties.SetVector(Uniforms._Lut2D_Params, new Vector4(
                k_Lut2DSize,
                0.5f / (k_Lut2DSize * k_Lut2DSize),
                0.5f / k_Lut2DSize,
                k_Lut2DSize / (k_Lut2DSize - 1f))
            );

            var colorBalance = ColorUtilities.ComputeColorBalance(settings.temperature.value, settings.tint.value);
            sheet.properties.SetVector(Uniforms._ColorBalance, colorBalance);

            sheet.properties.SetVector(Uniforms._ColorFilter, settings.colorFilter.value);
            sheet.properties.SetFloat(Uniforms._Contrast, settings.contrast.value / 100f + 1f);     // Remap to [0;2]
            sheet.properties.SetFloat(Uniforms._Saturation, settings.saturation.value / 100f + 1f); // Remap to [0;2]
            sheet.properties.SetFloat(Uniforms._HueShift, settings.hueShift.value / 360f);          // Remap to [-0.5;0.5]
                
            var channelMixerR = new Vector3(settings.mixerRedOutRedIn, settings.mixerRedOutGreenIn, settings.mixerRedOutBlueIn);
            var channelMixerG = new Vector3(settings.mixerGreenOutRedIn, settings.mixerGreenOutGreenIn, settings.mixerGreenOutBlueIn);
            var channelMixerB = new Vector3(settings.mixerBlueOutRedIn, settings.mixerBlueOutGreenIn, settings.mixerBlueOutBlueIn);
            sheet.properties.SetVector(Uniforms._ChannelMixerRed, channelMixerR / 100f);            // Remap to [-2;2]
            sheet.properties.SetVector(Uniforms._ChannelMixerGreen, channelMixerG / 100f);
            sheet.properties.SetVector(Uniforms._ChannelMixerBlue, channelMixerB / 100f);
        }

        void SetupTonemapping(PropertySheet sheet)
        {
            var tonemapper = settings.tonemapper.value;
            if (tonemapper == Tonemapper.ACES)
            {
                sheet.EnableKeyword("TONEMAPPING_ACES");
            }
            else if (tonemapper == Tonemapper.Neutral)
            {
                sheet.EnableKeyword("TONEMAPPING_NEUTRAL");
            }
            else if (tonemapper == Tonemapper.Custom)
            {
                sheet.EnableKeyword("TONEMAPPING_CUSTOM");

                m_HableCurve.Init(
                    settings.toneCurveToeStrength.value,
                    settings.toneCurveToeLength.value,
                    settings.toneCurveShoulderStrength.value,
                    settings.toneCurveShoulderLength.value,
                    settings.toneCurveShoulderAngle.value,
                    settings.toneCurveGamma.value
                );

                var curve = new Vector3(m_HableCurve.inverseWhitePoint, m_HableCurve.x0, m_HableCurve.x1);
                sheet.properties.SetVector(Uniforms._CustomToneCurve, curve);
                sheet.properties.SetFloatArray(Uniforms._ToeSegment, m_HableCurve.segments[0].data);
                sheet.properties.SetFloatArray(Uniforms._MidSegment, m_HableCurve.segments[1].data);
                sheet.properties.SetFloatArray(Uniforms._ShoSegment, m_HableCurve.segments[2].data);
            }
        }

        void CheckInternalLogLut()
        {
            // Check internal lut state, (re)create it if needed
            if (m_InternalLogLut == null || !m_InternalLogLut.IsCreated())
            {
                RuntimeUtilities.Destroy(m_InternalLogLut);

                var format = GetLutFormat();
                m_InternalLogLut = new RenderTexture(k_Lut2DSize * k_Lut2DSize, k_Lut2DSize, 0, format, RenderTextureReadWrite.Linear)
                {
                    name = "Color Grading Log Lut",
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0
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
                    anisoLevel = 0
                };
                m_InternalLdrLut.Create();
            }
        }

        Texture2D GetCurveTexture(bool hdr)
        {
            if (m_GradingCurves == null)
            {
                var format = GetCurveFormat();
                m_GradingCurves = new Texture2D(k_CurvePrecision, 2, format, false, true)
                {
                    name = "Internal Curves Texture",
                    hideFlags = HideFlags.DontSave,
                    anisoLevel = 0,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            settings.hueVsHueCurve.value.Cache();
            settings.hueVsSatCurve.value.Cache();
            var pixels = m_Pixels;

            for (int i = 0; i < k_CurvePrecision; i++)
            {
                float t = i * k_CurveStep;

                // Secondary/VS curves
                float x = settings.hueVsHueCurve.value.Evaluate(t);
                float y = settings.hueVsSatCurve.value.Evaluate(t);
                float z = settings.satVsSatCurve.value.Evaluate(t);
                float w = settings.lumVsSatCurve.value.Evaluate(t);
                pixels[i] = new Color(x, y, z, w);

                // YRGB
                if (!hdr)
                {
                    float m = settings.masterCurve.value.Evaluate(t);
                    float r = settings.redCurve.value.Evaluate(t);
                    float g = settings.greenCurve.value.Evaluate(t);
                    float b = settings.blueCurve.value.Evaluate(t);
                    pixels[i + k_CurvePrecision] = new Color(r, g, b, m);
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
