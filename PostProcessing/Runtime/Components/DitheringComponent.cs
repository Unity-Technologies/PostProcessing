namespace UnityEngine.PostProcessing
{
    public sealed class DitheringComponent : PostProcessingComponentRenderTexture<DitheringModel>
    {
        static class Uniforms
        {
            internal static readonly int _Dithering_Params     = Shader.PropertyToID("_Dithering_Params");
            internal static readonly int _Dithering_ColorRange = Shader.PropertyToID("_Dithering_ColorRange");
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
                colorsPerChannel = (float)(System.Math.Pow(System.Math.Pow(2, settings.colorDepth), 1d / 3d));
                depth = settings.colorDepth;

                if(settings.limitAutomatically)
                {
                    if(settings.colorDepth > 7)
                        ditherRangeLimit = Mathf.Clamp01(Mathf.Pow(1.0f - 1.0f / (colorsPerChannel * 0.34f), 4));
                    else
                        ditherRangeLimit = Mathf.Lerp(0.015f, 0.08f, (float)settings.colorDepth / 8);
                }
                limitAutomatically = settings.limitAutomatically;
            }

            if(!settings.limitAutomatically)
            {
                ditherRangeLimit = settings.ditherRangeLimit;
            }

            if(settings.detectBitDepth){
                // TODO: Detect the display's color bit depth
            }

            float rndOffsetX = 0;
            float rndOffsetY = 0;

            if(settings.animatedNoise)
            {
                rndOffsetX = Random.value;
                rndOffsetY = Random.value;
            }
            
            uberMaterial.EnableKeyword("DITHERING");
            if(settings.previewColorDepth)
            {
                uberMaterial.EnableKeyword("DITHERING_LIMIT_COLOR_RANGE");
            }
            uberMaterial.SetVector(Uniforms._Dithering_Params,new Vector4(settings.ditheringAmount, ditherRangeLimit, rndOffsetX, rndOffsetY));
            uberMaterial.SetFloat(Uniforms._Dithering_ColorRange, colorsPerChannel);
        }
    }
}
