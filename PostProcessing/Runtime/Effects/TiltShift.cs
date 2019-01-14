using System;


namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(TiltShiftRenderer), "Unity/TiltShift")]
    public sealed class TiltShift : PostProcessEffectSettings
    {
        public enum TiltShiftMode
        {
            TiltShiftMode,
            IrisMode,
        }

        [Serializable]
        public sealed class TiltShiftModeParameter : ParameterOverride<TiltShiftMode> { }

        public enum TiltShiftQuality
        {
            Preview,
            Low,
            Normal,
            High,
        }

        [Serializable]
        public sealed class TiltShiftQualityParameter : ParameterOverride<TiltShiftQuality> { }


        public TiltShiftModeParameter mode = new TiltShiftModeParameter { value = TiltShiftMode.TiltShiftMode };
        public TiltShiftQualityParameter quality = new TiltShiftQualityParameter { value = TiltShiftQuality.Normal };

        [Range(0.0f, 15.0f)]
        public FloatParameter blurArea = new FloatParameter { value = 1.0f };

        [Range(0.0f, 25.0f)]
        public FloatParameter maxBlurSize = new FloatParameter { value = 5.0f };

        public BoolParameter downsample = new BoolParameter { value = false };
    }


    public class TiltShiftRenderer : PostProcessEffectRenderer<TiltShift>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("TiltShift");

            var sheet = context.propertySheets.Get(context.resources.shaders.tiltShift);

            int width = settings.downsample ? context.screenWidth >> 1 : context.screenWidth;
            int height = settings.downsample ? context.screenHeight >> 1 : context.screenHeight;

            context.GetScreenSpaceTemporaryRT(cmd, ShaderIDs.TiltShiftBuffer, 0,
                context.sourceFormat,
                RenderTextureReadWrite.Default,
                FilterMode.Bilinear, width, height);

            sheet.properties.SetFloat(ShaderIDs.BlurArea, settings.blurArea);

            if (settings.mode == TiltShift.TiltShiftMode.IrisMode)
                sheet.EnableKeyword("IRIS");
            else
                sheet.DisableKeyword("IRIS");
            
            if ( settings.quality.value == TiltShift.TiltShiftQuality.Preview)
            {
                var finalDestination = context.destination;

                context.GetScreenSpaceTemporaryRT(context.command, ShaderIDs.TiltShiftBuffer, 0, context.sourceFormat);
                cmd.BlitFullscreenTriangle(context.source, ShaderIDs.TiltShiftBuffer, sheet, 0);

                context.source = ShaderIDs.TiltShiftBuffer;
                context.destination = finalDestination;
            }
            else
            {
                sheet.properties.SetFloat(ShaderIDs.BlurSize, settings.maxBlurSize < 0.0f ? 0.0f : settings.maxBlurSize);

                cmd.BlitFullscreenTriangle(context.source, ShaderIDs.TiltShiftBuffer, sheet, (int)settings.quality.value);
                cmd.SetGlobalTexture(ShaderIDs.TiltShiftTex, ShaderIDs.TiltShiftBuffer);

                // Send everything to the uber shader
                var uberSheet = context.uberSheet;
                uberSheet.EnableKeyword("TILTSHIFT");
            }

            cmd.EndSample("TiltShift");

            context.tiltShiftBufferNameID = ShaderIDs.TiltShiftBuffer;
        }
    }
}
