

using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(BlurEffect), PostProcessEvent.AfterStack, "Unity/Legacy/Blur")]
    public sealed class Blur : PostProcessEffectSettings
    {
        public BlurModeParameter Mode = new BlurModeParameter();
        public IntParameter Downsample = new IntParameter { value = 1 };
        public IntParameter BlurIterations = new IntParameter { value = 1 };
        public FloatParameter BlurSize = new FloatParameter { value = 3.0f };
    }

    [Serializable]
    public sealed class BlurModeParameter : ParameterOverride<BlurEffect.Mode>
    {

    }

    public sealed class BlurEffect : PostProcessEffectRenderer<Blur>
    {
        public enum Mode
        {
            StandardGaussian,
            SgxGaussian
        }

        public enum Pass
        {
            Downsample = 0,
            BlurVertical = 1,
            BlurHorizontal = 2,
        }

        public override void Render(PostProcessRenderContext context)
        {
            CommandBuffer command = context.command;

            command.BeginSample("BlurPostEffect");

            int downsample = settings.Downsample;
            int blurIterations = settings.BlurIterations;
            float blurSize = settings.BlurSize;
            float widthMod = 1.0f / (1.0f * (1 << downsample));

            int rtW = context.width >> downsample;
            int rtH = context.height >> downsample;

            PropertySheet sheet = context.propertySheets.Get(Shader.Find("Hidden/Legacy/Blur"));
            sheet.properties.Clear();
            sheet.properties.SetVector("_Parameter", new Vector4(blurSize * widthMod, -blurSize * widthMod, 0.0f, 0.0f));

            int blurId = Shader.PropertyToID("_BlurPostProcessEffect");
            command.GetTemporaryRT(blurId, rtW, rtH, 0, FilterMode.Bilinear);
            command.BlitFullscreenTriangle(context.source, blurId, sheet, (int)Pass.Downsample);

            int pass = settings.Mode.value == Mode.SgxGaussian ? 2 : 0;

            int rtIndex = 0;
            for(int i = 0; i < blurIterations; i++)
            {
                float iterationOffs = i * 1.0f;
                sheet.properties.SetVector("_Parameter", new Vector4(blurSize * widthMod + iterationOffs, -blurSize * widthMod - iterationOffs, 0.0f, 0.0f));

                // Vertical blur..
                int rtId2 = Shader.PropertyToID("_BlurPostProcessEffect" + rtIndex++);
                command.GetTemporaryRT(rtId2, rtW, rtH, 0, FilterMode.Bilinear);
                command.BlitFullscreenTriangle(blurId, rtId2, sheet, (int)Pass.BlurVertical + pass);
                command.ReleaseTemporaryRT(blurId);
                blurId = rtId2;

                // Horizontal blur..
                rtId2 = Shader.PropertyToID("_BlurPostProcessEffect" + rtIndex++);
                command.GetTemporaryRT(rtId2, rtW, rtH, 0, FilterMode.Bilinear);
                command.BlitFullscreenTriangle(blurId, rtId2, sheet, (int)Pass.BlurHorizontal + pass);
                command.ReleaseTemporaryRT(blurId);
                blurId = rtId2;
            }

            command.Blit(blurId, context.destination);
            command.ReleaseTemporaryRT(blurId);

            command.EndSample("BlurPostEffect");
        }
    }
}