namespace UnityEngine.PostProcessing
{
    public sealed class DitheringComponent : PostProcessingComponentRenderTexture<DitheringModel>
    {
        static class Uniforms
        {
            internal static readonly int _Dithering_Amount = Shader.PropertyToID("_Dithering_Amount");
            internal static readonly int _Dithering_ColorRange = Shader.PropertyToID("_Dithering_ColorRange");
            internal static readonly int _Dithering_NoiseRangeLimit = Shader.PropertyToID("_Dithering_NoiseRangeLimit");
        }

        public override bool active
        {
            get
            {
                return model.enabled && !context.interrupted;
            }
        }

        float colorsPerChannel;
        float ditherRangeLimit;
        bool limitAutomatically;
        int depth = -1;

        public override void Prepare(Material uberMaterial)
        {
            var settings = model.settings;

            if(settings.colorDepth != depth || settings.limitAutomatically != limitAutomatically)
            {
                colorsPerChannel = (float)(System.Math.Pow(System.Math.Pow(2, settings.colorDepth), 1d/3d));
                depth = settings.colorDepth;

                if(settings.limitAutomatically)
                {
                    if(settings.colorDepth > 7)
                        ditherRangeLimit = Mathf.Clamp01(Mathf.Pow(1.0f - 1.0f / (colorsPerChannel * .38f), 4));
                    else
                        ditherRangeLimit = Mathf.Lerp(.025f, .1f, (float)settings.colorDepth / 8);
                }
                limitAutomatically=settings.limitAutomatically;
            }
            if(settings.detectBitDepth){
                // TODO: Detect the display's color bit depth
            }
            if(!limitAutomatically)
                ditherRangeLimit=settings.ditherRangeLimit;

            uberMaterial.EnableKeyword("DITHERING");
            if(settings.animatedNoise){
                uberMaterial.EnableKeyword("DITHERING_ANIMATED");                
            }
            if(settings.previewColorDepth){
                uberMaterial.EnableKeyword("DITHERING_LIMIT_COLOR_RANGE");
            }
            uberMaterial.SetFloat(Uniforms._Dithering_Amount, settings.ditheringAmount);
            uberMaterial.SetFloat(Uniforms._Dithering_ColorRange, colorsPerChannel);
            uberMaterial.SetFloat(Uniforms._Dithering_NoiseRangeLimit, ditherRangeLimit);
        }
    }
}
