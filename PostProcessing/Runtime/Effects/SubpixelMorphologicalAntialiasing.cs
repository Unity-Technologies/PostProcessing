using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Subpixel Morphological Anti-aliasing (SMAA) effect.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    [Serializable]
    public sealed class SubpixelMorphologicalAntialiasing
    {
        enum Pass
        {
            EdgeDetection = 0,
            BlendWeights = 3,
            NeighborhoodBlending = 6
        }

        /// <summary>
        /// Quality presets.
        /// </summary>
        public enum Quality
        {
            /// <summary>
            /// Low quality.
            /// </summary>
            Low = 0,

            /// <summary>
            /// Medium quality.
            /// </summary>
            Medium = 1,

            /// <summary>
            /// High quality.
            /// </summary>
            High = 2
        }

        /// <summary>
        /// The quality preset to use for the anti-aliasing filter.
        /// </summary>
        [Tooltip("Lower quality is faster at the expense of visual quality (Low = ~60%, Medium = ~80%).")]
        public Quality quality = Quality.High;

        /// <summary>
        /// Checks if the effect is supported on the target platform.
        /// </summary>
        /// <returns><c>true</c> if the anti-aliasing filter is supported, <c>false</c> otherwise</returns>
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

#if UNITY_2017_3_OR_NEWER
            cmd.GetTemporaryRT(ShaderIDs.SMAA_Flip, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None, context.camera.allowDynamicResolution);
            cmd.GetTemporaryRT(ShaderIDs.SMAA_Flop, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.None, context.camera.allowDynamicResolution);
#else
            cmd.GetTemporaryRT(ShaderIDs.SMAA_Flip, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Linear, 1, false);
            cmd.GetTemporaryRT(ShaderIDs.SMAA_Flop, context.width, context.height, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Linear, 1, false);
#endif            

            cmd.BlitFullscreenTriangle(context.source, ShaderIDs.SMAA_Flip, sheet, (int)Pass.EdgeDetection + (int)quality, true);
            cmd.BlitFullscreenTriangle(ShaderIDs.SMAA_Flip, ShaderIDs.SMAA_Flop, sheet, (int)Pass.BlendWeights + (int)quality);
            cmd.SetGlobalTexture("_BlendTex", ShaderIDs.SMAA_Flop);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.NeighborhoodBlending);

            cmd.ReleaseTemporaryRT(ShaderIDs.SMAA_Flip);
            cmd.ReleaseTemporaryRT(ShaderIDs.SMAA_Flop);
            
            cmd.EndSample("SubpixelMorphologicalAntialiasing");
        }
    }
}
