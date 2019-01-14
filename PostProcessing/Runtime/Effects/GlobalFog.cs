using System;


namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(GlobalFogRenderer), PostProcessEvent.BeforeTransparent, "Unity/GlobalFog")]
    public sealed class GlobalFog : PostProcessEffectSettings
    {
        [Tooltip("Apply distance-based fog?")]
        public BoolParameter distanceFog = new BoolParameter { value = true };
        [Tooltip("Exclude far plane pixels from distance-based fog? (Skybox or clear color)")]
        public BoolParameter excludeFarPixels = new BoolParameter { value = true };
        [Tooltip("Distance fog is based on radial distance from camera when checked")]
        public BoolParameter useRadialDistance = new BoolParameter { value = false };
        [Tooltip("Apply height-based fog?")]
        public BoolParameter heightFog = new BoolParameter { value = true };
        [Tooltip("Fog top Y coordinate")]
        public FloatParameter height = new FloatParameter { value = 1.0f };
        [Range(0.001f, 10.0f)]
        public FloatParameter heightDensity = new FloatParameter { value = 2.0f };
        [Tooltip("Push fog away from the camera by this amount")]
        public FloatParameter startDistance = new FloatParameter { value = 0.0f };
    }

    [ImageEffectOpaque]
    public class GlobalFogRenderer : PostProcessEffectRenderer<GlobalFog>
    {
        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("GlobalFog");
            var sheet = context.propertySheets.Get(context.resources.shaders.globalFog);

            Camera cam = context.camera;
            Transform camtr = cam.transform;

            Vector3[] frustumCorners = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, cam.stereoActiveEye, frustumCorners);
            var bottomLeft = camtr.TransformVector(frustumCorners[0]);
            var topLeft = camtr.TransformVector(frustumCorners[1]);
            var topRight = camtr.TransformVector(frustumCorners[2]);
            var bottomRight = camtr.TransformVector(frustumCorners[3]);

            Matrix4x4 frustumCornersArray = Matrix4x4.identity;
            frustumCornersArray.SetRow(0, bottomLeft);
            frustumCornersArray.SetRow(1, bottomRight);
            frustumCornersArray.SetRow(2, topLeft);
            frustumCornersArray.SetRow(3, topRight);

            var camPos = camtr.position;
            float FdotC = camPos.y - settings.height;
            float paramK = (FdotC <= 0.0f ? 1.0f : 0.0f);
            float excludeDepth = (settings.excludeFarPixels ? 1.0f : 2.0f);

            sheet.properties.SetMatrix(ShaderIDs.FrustumCornersWS, frustumCornersArray);
            sheet.properties.SetVector(ShaderIDs.CameraWS, camPos);
            sheet.properties.SetVector(ShaderIDs.HeightParams, new Vector4(settings.height, FdotC, paramK, settings.heightDensity * 0.5f));
            sheet.properties.SetVector(ShaderIDs.DistanceParams, new Vector4(-Mathf.Max(settings.startDistance, 0.0f), excludeDepth, 0, 0));

            var sceneMode = RenderSettings.fogMode;
            var sceneDensity = RenderSettings.fogDensity;
            var sceneStart = RenderSettings.fogStartDistance;
            var sceneEnd = RenderSettings.fogEndDistance;
            Vector4 sceneParams;
            bool linear = (sceneMode == FogMode.Linear);
            float diff = linear ? sceneEnd - sceneStart : 0.0f;
            float invDiff = Mathf.Abs(diff) > 0.0001f ? 1.0f / diff : 0.0f;
            sceneParams.x = sceneDensity * 1.2011224087f; // density / sqrt(ln(2)), used by Exp2 fog mode
            sceneParams.y = sceneDensity * 1.4426950408f; // density / ln(2), used by Exp fog mode
            sceneParams.z = linear ? -invDiff : 0.0f;
            sceneParams.w = linear ? sceneEnd * invDiff : 0.0f;
            sheet.properties.SetVector(ShaderIDs.SceneFogParams, sceneParams);
            sheet.properties.SetVector(ShaderIDs.SceneFogMode, new Vector4((int)sceneMode, settings.useRadialDistance ? 1 : 0, 0, 0));

            int pass = 0;
            if (settings.distanceFog && settings.heightFog)
                pass = 0; // distance + height
            else if (settings.distanceFog)
                pass = 1; // distance only
            else
                pass = 2; // height only

            cmd.BlitFullscreenQuad(context.source, context.destination, sheet, pass);

            cmd.EndSample("GlobalFog");
        }
    }
}