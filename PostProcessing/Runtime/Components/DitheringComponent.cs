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
        int mode;

        public override void Prepare(Material uberMaterial)
        {
            var settings = model.settings;


            if (settings.bitsPerChannel_R != depthR || settings.bitsPerChannel_G != depthG || settings.bitsPerChannel_B != depthB || settings.depthSelectionMode != mode)
            {
                mode = settings.depthSelectionMode;

                if (mode == 2) // 2 = "Automatic" color depth
                {
                    // TODO: Detect the bit depth of the display
                    depthR = 11;
                    depthG = 11;
                    depthB = 10;
                }
                else
                {
                    depthR = settings.bitsPerChannel_R;
                    depthG = settings.bitsPerChannel_G;
                    depthB = settings.bitsPerChannel_B;
                }

                // Limit dithering range for more accurate results
                if (depthR > 6)
                {
                    ditherRangeLimit.x = Mathf.Lerp(0.45f, 0, 1.0f - (1.0f / ((float)depthR - 8)));
                }
                else
                {
                    ditherRangeLimit.x = Mathf.Lerp(0.015f, 0.18f, Mathf.Pow((float)depthR / 6, 2));
                }

                if (depthG > 6)
                {
                    ditherRangeLimit.y = Mathf.Lerp(0.45f, 0, 1.0f - (1.0f / ((float)depthG - 8)));
                }
                else
                {
                    ditherRangeLimit.y = Mathf.Lerp(0.015f, 0.18f, Mathf.Pow((float)depthG / 6, 2));
                }

                if (depthB > 6)
                {
                    ditherRangeLimit.z = Mathf.Lerp(0.45f, 0, 1.0f - (1.0f / ((float)depthB - 8)));
                }
                else
                {
                    ditherRangeLimit.z = Mathf.Lerp(0.015f, 0.18f, Mathf.Pow((float)depthB / 6, 2));
                }

                colorsPerChannel = new Vector3((float)System.Math.Pow(2d, depthR), 
                                               (float)System.Math.Pow(2d, depthG), 
                                               (float)System.Math.Pow(2d, depthB));
            }

            if (settings.depthSelectionMode == 2)
            {
                // Values need to be reset to avoid the above 'if' statement running when unnecessary
                depthR = settings.bitsPerChannel_R;
                depthG = settings.bitsPerChannel_G;
                depthB = settings.bitsPerChannel_B;
            }
            else
            {
                uberMaterial.EnableKeyword("DITHERING_CUSTOM_BIT_DEPTH");
            }

            Vector2 rndOffset = Vector2.zero;

            switch (settings.ditheringMode)
            {
                case 0:
                {
                    uberMaterial.EnableKeyword("DITHERING_DISABLED");
                    break;
                }
                case 1:
                {
                    uberMaterial.EnableKeyword("DITHERING");
                    break;
                }
                case 2:
                {
                    uberMaterial.EnableKeyword("DITHERING");
                    rndOffset = new Vector2(Random.value, Random.value);
                    break;
                }
            }

            uberMaterial.SetVector(Uniforms._Dithering_Params, rndOffset);
            uberMaterial.SetVector(Uniforms._Dithering_RangeLimit, ditherRangeLimit);
            uberMaterial.SetVector(Uniforms._Dithering_ColorRange, colorsPerChannel);
        }
    }
}
