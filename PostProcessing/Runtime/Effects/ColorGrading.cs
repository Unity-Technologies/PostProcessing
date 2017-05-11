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

    // TODO: Tooltips
    [Serializable]
    [PostProcess(typeof(ColorGradingRenderer), "Unity/Color Grading")]
    public sealed class ColorGrading : PostProcessEffectSettings
    {
        [DisplayName("Mode"), Tooltip("Select a color grading mode that fits your dynamic range and workflow. Use HDR if your camera is set to render in HDR and your target platform supports it. Use LDR for low-end mobiles or devices that don't support HDR. Use Custom HDR if you prefer authoring a Log LUT in external softwares.")]
        public GradingModeParameter gradingMode = new GradingModeParameter { value = GradingMode.HighDefinitionRange };

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

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerRedOutRedIn = new FloatParameter { value = 100f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerRedOutGreenIn = new FloatParameter { value = 0f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerRedOutBlueIn = new FloatParameter { value = 0f };

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerGreenOutRedIn = new FloatParameter { value = 0f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerGreenOutGreenIn = new FloatParameter { value = 100f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerGreenOutBlueIn = new FloatParameter { value = 0f };

        [DisplayName("Red"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerBlueOutRedIn = new FloatParameter { value = 0f };

        [DisplayName("Green"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerBlueOutGreenIn = new FloatParameter { value = 0f };

        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("")]
        public FloatParameter mixerBlueOutBlueIn = new FloatParameter { value = 100f };
        
        [DisplayName("Lift"), Tooltip("Controls the darkest portions of the render."), Trackball]
        public Vector4Parameter lift = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };
        
        [DisplayName("Gamma"), Tooltip("Power function that controls midrange tones."), Trackball]
        public Vector4Parameter gamma = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };
        
        [DisplayName("Gain"), Tooltip("Controls the lightest portions of the render."), Trackball]
        public Vector4Parameter gain = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };

        public GradingCurveParameter masterCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter redCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter greenCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter blueCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter hueVsHueCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };
        public GradingCurveParameter hueVsSatCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };
        public GradingCurveParameter satVsSatCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };
        public GradingCurveParameter lumVsSatCurve = new GradingCurveParameter { value = new ColorGradingCurve(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };

        // Editor fields to keep track of the currently selected channel & curve - unused at runtime
        [SerializeField]
        int m_CurrentMixerChannel;

        [SerializeField]
        int m_CurrentEditingCurve;

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
