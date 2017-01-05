namespace UnityEngine.PostProcessing
{
    public sealed class DitheringComponent : PostProcessingComponentRenderTexture<DitheringModel>
    {
        static class Uniforms
        {
            internal static readonly int _Dithering_Amount = Shader.PropertyToID("_Dithering_Amount");
            internal static readonly int _Dithering_ColorRange = Shader.PropertyToID("_Dithering_ColorRange");
            internal static readonly int _Dithering_Animate = Shader.PropertyToID("_Dithering_Animate");
            internal static readonly int _Dithering_NoiseRangeLimit = Shader.PropertyToID("_Dithering_NoiseRangeLimit");
            internal static readonly int _Dithering_LimitColorRange = Shader.PropertyToID("_Dithering_LimitColorRange");
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
                colorsPerChannel = (float)(System.Math.Pow(System.Math.Pow(2, (float)settings.colorDepth), 1d/3d));
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
            uberMaterial.SetFloat(Uniforms._Dithering_Amount, settings.ditheringAmount);
            uberMaterial.SetFloat(Uniforms._Dithering_ColorRange, colorsPerChannel);
            uberMaterial.SetInt(Uniforms._Dithering_Animate, settings.animatedNoise ? 1 : 0);
            uberMaterial.SetFloat(Uniforms._Dithering_NoiseRangeLimit, ditherRangeLimit);
            uberMaterial.SetInt(Uniforms._Dithering_LimitColorRange, settings.previewColorDepth ? 1 : 0);
        }
    }
}
