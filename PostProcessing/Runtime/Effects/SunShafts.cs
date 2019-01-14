using System;


namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(SunShaftsRenderer), "Unity/SunShafts")]
    public sealed class SunShafts : PostProcessEffectSettings
    {
        public enum Resolution
        {
            Low = 0,
            Normal = 1,
            High = 2,
        }

        public enum ScreenBlendMode
        {
            Screen = 0,
            Add = 1,
        }

        [Serializable]
        public sealed class ResolutionParameter : ParameterOverride<Resolution> { }
        [Serializable]
        public sealed class ScreenBlendModeParameter : ParameterOverride<ScreenBlendMode> { }


        public ResolutionParameter resolution = new ResolutionParameter { value = Resolution.Normal };
        public ScreenBlendModeParameter screenBlendMode = new ScreenBlendModeParameter { value = ScreenBlendMode.Screen };

        [Tooltip("Chose a position that acts as a root point for the produced sun shafts")]
        public Vector3Parameter sunPosition = new Vector3Parameter { value = new Vector3(0.5f, 0.5f, 0.0f) };

        [Range(1, 3)]
        public IntParameter radialBlurIterations = new IntParameter { value = 2 };

        public ColorParameter shaftsColor = new ColorParameter { value = Color.white };

        public ColorParameter thresholdColor = new ColorParameter { value = new Color(0.87f, 0.74f, 0.65f) };

        [Range(0f, 10f)]
        public FloatParameter blurRadius = new FloatParameter { value = 2.5f };

        public FloatParameter intensity = new FloatParameter { value = 1.15f };

        [Range(0.1f, 1.0f)]
        public FloatParameter distanceFalloff = new FloatParameter { value = 0.25f };
    }




    public class SunShaftsRenderer : PostProcessEffectRenderer<SunShafts>
    {
        int rtW, rtH;
        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        private void CreateTempRenderTarget(PostProcessRenderContext context)
        {
            int divider = 4;
            if (settings.resolution == SunShafts.Resolution.Normal)
                divider = 2;
            else if (settings.resolution == SunShafts.Resolution.High)
                divider = 1;

            rtW = context.screenWidth / divider;
            rtH = context.screenHeight / divider;

            context.GetScreenSpaceTemporaryRT(context.command, ShaderIDs.LrDepthBuffer, 0,
                context.sourceFormat,
                RenderTextureReadWrite.Default,
                FilterMode.Bilinear,
                rtW, rtH);

            context.GetScreenSpaceTemporaryRT(context.command, ShaderIDs.LrColorB, 0,
                context.sourceFormat,
                RenderTextureReadWrite.Default,
                FilterMode.Bilinear,
                rtW, rtH);
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("SunShafts");

            var sheet = context.propertySheets.Get(context.resources.shaders.sunShafts);

            CreateTempRenderTarget(context);

            sheet.properties.SetVector(ShaderIDs.ScreenResultion, new Vector4(rtW -0.5f, rtH - 0.5f,0,0));
            sheet.properties.SetVector(ShaderIDs.SunPosition, SunPosition(context.camera));
            sheet.properties.SetVector(ShaderIDs.SunThreshold, settings.thresholdColor);
            cmd.BlitFullscreenTriangle(context.source, ShaderIDs.LrDepthBuffer, sheet, 1);

            // radial blur:
            SetBlurRaidus(sheet, 1.0f / 768.0f);
            for (int i = 0; i < settings.radialBlurIterations; i++)
            {
                // each iteration takes 2 * 6 samples
                // we update _BlurRadius each time to cheaply get a very smooth look
                cmd.BlitFullscreenTriangle(ShaderIDs.LrDepthBuffer, ShaderIDs.LrColorB, sheet, 0);
                SetBlurRaidus(sheet, (((i * 2.0f + 1.0f) * 6.0f)) / 768.0f);
                cmd.BlitFullscreenTriangle(ShaderIDs.LrColorB, ShaderIDs.LrDepthBuffer, sheet, 0);
                SetBlurRaidus(sheet, (((i * 2.0f + 2.0f) * 6.0f)) / 768.0f);
            }


            cmd.SetGlobalTexture(ShaderIDs.SunShaftTex, ShaderIDs.LrDepthBuffer);
            /*
            sheet.properties.SetVector(ShaderIDs.SunColor, SunColor);
            int pass = (settings.screenBlendMode == SunShafts.ScreenBlendMode.Screen) ? 0 : 4;
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, pass);
            */
            cmd.ReleaseTemporaryRT(ShaderIDs.LrColorB);
            //cmd.ReleaseTemporaryRT(ShaderIDs.LrDepthBuffer);
            cmd.EndSample("SunShafts");

            // Send everything to the uber shader
            var uberSheet = context.uberSheet;
            if(settings.screenBlendMode == SunShafts.ScreenBlendMode.Screen)
                uberSheet.EnableKeyword("SUNSHAFTS_SCREEN");
            else
                uberSheet.EnableKeyword("SUNSHAFTS_ADD");
            uberSheet.properties.SetVector(ShaderIDs.SunColor, SunColor);

            context.sunShaftsBufferNameID = ShaderIDs.SunShaftTex;
        }
        
        private void SetBlurRaidus(PropertySheet sheet, float ratio)
        {
            float ofs = settings.blurRadius * ratio;
            sheet.properties.SetVector(ShaderIDs.BlurRadius4, new Vector4(ofs, ofs, 0.0f, 0.0f));
        }

        private float MaxRadius
        {
            get { return 1 - settings.distanceFalloff.value; }
        }
        
        private Vector4 SunColor
        {
            get {
                Color c = settings.shaftsColor;
                if (MaxRadius >= 0.0f)
                    return new Vector4(c.r, c.g, c.b, c.a) * settings.intensity;
                else
                    return Vector4.zero; // no backprojection !
            }
        }

        private Vector4 SunPosition(Camera camera)
        {
            Vector3 v = new Vector3(0.5f, 0.5f, 0.0f); // default
            if (settings.sunPosition != v)
                v = camera.WorldToViewportPoint(settings.sunPosition);
            return new Vector4(v.x, v.y, v.z, MaxRadius);
        }
    }
}