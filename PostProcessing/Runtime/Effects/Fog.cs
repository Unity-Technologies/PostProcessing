using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Fog effect with the deferred rendering path.
    /// </summary>
#if UNITY_2017_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    [Serializable]
    [PostProcess(typeof(FogRenderer), "Unity/Fog")]
    public sealed class Fog : PostProcessEffectSettings
    {
        [Serializable]
        public sealed class FogModeParameter : ParameterOverride<FogMode> { }

        public BoolParameter excludeSkybox = new BoolParameter { value = true };
        public ColorParameter color = new ColorParameter { value = Color.gray };
        public FloatParameter density = new FloatParameter { value = 0.01f };
        public FloatParameter startDistance = new FloatParameter { value = 100 };
        public FloatParameter endDistance = new FloatParameter { value = 200 };
        public FogModeParameter mode = new FogModeParameter { value = FogMode.Exponential };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled
                && RenderSettings.fog
                && !RuntimeUtilities.scriptableRenderPipelineActive
                && context.resources.shaders.deferredFog
                && context.resources.shaders.deferredFog.isSupported
                && context.camera.actualRenderingPath == RenderingPath.DeferredShading;  // In forward fog is already done at shader level
        }

        public override void Reset(PostProcessEffectSettings defaultSettings)
        {
            base.Reset(defaultSettings);

            enabled.value = RenderSettings.fog;
            color.value = RenderSettings.fogColor;
            density.value = RenderSettings.fogDensity;
            startDistance.value = RenderSettings.fogStartDistance;
            endDistance.value = RenderSettings.fogEndDistance;
            mode.value = RenderSettings.fogMode;
        }
    }

    public sealed class FogRenderer : PostProcessEffectRenderer<Fog>
    {
        Fog m_backupSettings = ScriptableObject.CreateInstance<Fog>();

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(context.resources.shaders.deferredFog);
            sheet.ClearKeywords();

            var fogColor = RuntimeUtilities.isLinearColorSpace ? RenderSettings.fogColor.linear : RenderSettings.fogColor;
            sheet.properties.SetVector(ShaderIDs.FogColor, fogColor);
            sheet.properties.SetVector(ShaderIDs.FogParams, new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));

            var cmd = context.command;
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, settings.excludeSkybox ? 1 : 0);
        }

        static void Load(Fog settings)
        {
            RenderSettings.fog = settings.enabled;
            RenderSettings.fogColor = settings.color;
            RenderSettings.fogDensity = settings.density;
            RenderSettings.fogStartDistance = settings.startDistance;
            RenderSettings.fogEndDistance = settings.endDistance;
            RenderSettings.fogMode = settings.mode;
        }

        static void Store(Fog result)
        {
            result.enabled.value = RenderSettings.fog;
            result.color.value = RenderSettings.fogColor;
            result.density.value = RenderSettings.fogDensity;
            result.startDistance.value = RenderSettings.fogStartDistance;
            result.endDistance.value = RenderSettings.fogEndDistance;
            result.mode.value = RenderSettings.fogMode;
        }

        public void ApplySettings(PostProcessRenderContext context)
        {
            // Backup settings
            Store(m_backupSettings);

            // Apply settings
            Load(settings);

#if UNITY_EDITOR
            if (context.isSceneView && !UnityEditor.SceneView.currentDrawingSceneView.sceneViewState.showFog)
            {
                RenderSettings.fog = false;
            }
#endif
        }

        public void RestoreSettings()
        {
            Load(m_backupSettings);
        }
    }
}
