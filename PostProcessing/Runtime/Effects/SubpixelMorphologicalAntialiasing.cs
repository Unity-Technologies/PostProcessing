using System;

namespace UnityEngine.Experimental.PostProcessing
{
    [Serializable]
    public sealed class SubpixelMorphologicalAntialiasing
    {
        enum Pass
        {
            EdgeDetection,
            BlendWeights,
            NeighborhoodBlending
        }

        public bool IsSupported()
        {
            return !RuntimeUtilities.isSinglePassStereoEnabled;
        }

        internal void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(context.resources.shaders.subpixelMorphologicalAntialiasing);
            sheet.properties.SetTexture("_AreaTex", context.resources.smaaLuts.area);
            sheet.properties.SetTexture("_SearchTex", context.resources.smaaLuts.search);

            var cmd = context.command;
            cmd.BeginSample("SubpixelMorphologicalAntialiasing");

            cmd.GetTemporaryRT(Uniforms._SMAA_Flip, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Linear);
            cmd.GetTemporaryRT(Uniforms._SMAA_Flop, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Linear);

            cmd.BlitFullscreenTriangle(context.source, Uniforms._SMAA_Flip, sheet, (int)Pass.EdgeDetection, true);
            cmd.BlitFullscreenTriangle(Uniforms._SMAA_Flip, Uniforms._SMAA_Flop, sheet, (int)Pass.BlendWeights);
            cmd.SetGlobalTexture("_BlendTex", Uniforms._SMAA_Flop);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.NeighborhoodBlending);

            cmd.ReleaseTemporaryRT(Uniforms._SMAA_Flip);
            cmd.ReleaseTemporaryRT(Uniforms._SMAA_Flop);
            
            cmd.EndSample("SubpixelMorphologicalAntialiasing");
        }
    }
}
