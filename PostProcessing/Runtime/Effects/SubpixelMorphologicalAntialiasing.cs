using System;

namespace UnityEngine.Rendering.PostProcessing
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

            cmd.GetTemporaryRT(ShaderIDs.SMAA_Flip, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Linear);
            cmd.GetTemporaryRT(ShaderIDs.SMAA_Flop, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Linear);

            cmd.BlitFullscreenTriangle(context.source, ShaderIDs.SMAA_Flip, sheet, (int)Pass.EdgeDetection, true);
            cmd.BlitFullscreenTriangle(ShaderIDs.SMAA_Flip, ShaderIDs.SMAA_Flop, sheet, (int)Pass.BlendWeights);
            cmd.SetGlobalTexture("_BlendTex", ShaderIDs.SMAA_Flop);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.NeighborhoodBlending);

            cmd.ReleaseTemporaryRT(ShaderIDs.SMAA_Flip);
            cmd.ReleaseTemporaryRT(ShaderIDs.SMAA_Flop);
            
            cmd.EndSample("SubpixelMorphologicalAntialiasing");
        }
    }
}
