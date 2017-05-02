using System;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.PostProcessing
{
    [Serializable]
    public sealed class Dithering
    {
        int m_NoiseTextureIndex = 0;

        internal void Render(PostProcessRenderContext context)
        {
            var blueNoise = context.resources.blueNoise;
            Assert.IsTrue(blueNoise != null && blueNoise.Length > 0);

        #if POSTFX_DEBUG_STATIC_DITHERING // Used by QA for automated testing
            textureIndex = 0;
            float rndOffsetX = 0f;
            float rndOffsetY = 0f;
        #else
            if (++m_NoiseTextureIndex >= blueNoise.Length)
                m_NoiseTextureIndex = 0;

            float rndOffsetX = Random.value;
            float rndOffsetY = Random.value;
        #endif

            var noiseTex = blueNoise[m_NoiseTextureIndex];
            var uberSheet = context.uberSheet;

            uberSheet.properties.SetTexture(Uniforms._DitheringTex, noiseTex);
            uberSheet.properties.SetVector(Uniforms._Dithering_Coords, new Vector4(
                (float)context.width / (float)noiseTex.width,
                (float)context.height / (float)noiseTex.height,
                rndOffsetX,
                rndOffsetY
            ));
        }
    }
}
