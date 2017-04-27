using System;

namespace UnityEngine.Experimental.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(ColorGradingRenderer), "Unity/Color Grading")]
    public sealed class ColorGrading : PostProcessEffectSettings
    {
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
