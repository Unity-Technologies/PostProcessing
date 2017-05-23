using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.PostProcessing
{
    // TODO: User effect sorting for this layer (ReorderableList)
    // TODO: XMLDoc everything (?)
    // TODO: Final pass should be done on an ARGB32 buffer instead of source format.
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
        public FastApproximateAntialiasing fastApproximateAntialiasing;
        public Dithering dithering;

        public PostProcessDebugView debugView;

        [SerializeField]
        PostProcessResources m_Resources;

        // Pre-ordered custom user effects
        Dictionary<PostProcessEvent, List<PostProcessBundle>> m_SortedBundles;

        // Settings/Renderer bundles mapped to settings types
        Dictionary<Type, PostProcessBundle> m_Bundles;

        PropertySheetFactory m_PropertySheetFactory;
        CommandBuffer m_LegacyCmdBufferOpaque;
        CommandBuffer m_LegacyCmdBuffer;
        Camera m_Camera;
        PostProcessRenderContext m_CurrentContext;

        bool m_SettingsUpdateNeeded = true;
        bool m_IsRenderingInSceneView = false;

        TargetPool m_TargetPool;

        // Recycled list - used to reduce GC stress when gathering active effects in a bundle list
        // on each frame
        readonly List<PostProcessEffectRenderer> m_ActiveEffects = new List<PostProcessEffectRenderer>();
        readonly List<RenderTargetIdentifier> m_Targets = new List<RenderTargetIdentifier>();

        void OnEnable()
        {
            // Load resource asset if needed
            if (m_Resources == null)
                m_Resources = PostProcessResources.instance;

            m_Bundles = new Dictionary<Type, PostProcessBundle>();
            m_SortedBundles = new Dictionary<PostProcessEvent, List<PostProcessBundle>>(new PostProcessEventComparer())
            {
                { PostProcessEvent.BeforeTransparent, new List<PostProcessBundle>() },
                { PostProcessEvent.BeforeStack,       new List<PostProcessBundle>() },
                { PostProcessEvent.AfterStack,        new List<PostProcessBundle>() }
            };

            foreach (var type in PostProcessManager.instance.settingsTypes.Keys)
            {
                var settings = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
                var bundle = new PostProcessBundle(settings);
                m_Bundles.Add(type, bundle);

                if (!bundle.attribute.builtinEffect)
                    m_SortedBundles[bundle.attribute.eventType].Add(bundle);
            }

            m_PropertySheetFactory = new PropertySheetFactory();
            m_TargetPool = new TargetPool();

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

            temporalAntialiasing.Release();

            foreach (var bundles in m_SortedBundles.Values)
                bundles.Clear();

            foreach (var bundle in m_Bundles.Values)
                bundle.Release();

            m_Bundles.Clear();
            m_SortedBundles.Clear();
            m_PropertySheetFactory.Release();
        }

        // Called everytime the user resets the component from the inspector and more importantly
        // the first time it's added to a GameObject. As we don't have added/removed event for
        // components, this will do fine
        void Reset()
        {
            volumeTrigger = transform;
        }

        void OnPreCull()
        {
            // Unused in scriptable render pipelines
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            var sourceFormat = m_Camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            int tempRt = m_TargetPool.Get();

            m_CurrentContext.Reset();
            m_CurrentContext.camera = m_Camera;
            m_CurrentContext.sourceFormat = sourceFormat;
            m_CurrentContext.source = new RenderTargetIdentifier(tempRt);
            m_CurrentContext.destination = BuiltinRenderTextureType.CameraTarget;

            // Update & override layer settings first (volume blending)
            UpdateSettingsIfNeeded();

            m_LegacyCmdBufferOpaque.Clear();
            m_LegacyCmdBuffer.Clear();

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
            return HasActiveEffects(PostProcessEvent.BeforeTransparent);
        }

        public bool HasActiveEffects(PostProcessEvent evt)
        {
            var list = m_SortedBundles[evt];

            foreach (var bundle in list)
            {
                if (bundle.settings.IsEnabledAndSupported())
                    return true;
            }

            return false;
        }

        void SetupContext(PostProcessRenderContext context)
        {
            m_IsRenderingInSceneView = context.camera.cameraType == CameraType.SceneView;
            context.isSceneView = m_IsRenderingInSceneView;
            context.resources = m_Resources;
            context.propertySheets = m_PropertySheetFactory;
            context.antialiasing = antialiasingMode;
            context.temporalAntialiasing = temporalAntialiasing;
            SetLegacyCameraFlags(context);

            // Unsafe to keep this around but we need it for OnGUI events for debug views
            m_CurrentContext = context;
        }

        void UpdateSettingsIfNeeded()
        {
            // Release temporary targets used for texture lerping from last frame
            RuntimeUtilities.ReleaseLerpTargets();

            if (m_SettingsUpdateNeeded)
            {
                PostProcessManager.instance.UpdateSettings(this);
                m_TargetPool.Reset();
            }

            m_SettingsUpdateNeeded = false;
        }

        // Renders before-transparent effects.
        // Make sure you check `HasOpaqueOnlyEffects()` before calling this method as it won't
        // automatically blit source into destination if no opaque effects are active.
        public void RenderOpaqueOnly(PostProcessRenderContext context)
        {
            // Update & override layer settings first (volume blending), will only be done once per
            // frame, either here or in Render() if there isn't any opaque-only effect to render.
            UpdateSettingsIfNeeded();

            SetupContext(context);

            RenderList(m_SortedBundles[PostProcessEvent.BeforeTransparent], context, "OpaqueOnly");
        }

        // Renders everything not opaque-only
        //
        // Current order of operation is as following:
        //     1. Pre-stack
        //     2. Built-in stack
        //     3. Post-stack
        //     4. Built-in final pass
        //
        // Final pass should be skipped when outputting to a HDR display.
        public void Render(PostProcessRenderContext context)
        {
            // Update & override layer settings first (volume blending) if the opaque only pass
            // hasn't been called this frame.
            UpdateSettingsIfNeeded();

            SetupContext(context);

            // Do temporal anti-aliasing first
            int lastTarget = -1;
            if (context.IsTemporalAntialiasingActive() && !context.isSceneView)
            {
                temporalAntialiasing.SetProjectionMatrix(context.camera);
                lastTarget = m_TargetPool.Get();
                RenderEffect(context, lastTarget, temporalAntialiasing.Render);
            }

            // Right before the builtin stack
            lastTarget = RenderInjectionPoint(PostProcessEvent.BeforeStack, context, "BeforeStack", lastTarget);

            // Builtin stack
            lastTarget = RenderBuiltins(context, lastTarget);

            // After the builtin stack but before the final pass (before FXAA & Dithering)
            lastTarget = RenderInjectionPoint(PostProcessEvent.AfterStack, context, "AfterStack", lastTarget);

            // And close with the final pass
            RenderFinalPass(context, lastTarget);

            m_SettingsUpdateNeeded = true;
        }

        int RenderInjectionPoint(PostProcessEvent evt, PostProcessRenderContext context, string marker, int releaseTargetAfterUse = -1)
        {
            // Make sure we have active effects in this injection point, skip it otherwise
            bool hasActiveEffects = HasActiveEffects(evt);

            if (!hasActiveEffects)
                return releaseTargetAfterUse;

            int tempTarget = m_TargetPool.Get();
            var finalDestination = context.destination;

            var cmd = context.command;
            cmd.GetTemporaryRT(tempTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
            context.destination = tempTarget;
            RenderList(m_SortedBundles[evt], context, marker);
            context.source = tempTarget;
            context.destination = finalDestination;

            if (releaseTargetAfterUse > -1)
                cmd.ReleaseTemporaryRT(releaseTargetAfterUse);

            return tempTarget;
        }

        void RenderList(List<PostProcessBundle> list, PostProcessRenderContext context, string marker)
        {
            var cmd = context.command;
            cmd.BeginSample(marker);

            // First gather active effects - we need this to manage render targets more efficiently
            m_ActiveEffects.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var effect = list[i];
                if (effect.settings.IsEnabledAndSupported())
                {
                    if (!context.isSceneView || (context.isSceneView && effect.attribute.allowInSceneView))
                        m_ActiveEffects.Add(effect.renderer);
                }
            }

            int count = m_ActiveEffects.Count;
            
            // If there's only one active effect, we can simply execute it and skip the rest
            if (count == 1)
            {
                m_ActiveEffects[0].Render(context);
            }
            else
            {
                // Else create the target chain
                m_Targets.Clear();
                m_Targets.Add(context.source); // First target is always source

                int tempTarget1 = m_TargetPool.Get();
                int tempTarget2 = m_TargetPool.Get();

                for (int i = 0; i < count - 1; i++)
                    m_Targets.Add(i % 2 == 0 ? tempTarget1 : tempTarget2);

                m_Targets.Add(context.destination); // Last target is always destination

                // Render
                cmd.GetTemporaryRT(tempTarget1, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                if (count > 2)
                    cmd.GetTemporaryRT(tempTarget2, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);

                for (int i = 0; i < count; i++)
                {
                    context.source = m_Targets[i];
                    context.destination = m_Targets[i + 1];
                    m_ActiveEffects[i].Render(context);
                }
                
                cmd.ReleaseTemporaryRT(tempTarget1);
                if (count > 2)
                    cmd.ReleaseTemporaryRT(tempTarget2);
            }

            cmd.EndSample(marker);
        }

        int RenderBuiltins(PostProcessRenderContext context, int releaseTargetAfterUse = -1)
        {
            var uberSheet = context.propertySheets.Get(context.resources.shaders.uber);
            uberSheet.ClearKeywords();
            uberSheet.properties.Clear();
            context.uberSheet = uberSheet;
            context.autoExposureTexture = RuntimeUtilities.whiteTexture;

            var cmd = context.command;
            cmd.BeginSample("BuiltinStack");

            // Render to an intermediate target as this won't be the final pass
            int tempTarget = m_TargetPool.Get();
            cmd.GetTemporaryRT(tempTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
            var finalDestination = context.destination;
            context.destination = tempTarget;

            // Motion blur is a separate pass - could potentially be done after DoF depending on the
            // kind of results you're looking for...
            int motionBlurTarget = RenderEffect<MotionBlur>(context, true);

            // Depth of field final combination pass used to be done in Uber which led to artifacts
            // when used at the same time as Bloom (because both effects used the same source, so
            // the stronger bloom was, the more DoF was eaten away in out of focus areas)
            int depthOfFieldTarget = RenderEffect<DepthOfField>(context, true);

            // Uber effects
            RenderEffect<ChromaticAberration>(context);
            RenderEffect<AutoExposure>(context);

            uberSheet.properties.SetTexture(Uniforms._AutoExposureTex, context.autoExposureTexture);

            RenderEffect<Bloom>(context);
            RenderEffect<ColorGrading>(context);
            RenderEffect<Vignette>(context);
            RenderEffect<Grain>(context);
            
            cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, 0);

            context.source = context.destination;
            context.destination = finalDestination;

            if (releaseTargetAfterUse > -1) cmd.ReleaseTemporaryRT(releaseTargetAfterUse);
            if (motionBlurTarget > -1) cmd.ReleaseTemporaryRT(motionBlurTarget);
            if (depthOfFieldTarget > -1) cmd.ReleaseTemporaryRT(motionBlurTarget);
            
            cmd.EndSample("BuiltinStack");

            return tempTarget;
        }

        // This pass will have to be disabled for HDR screen output as it's an LDR pass
        void RenderFinalPass(PostProcessRenderContext context, int releaseTargetAfterUse = -1)
        {
            var cmd = context.command;
            cmd.BeginSample("FinalPass");

            var uberSheet = context.propertySheets.Get(context.resources.shaders.finalPass);
            uberSheet.ClearKeywords();
            uberSheet.properties.Clear();
            context.uberSheet = uberSheet;

            if (antialiasingMode == Antialiasing.FastApproximateAntialiasing)
            {
                uberSheet.EnableKeyword(fastApproximateAntialiasing.mobileOptimized
                    ? "FXAA_LOW"
                    : "FXAA"
                );
            }

            dithering.Render(context);

            cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, (context.flip && !context.isSceneView) ? 1 : 0);

            if (releaseTargetAfterUse > -1)
                cmd.ReleaseTemporaryRT(releaseTargetAfterUse);

            cmd.EndSample("FinalPass");
        }

        // TODO: Only used by TAA, which needs to become a PostProcessEffectRenderer and use the other RenderEffect() instead.
        int RenderEffect(PostProcessRenderContext context, int tempTarget, Action<PostProcessRenderContext> renderFunc)
        {
            var finalDestination = context.destination;

            context.command.GetTemporaryRT(tempTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
            context.destination = tempTarget;
            renderFunc(context);
            context.source = tempTarget;
            context.destination = finalDestination;

            return tempTarget;
        }

        int RenderEffect<T>(PostProcessRenderContext context, bool useTempTarget = false)
            where T : PostProcessEffectSettings
        {
            var effect = GetBundle<T>();

            if (!effect.settings.IsEnabledAndSupported())
                return -1;

            if (m_IsRenderingInSceneView && !effect.attribute.allowInSceneView)
                return -1;

            if (!useTempTarget)
            {
                effect.renderer.Render(context);
                return -1;
            }

            var finalDestination = context.destination;
            var tempTarget = m_TargetPool.Get();
            context.command.GetTemporaryRT(tempTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
            context.destination = tempTarget;
            effect.renderer.Render(context);
            context.source = tempTarget;
            context.destination = finalDestination;
            return tempTarget;
        }

        // Debug view display
        void OnGUI()
        {
            debugView.OnGUI(m_CurrentContext);
        }
    }
}
