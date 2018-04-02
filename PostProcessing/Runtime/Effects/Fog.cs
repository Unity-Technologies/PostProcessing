using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(FogRenderer), PostProcessEvent.AfterStack, "Unity/Fog")]
    public sealed class Fog : PostProcessEffectSettings
    {
        [Serializable]
        public sealed class FogModeParameter : ParameterOverride<FogMode> { }

        public BoolParameter excludeSkybox = new BoolParameter { value = false };
        public ColorParameter color = new ColorParameter { value = Color.gray };
        public FloatParameter density = new FloatParameter { value = 0.01f };
        public FloatParameter startDistance = new FloatParameter { value = 100 };
        public FloatParameter endDistance = new FloatParameter { value = 200 };
        public FogModeParameter mode = new FogModeParameter { value = FogMode.Exponential };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            // Never rendered directly
            return false;
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

        public override void Render(PostProcessRenderContext context)
        {
            throw new NotSupportedException();
        }

        static void Load(Fog settings, ref bool excludeSkybox)
        {
            excludeSkybox = settings.excludeSkybox;
            RenderSettings.fog = settings.enabled;
            RenderSettings.fogColor = settings.color;
            RenderSettings.fogDensity = settings.density;
            RenderSettings.fogStartDistance = settings.startDistance;
            RenderSettings.fogEndDistance = settings.endDistance;
            RenderSettings.fogMode = settings.mode;
        }

        static void Store(Fog result, bool excludeSkybox)
        {
            result.excludeSkybox.value = excludeSkybox;
            result.enabled.value = RenderSettings.fog;
            result.color.value = RenderSettings.fogColor;
            result.density.value = RenderSettings.fogDensity;
            result.startDistance.value = RenderSettings.fogStartDistance;
            result.endDistance.value = RenderSettings.fogEndDistance;
            result.mode.value = RenderSettings.fogMode;
        }

        public void ApplySettings(PostProcessRenderContext context, ref bool excludeSkybox)
        {
            // Backup settings
            Store(m_backupSettings, excludeSkybox);

            // Apply settings
            Load(settings, ref excludeSkybox);

#if UNITY_EDITOR
            if (context.isSceneView && !UnityEditor.SceneView.currentDrawingSceneView.sceneViewState.showFog)
            {
                RenderSettings.fog = false;
            }
#endif
        }

        public void RestoreSettings(ref bool excludeSkybox)
        {
            Load(m_backupSettings, ref excludeSkybox);
        }
    }
}
