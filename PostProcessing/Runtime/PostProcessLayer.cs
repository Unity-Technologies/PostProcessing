using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.PostProcessing
{
    using VolumeManager = PostProcessVolumeManager;

    // TODO: Image effects in the sceneview (make sure they work, MB & TAA should be always off in scene view)
    // TODO: User effect sorting for this layer (ReorderableList)
    // TODO: XMLDoc everything (?)
    [DisallowMultipleComponent, ExecuteInEditMode, ImageEffectAllowedInSceneView]
    [RequireComponent(typeof(Camera))]
    public sealed class PostProcessLayer : MonoBehaviour
    {
        public enum Antialiasing
        {
            None,
            FastApproximateAntialiasing,
            TemporalAntialiasing
        }

        // Settings
        public Transform volumeTrigger;
        public LayerMask volumeLayer;

        // Builtins / hardcoded effects that don't benefit from volume blending
        public Antialiasing antialiasingMode = Antialiasing.None;
        public TemporalAntialiasing temporalAntialiasing;
        public Dithering dithering;

        // Debug utilities
        public bool showDebugUI;
        public PostProcessDebugView debugView;

        // Pre-ordered custom user effects
        Dictionary<PostProcessEvent, List<PostProcessBundle>> m_SortedBundles;

        // Settings/Renderer bundles mapped to settings types
        Dictionary<Type, PostProcessBundle> m_Bundles;

        PropertySheetFactory m_PropertySheetFactory;
        CommandBuffer m_LegacyCmdBufferOpaque;
        CommandBuffer m_LegacyCmdBuffer;
        Camera m_Camera;
        PostProcessRenderContext m_CurrentContext;
        BlueNoise m_BlueNoise;

        bool m_SettingsUpdateNeeded = true;
        bool m_IsRenderingInSceneView = false;

        // Recycled list - used to reduce GC stress when gathering active effects in a bundle list
        // on each frame
        List<PostProcessEffectRenderer> m_ActivePool = new List<PostProcessEffectRenderer>();
        List<RenderTargetIdentifier> m_TargetPool = new List<RenderTargetIdentifier>();

        void OnEnable()
        {
            m_Bundles = new Dictionary<Type, PostProcessBundle>();
            m_SortedBundles = new Dictionary<PostProcessEvent, List<PostProcessBundle>>(new PostProcessEventComparer())
            {
                { PostProcessEvent.BeforeTransparent, new List<PostProcessBundle>() },
                { PostProcessEvent.BeforeStack,       new List<PostProcessBundle>() },
                { PostProcessEvent.AfterStack,        new List<PostProcessBundle>() }
            };

            foreach (var type in VolumeManager.instance.settingsTypes.Keys)
            {
                var settings = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
                var bundle = new PostProcessBundle(settings);
                m_Bundles.Add(type, bundle);

                if (!bundle.attribute.builtinEffect)
                    m_SortedBundles[bundle.attribute.eventType].Add(bundle);
            }

            m_PropertySheetFactory = new PropertySheetFactory();

            m_BlueNoise = new BlueNoise();

            // Scriptable render pipeline handles their own command buffers
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            m_LegacyCmdBuffer = new CommandBuffer { name = "Post-processing" };
            m_LegacyCmdBufferOpaque = new CommandBuffer { name = "Opaque Only Post-processing" };

            m_Camera = GetComponent<Camera>();
            m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_LegacyCmdBufferOpaque);
            m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, m_LegacyCmdBuffer);

            // Internal context used if no SRP is set
            m_CurrentContext = new PostProcessRenderContext();
        }

        void OnDisable()
        {
            if (!RuntimeUtilities.scriptableRenderPipelineActive)
            {
                m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_LegacyCmdBufferOpaque);
                m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, m_LegacyCmdBuffer);
            }

            m_BlueNoise.Release();
            temporalAntialiasing.Release();

            foreach (var bundles in m_SortedBundles.Values)
                bundles.Clear();

            foreach (var bundle in m_Bundles.Values)
                bundle.Release();

            m_Bundles.Clear();
            m_SortedBundles.Clear();
            m_PropertySheetFactory.Release();
        }

        void OnPreCull()
        {
            // Unused in scriptable render pipelines
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            var sourceFormat = m_Camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            int tempRt = Uniforms._LegacyTemp;

            m_CurrentContext.Reset();
            m_CurrentContext.camera = m_Camera;
            m_CurrentContext.sourceFormat = sourceFormat;
            m_CurrentContext.source = new RenderTargetIdentifier(tempRt);
            m_CurrentContext.destination = BuiltinRenderTextureType.CameraTarget;

            // Update & override layer settings first (volume blending)
            UpdateSettingsIfNeeded(m_CurrentContext);

            m_LegacyCmdBufferOpaque.Clear();
            m_LegacyCmdBuffer.Clear();

            // TODO: Factorize this
            if (HasOpaqueOnlyEffects())
            {
                m_LegacyCmdBufferOpaque.GetTemporaryRT(tempRt, m_Camera.pixelWidth, m_Camera.pixelHeight, 24, FilterMode.Bilinear, sourceFormat);
                m_LegacyCmdBufferOpaque.BlitFullscreenTriangle(BuiltinRenderTextureType.CameraTarget, tempRt);
                m_CurrentContext.command = m_LegacyCmdBufferOpaque;
                RenderOpaqueOnly(m_CurrentContext);
                m_LegacyCmdBufferOpaque.ReleaseTemporaryRT(tempRt);
            }

            m_LegacyCmdBuffer.GetTemporaryRT(tempRt, m_Camera.pixelWidth, m_Camera.pixelHeight, 24, FilterMode.Bilinear, sourceFormat);
            m_LegacyCmdBuffer.BlitFullscreenTriangle(BuiltinRenderTextureType.CameraTarget, tempRt);
            m_CurrentContext.command = m_LegacyCmdBuffer;
            Render(m_CurrentContext);
            m_LegacyCmdBuffer.ReleaseTemporaryRT(tempRt);
        }

        void OnPostRender()
        {
            // Unused in scriptable render pipelines
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            if (m_CurrentContext.IsTemporalAntialiasingActive() && !m_IsRenderingInSceneView)
                m_Camera.ResetProjectionMatrix();
        }

        PostProcessBundle GetBundle<T>()
            where T : PostProcessEffectSettings
        {
            return GetBundle(typeof(T));
        }

        PostProcessBundle GetBundle(Type settingsType)
        {
            Assert.IsTrue(m_Bundles.ContainsKey(settingsType), "Invalid type");
            return m_Bundles[settingsType];
        }

        internal void OverrideSettings(List<PostProcessEffectSettings> baseSettings, float interpFactor)
        {
            // Go through all settings & overriden parameters for the given volume and lerp values
            foreach (var settings in baseSettings)
            {
                if (!settings.active)
                    continue;

                var target = GetBundle(settings.GetType()).settings;
                int count = settings.parameters.Count;

                for (int i = 0; i < count; i++)
                {
                    var toParam = settings.parameters[i];
                    if (toParam.overrideState)
                    {
                        var fromParam = target.parameters[i];
                        fromParam.Interp(fromParam, toParam, interpFactor);
                    }
                }
            }
        }

        // In the legacy render loop you have to explicitely set flags on camera to tell that you
        // need depth, depth+normals or motion vectors... This won't have any effect with most
        // scriptable render pipelines.
        void SetLegacyCameraFlags(PostProcessRenderContext context)
        {
            var flags = DepthTextureMode.None;

            foreach (var bundle in m_Bundles)
            {
                if (bundle.Value.settings.IsEnabledAndSupported())
                    flags |= bundle.Value.renderer.GetLegacyCameraFlags();
            }

            // Special case for TAA
            flags |= temporalAntialiasing.GetLegacyCameraFlags();

            context.camera.depthTextureMode = flags;
        }

        // Call this function whenever you need to reset any temporal effect (TAA, Motion Blur etc).
        // Mainly used when doing camera cuts.
        public void ResetHistory()
        {
            foreach (var bundle in m_Bundles)
                bundle.Value.ResetHistory();

            temporalAntialiasing.ResetHistory();
        }

        public bool HasOpaqueOnlyEffects()
        {
            return HasActiveEffects(m_SortedBundles[PostProcessEvent.BeforeTransparent]);
        }

        bool HasActiveEffects(List<PostProcessBundle> list)
        {
            foreach (var bundle in list)
            {
                if (bundle.settings.IsEnabledAndSupported())
                    return true;
            }

            return false;
        }

        void SetupContext(PostProcessRenderContext context)
        {
            context.propertySheets = m_PropertySheetFactory;
            context.antialiasing = antialiasingMode;
            context.temporalAntialiasing = temporalAntialiasing;
            context.blueNoise = m_BlueNoise;
            SetLegacyCameraFlags(context);
        }

        void UpdateSettingsIfNeeded(PostProcessRenderContext context)
        {
#if UNITY_EDITOR
            m_IsRenderingInSceneView = UnityEditor.SceneView.currentDrawingSceneView != null
                && context.camera == UnityEditor.SceneView.currentDrawingSceneView.camera;
#endif

            // Release temporary targets used for texture lerping from last frame
            RuntimeUtilities.ReleaseLerpTargets();

            if (m_SettingsUpdateNeeded)
                VolumeManager.instance.UpdateSettings(this);

            m_SettingsUpdateNeeded = false;
        }

        // Renders before-transparent effects.
        // Make sure you check `HasOpaqueOnlyEffects()` before calling this method as it won't
        // automatically blit source into destination if no opaque effects are active.
        public void RenderOpaqueOnly(PostProcessRenderContext context)
        {
            // Update & override layer settings first (volume blending), will only be done once per
            // frame, either here or in Render() if there isn't any opaque-only effect to render.
            UpdateSettingsIfNeeded(context);

            SetupContext(context);

            RenderList(m_SortedBundles[PostProcessEvent.BeforeTransparent], context, "OpaqueOnly");
        }

        // Renders before-stack, builtin stack and after-stack effects
        // TODO: Refactor this, it's a mess and it's hard to maintain
        public void Render(PostProcessRenderContext context)
        {
            // Update & override layer settings first (volume blending) if the opaque only pass
            // hasn't been called this frame.
            UpdateSettingsIfNeeded(context);

            SetupContext(context);
            var finalDestination = context.destination;
            var cmd = context.command;

            // Do temporal anti-aliasing first
            if (context.IsTemporalAntialiasingActive() && !m_IsRenderingInSceneView)
            {
                temporalAntialiasing.SetProjectionMatrix(context.camera);

                cmd.GetTemporaryRT(Uniforms._AATemp, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                context.destination = Uniforms._AATemp;
                temporalAntialiasing.Render(context);
                context.source = Uniforms._AATemp;
                context.destination = Uniforms._TempTargetPool[4];
            }

            bool hasBeforeStack = HasActiveEffects(m_SortedBundles[PostProcessEvent.BeforeStack]);
            bool hasAfterStack = HasActiveEffects(m_SortedBundles[PostProcessEvent.AfterStack]);

            // Render builtin stack and user effects
            if (hasBeforeStack)
            {
                cmd.GetTemporaryRT(Uniforms._TempTargetPool[2], context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                context.destination = Uniforms._TempTargetPool[2];
                RenderList(m_SortedBundles[PostProcessEvent.BeforeStack], context, "BeforeStack");
                context.source = Uniforms._TempTargetPool[2];
                context.destination = Uniforms._TempTargetPool[4];
            }

            if (hasAfterStack)
            {
                cmd.GetTemporaryRT(Uniforms._TempTargetPool[3], context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                context.destination = Uniforms._TempTargetPool[3];
                RenderBuiltins(context);

                if (hasBeforeStack)
                    cmd.ReleaseTemporaryRT(Uniforms._TempTargetPool[2]);

                context.source = Uniforms._TempTargetPool[3];
                context.destination = Uniforms._TempTargetPool[4];
                cmd.GetTemporaryRT(Uniforms._TempTargetPool[4], context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                RenderList(m_SortedBundles[PostProcessEvent.AfterStack], context, "AfterStack");
                cmd.ReleaseTemporaryRT(Uniforms._TempTargetPool[3]);
            }
            else // Should be skippable if nothing's enabled in builtins
            {
                context.destination = Uniforms._TempTargetPool[4];
                cmd.GetTemporaryRT(Uniforms._TempTargetPool[4], context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                RenderBuiltins(context);
            
                if (hasBeforeStack)
                    cmd.ReleaseTemporaryRT(Uniforms._TempTargetPool[2]);
            }

            context.source = Uniforms._TempTargetPool[4];
            context.destination = finalDestination;
            RenderFinalPass(context);
            cmd.ReleaseTemporaryRT(Uniforms._TempTargetPool[4]);

            m_SettingsUpdateNeeded = true;
        }

        void RenderList(List<PostProcessBundle> list, PostProcessRenderContext context, string marker)
        {
            var cmd = context.command;
            cmd.BeginSample(marker);

            // First gather active effects - we need this to manage render targets more efficiently
            m_ActivePool.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var effect = list[i];
                if (effect.settings.IsEnabledAndSupported())
                {
                    if (!m_IsRenderingInSceneView || (m_IsRenderingInSceneView && effect.attribute.allowInSceneView))
                        m_ActivePool.Add(effect.renderer);
                }
            }

            int count = m_ActivePool.Count;
            
            // If there's only one active effect, we can simply execute it and skip the rest
            if (count == 1)
            {
                m_ActivePool[0].Render(context);
            }
            else
            {
                // Else create the target chain
                m_TargetPool.Clear();
                m_TargetPool.Add(context.source); // First target is always source

                for (int i = 0; i < count - 1; i++)
                    m_TargetPool.Add(Uniforms._TempTargetPool[i % 2]);

                m_TargetPool.Add(context.destination); // Last target is always destination

                // Render
                cmd.GetTemporaryRT(Uniforms._TempTargetPool[0], context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                if (count > 2)
                    cmd.GetTemporaryRT(Uniforms._TempTargetPool[1], context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);

                for (int i = 0; i < count; i++)
                {
                    context.source = m_TargetPool[i];
                    context.destination = m_TargetPool[i + 1];
                    m_ActivePool[i].Render(context);
                }
                
                cmd.ReleaseTemporaryRT(Uniforms._TempTargetPool[0]);
                if (count > 2)
                    cmd.ReleaseTemporaryRT(Uniforms._TempTargetPool[1]);
            }

            cmd.EndSample(marker);
        }

        void RenderBuiltins(PostProcessRenderContext context)
        {
            var uberSheet = context.propertySheets.Get("Hidden/PostProcessing/Uber");
            uberSheet.ClearKeywords();
            uberSheet.properties.Clear();
            context.uberSheet = uberSheet;
            context.autoExposureTexture = RuntimeUtilities.whiteTexture;

            var cmd = context.command;
            cmd.BeginSample("BuiltinStack");

            // Motion blur is a separate pass
            var motionBlur = GetBundle<MotionBlur>();

            if (motionBlur.settings.IsEnabledAndSupported())
            {
                var finalDestination = context.destination;
                cmd.GetTemporaryRT(Uniforms._MotionBlurTemp, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                context.destination = Uniforms._MotionBlurTemp;
                motionBlur.renderer.Render(context);
                context.source = Uniforms._MotionBlurTemp;
                context.destination = finalDestination;
            }

            // Uber effects
            RenderEffect<DepthOfField>(context);
            RenderEffect<ChromaticAberration>(context);
            RenderEffect<AutoExposure>(context);

            uberSheet.properties.SetTexture(Uniforms._AutoExposureTex, context.autoExposureTexture);

            RenderEffect<Bloom>(context);
            RenderEffect<ColorGrading>(context);
            RenderEffect<Vignette>(context);
            
            cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, 0);
            
            cmd.EndSample("BuiltinStack");
        }

        void RenderFinalPass(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("FinalPass");

            var uberSheet = context.propertySheets.Get("Hidden/PostProcessing/FinalPass");
            uberSheet.ClearKeywords();
            uberSheet.properties.Clear();
            context.uberSheet = uberSheet;

            if (antialiasingMode == Antialiasing.FastApproximateAntialiasing)
                uberSheet.EnableKeyword("FXAA");

            RenderEffect<Grain>(context);
            dithering.Render(context);

            cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, (context.flip && !m_IsRenderingInSceneView) ? 1 : 0);
            cmd.EndSample("FinalPass");
        }

        void RenderEffect<T>(PostProcessRenderContext context)
            where T : PostProcessEffectSettings
        {
            var effect = GetBundle<T>();

            if (!effect.settings.IsEnabledAndSupported())
                return;

            if (m_IsRenderingInSceneView && !effect.attribute.allowInSceneView)
                return;

            effect.renderer.Render(context);
        }

        // Debug view display
        void OnGUI()
        {
            if (!showDebugUI || !SystemInfo.supportsComputeShaders || m_CurrentContext == null)
                return;

            //debugView.OnGUI(m_CurrentContext, GetBundle<AutoExposure>());
        }
    }
}
