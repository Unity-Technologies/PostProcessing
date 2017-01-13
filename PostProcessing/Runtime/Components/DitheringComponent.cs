namespace UnityEngine.PostProcessing
{
    public sealed class DitheringComponent : PostProcessingComponentRenderTexture<DitheringModel>
    {
        static class Uniforms
        {
            internal static readonly int _Dithering_Params     = Shader.PropertyToID("_Dithering_Params");
            internal static readonly int _Dithering_RangeLimit = Shader.PropertyToID("_Dithering_RangeLimit");
            internal static readonly int _Dithering_ColorRange = Shader.PropertyToID("_Dithering_ColorRange");
        }

        public override bool active
        {
            get
            {
                return model.enabled && !context.interrupted;
            }
        }

        Vector3 colorsPerChannel;
        Vector3 ditherRangeLimit;
        int depthR;
        int depthG;
        int depthB;

        public override void Prepare(Material uberMaterial)
        {
            var settings = model.settings;

            if (settings.bitsPerChannel_R != depthR || settings.bitsPerChannel_G != depthG || settings.bitsPerChannel_B != depthB)
            {
                colorsPerChannel = new Vector3((float)System.Math.Pow(2d, settings.bitsPerChannel_R), 
                                               (float)System.Math.Pow(2d, settings.bitsPerChannel_G), 
                                               (float)System.Math.Pow(2d, settings.bitsPerChannel_B));

                // Limit dithering range for more accurate results
                if (settings.bitsPerChannel_R > 6)
                    ditherRangeLimit.x = Mathf.Clamp01(Mathf.Pow(1f - 1f / (colorsPerChannel.x * .34f), 4));
                else
                    ditherRangeLimit.x = Mathf.Lerp(0.013f, 1, Mathf.Pow((float)settings.bitsPerChannel_R / 6, 3));

                if (settings.bitsPerChannel_G > 6)
                    ditherRangeLimit.y = Mathf.Clamp01(Mathf.Pow(1f - 1f / (colorsPerChannel.y * .34f), 4));
                else
                    ditherRangeLimit.y = Mathf.Lerp(0.013f, 1, Mathf.Pow((float)settings.bitsPerChannel_G / 6, 3));

                if (settings.bitsPerChannel_B > 6)
                    ditherRangeLimit.z = Mathf.Clamp01(Mathf.Pow(1f - 1f / (colorsPerChannel.z * .34f), 4));
                else
                    ditherRangeLimit.z = Mathf.Lerp(0.013f, 1, Mathf.Pow((float)settings.bitsPerChannel_B / 6, 3));

                depthR = settings.bitsPerChannel_R;
                depthG = settings.bitsPerChannel_G;
                depthB = settings.bitsPerChannel_B;
            }

            if (settings.depthSelectionMode == 2)   // 2 = "Automatic" color depth
            {
                // TODO: Detect the display's bit depth, and set the 'bitsPerChannel_R/G/B' accordingly
            }

            float rndOffsetX = 0;
            float rndOffsetY = 0;

            if (settings.animatedNoise)
            {
                rndOffsetX = Random.value;
                rndOffsetY = Random.value;
            }
            
            uberMaterial.EnableKeyword("DITHERING");
            if (settings.depthSelectionMode != 2)
            {
                uberMaterial.EnableKeyword("DITHERING_CUSTOM_BIT_DEPTH");
            }
            uberMaterial.SetVector(Uniforms._Dithering_Params, new Vector4(rndOffsetX, rndOffsetY, settings.amount));
            uberMaterial.SetVector(Uniforms._Dithering_RangeLimit, ditherRangeLimit);
            uberMaterial.SetVector(Uniforms._Dithering_ColorRange, colorsPerChannel);
        }
    }
}
