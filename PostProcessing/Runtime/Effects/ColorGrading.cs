using System;

namespace UnityEngine.Experimental.PostProcessing
{
    public enum GradingMode
    {
        LowDefinitionRange,
        HighDefinitionRange,
        CustomLogLUT
    }

    [Serializable]
    public sealed class GradingModeParameter : ParameterOverride<GradingMode> {}


    // TODO: Clean this up, it's ugly
    [Serializable]
    [PostProcess(typeof(ColorGradingRenderer), "Unity/Color Grading")]
    public sealed class ColorGrading : PostProcessEffectSettings
    {
        [DisplayName("Mode"), Tooltip("Select a color grading mode that fits your dynamic range and workflow. Use HDR if your camera is set to render in HDR and your target platform supports it. Use LDR for low-end mobiles or devices that don't support HDR. Use Custom HDR if you prefer authoring a Log LUT in external softwares.")]
        public GradingModeParameter gradingMode = new GradingModeParameter { value = GradingMode.HighDefinitionRange };

        // -----------------------------------------------------------------------------------------
        // LDR settings

        [DisplayName("Lookup Texture"), Tooltip("Custom lookup texture (strip format, e.g. 256x16). If none is provided, a neutral one will be generated internally.")]
        public TextureParameter ldrLut = new TextureParameter { value = null };

        [DisplayName("Temperature"), Range(-100f, 100f), Tooltip("Sets the white balance to a custom color temperature.")]
        public FloatParameter ldrTemperature = new FloatParameter { value = 0f };

        [DisplayName("Tint"), Range(-100f, 100f), Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
        public FloatParameter ldrTint = new FloatParameter { value = 0f };

        [DisplayName("Color Filter"), ColorUsage(false, true, 0f, 8f, 0.125f, 3f), Tooltip("Tint the render by multiplying a color.")]
        public ColorParameter ldrColorFilter = new ColorParameter { value = Color.white };

        [DisplayName("Hue Shift"), Range(-180f, 180f), Tooltip("Shift the hue of all colors.")]
        public FloatParameter ldrHueShift = new FloatParameter { value = 0f };

        [DisplayName("Saturation"), Range(-100f, 100f), Tooltip("Pushes the intensity of all colors.")]
        public FloatParameter ldrSaturation = new FloatParameter { value = 0f };

        [DisplayName("Brightness"), Range(-100f, 100f), Tooltip("Makes the image brighter or darker.")]
        public FloatParameter ldrBrightness = new FloatParameter { value = 0f };

        [DisplayName("Contrast"), Range(-100f, 100f), Tooltip("Expands or shrinks the overall range of tonal values.")]
        public FloatParameter ldrContrast = new FloatParameter { value = 0f };

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerRedOutRedIn = new FloatParameter { value = 100f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerRedOutGreenIn = new FloatParameter { value = 0f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerRedOutBlueIn = new FloatParameter { value = 0f };

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerGreenOutRedIn = new FloatParameter { value = 0f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerGreenOutGreenIn = new FloatParameter { value = 100f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerGreenOutBlueIn = new FloatParameter { value = 0f };

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerBlueOutRedIn = new FloatParameter { value = 0f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerBlueOutGreenIn = new FloatParameter { value = 0f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter ldrMixerBlueOutBlueIn = new FloatParameter { value = 100f };

        // Used to keep track of the current selected channel in the editor - Unused at runtime
        [SerializeField]
        int m_LdrMixerChannel;
        
        [DisplayName("Lift"), Tooltip("Controls the darkest portions of the render."), Trackball]
        public Vector4Parameter ldrLift = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };
        
        [DisplayName("Gamma"), Tooltip("Power function that controls midrange tones."), Trackball]
        public Vector4Parameter ldrGamma = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };
        
        [DisplayName("Gain"), Tooltip("Controls the lightest portions of the render."), Trackball]
        public Vector4Parameter ldrGain = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };

        public GradingCurveParameter ldrMaster = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter ldrRed = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter ldrGreen = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter ldrBlue = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter ldrHueVsHue = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };
        public GradingCurveParameter ldrHueVsSat = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };
        public GradingCurveParameter ldrSatVsSat = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter ldrLumVsSat = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };

        // Used to keep track of the current selected curve in the editor - Unused at runtime
        [SerializeField]
        int m_LdrCurrentEditingCurve;

        // -----------------------------------------------------------------------------------------
        // HDR settings


        // -----------------------------------------------------------------------------------------
        // Custom HDR settings

        public TextureParameter logLut = new TextureParameter { value = null };
    }

    public sealed class ColorGradingRenderer : PostProcessEffectRenderer<ColorGrading>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var lut = settings.logLut.value;

            if (lut == null)
                return;

            var sheet = context.uberSheet;
            sheet.EnableKeyword("COLOR_GRADING");
            sheet.properties.SetVector(Uniforms._LogLut_Params, new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
            sheet.properties.SetTexture(Uniforms._LogLut, lut);
        }
    }
}
