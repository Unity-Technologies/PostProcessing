using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    // TODO: XMLDoc everything (?)
    // TODO: Add "keep alpha" checkbox
    [DisallowMultipleComponent, ExecuteInEditMode, ImageEffectAllowedInSceneView]
    [AddComponentMenu("Rendering/Post-process Layer", -1)]
    [RequireComponent(typeof(Camera))]
    public sealed class PostProcessLayer : MonoBehaviour
    {
        public enum Antialiasing
        {
            None,
            FastApproximateAntialiasing,
            SubpixelMorphologicalAntialiasing,
            TemporalAntialiasing
        }

        // Settings
        public Transform volumeTrigger;
        public LayerMask volumeLayer;
        public bool stopNaNPropagation = true;

        // Builtins / hardcoded effects that don't benefit from volume blending
        public Antialiasing antialiasingMode = Antialiasing.None;
        public TemporalAntialiasing temporalAntialiasing;
        public SubpixelMorphologicalAntialiasing subpixelMorphologicalAntialiasing;
        public FastApproximateAntialiasing fastApproximateAntialiasing;
        public AmbientOcclusion ambientOcclusion;
        public Fog fog;
        public Dithering dithering;

        public PostProcessMonitors monitors;

        [SerializeField]
        PostProcessResources m_Resources;

        // UI states
        [SerializeField] bool m_ShowRenderingFeatures;
        [SerializeField] bool m_ShowToolkit;
        [SerializeField] bool m_ShowCustomSorter;

        // Will stop applying post-processing effects just before color grading is applied
        // Currently used to export to exr without color grading
        public bool breakBeforeColorGrading = false;

        // Pre-ordered custom user effects
        // These are automatically populated and made to work properly with the serialization
        // system AND the editor. Modify at your own risk.
        [Serializable]
        public sealed class SerializedBundleRef
        {
            // We can't serialize Type so use assemblyQualifiedName instead, we only need this at
            // init time anyway so it's fine
            public string assemblyQualifiedName;

            // Not serialized, is set/reset when deserialization kicks in
            public PostProcessBundle bundle;
        }

        [SerializeField]
        List<SerializedBundleRef> m_BeforeTransparentBundles;

        [SerializeField]
        List<SerializedBundleRef> m_BeforeStackBundles;

        [SerializeField]
        List<SerializedBundleRef> m_AfterStackBundles;

        public Dictionary<PostProcessEvent, List<SerializedBundleRef>> sortedBundles { get; private set; }

        // We need to keep track of bundle initialization because for some obscure reason, on
        // assembly reload a MonoBehavior's Editor OnEnable will be called BEFORE the MonoBehavior's
        // own OnEnable... So we'll use it to pre-init bundles if the layer inspector is opened and
        // the component hasn't been enabled yet.
        public bool haveBundlesBeenInited { get; private set; }

        // Settings/Renderer bundles mapped to settings types
        Dictionary<Type, PostProcessBundle> m_Bundles;

        PropertySheetFactory m_PropertySheetFactory;
        CommandBuffer m_LegacyCmdBufferBeforeReflections;
        CommandBuffer m_LegacyCmdBufferOpaque;
        CommandBuffer m_LegacyCmdBuffer;
        Camera m_Camera;
        PostProcessRenderContext m_CurrentContext;
        LogHistogram m_LogHistogram;

        bool m_SettingsUpdateNeeded = true;
        bool m_IsRenderingInSceneView = false;

        TargetPool m_TargetPool;

        bool m_NaNKilled = false;

        // Recycled list - used to reduce GC stress when gathering active effects in a bundle list
        // on each frame
        readonly List<PostProcessEffectRenderer> m_ActiveEffects = new List<PostProcessEffectRenderer>();
        readonly List<RenderTargetIdentifier> m_Targets = new List<RenderTargetIdentifier>();

        void OnEnable()
        {
            if (!haveBundlesBeenInited)
                InitBundles();

            m_LogHistogram = new LogHistogram();
            m_PropertySheetFactory = new PropertySheetFactory();
            m_TargetPool = new TargetPool();

            if (monitors == null)
                monitors = new PostProcessMonitors();

            monitors.OnEnable();

            // Scriptable render pipelines handle their own command buffers
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            m_LegacyCmdBufferBeforeReflections = new CommandBuffer { name = "Deferred Ambient Occlusion" };
            m_LegacyCmdBufferOpaque = new CommandBuffer { name = "Opaque Only Post-processing" };
            m_LegacyCmdBuffer = new CommandBuffer { name = "Post-processing" };

            m_Camera = GetComponent<Camera>();
            m_Camera.AddCommandBuffer(CameraEvent.BeforeReflections, m_LegacyCmdBufferBeforeReflections);
            m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_LegacyCmdBufferOpaque);
            m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, m_LegacyCmdBuffer);

            // Internal context used if no SRP is set
            m_CurrentContext = new PostProcessRenderContext();
        }

        public void InitBundles()
        {
            // Create these lists only once, the serialization system will take over after that
            if (m_BeforeTransparentBundles == null)
                m_BeforeTransparentBundles = new List<SerializedBundleRef>();

            if (m_BeforeStackBundles == null)
                m_BeforeStackBundles = new List<SerializedBundleRef>();

            if (m_AfterStackBundles == null)
                m_AfterStackBundles = new List<SerializedBundleRef>();

            // Create a bundle for each effect type
            m_Bundles = new Dictionary<Type, PostProcessBundle>();

            foreach (var type in PostProcessManager.instance.settingsTypes.Keys)
            {
                var settings = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
                var bundle = new PostProcessBundle(settings);
                m_Bundles.Add(type, bundle);
            }

            // Update sorted lists with newly added or removed effects in the assemblies
            UpdateBundleSortList(m_BeforeTransparentBundles, PostProcessEvent.BeforeTransparent);
            UpdateBundleSortList(m_BeforeStackBundles, PostProcessEvent.BeforeStack);
            UpdateBundleSortList(m_AfterStackBundles, PostProcessEvent.AfterStack);

            // Push all sorted lists in a dictionary for easier access
            sortedBundles = new Dictionary<PostProcessEvent, List<SerializedBundleRef>>(new PostProcessEventComparer())
            {
                { PostProcessEvent.BeforeTransparent, m_BeforeTransparentBundles },
                { PostProcessEvent.BeforeStack,       m_BeforeStackBundles },
                { PostProcessEvent.AfterStack,        m_AfterStackBundles }
            };

            // Done
            haveBundlesBeenInited = true;
        }

        void UpdateBundleSortList(List<SerializedBundleRef> sortedList, PostProcessEvent evt)
        {
            // First get all effects associated with the injection point
            var effects = m_Bundles.Where(kvp => kvp.Value.attribute.eventType == evt && !kvp.Value.attribute.builtinEffect)
                                   .Select(kvp => kvp.Value)
                                   .ToList();

            // Remove types that don't exist anymore
            sortedList.RemoveAll(x =>
            {
                string searchStr = x.assemblyQualifiedName;
                return !effects.Exists(b => b.settings.GetType().AssemblyQualifiedName == searchStr);
            });

            // Add new ones
            foreach (var effect in effects)
            {
                string typeName = effect.settings.GetType().AssemblyQualifiedName;

                if (!sortedList.Exists(b => b.assemblyQualifiedName == typeName))
                {
                    var sbr = new SerializedBundleRef { assemblyQualifiedName = typeName };
                    sortedList.Add(sbr);
                }
            }

            // Link internal references
            foreach (var effect in sortedList)
            {
                string typeName = effect.assemblyQualifiedName;
                var bundle = effects.Find(b => b.settings.GetType().AssemblyQualifiedName == typeName);
                effect.bundle = bundle;
            }
        }

        void OnDisable()
        {
            if (!RuntimeUtilities.scriptableRenderPipelineActive)
            {
                m_Camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, m_LegacyCmdBufferBeforeReflections);
                m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_LegacyCmdBufferOpaque);
                m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, m_LegacyCmdBuffer);
            }

            temporalAntialiasing.Release();
            m_LogHistogram.Release();

            foreach (var bundle in m_Bundles.Values)
                bundle.Release();

            m_Bundles.Clear();
            m_PropertySheetFactory.Release();

            if (monitors != null)
                monitors.OnDisable();

            // Might be an issue if several layers are blending in the same frame...
            TextureLerper.instance.Clear();

            haveBundlesBeenInited = false;
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

            var context = m_CurrentContext;
            var sourceFormat = m_Camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            context.Reset();
            context.camera = m_Camera;
            context.sourceFormat = sourceFormat;

            m_LegacyCmdBufferBeforeReflections.Clear();
            m_LegacyCmdBufferOpaque.Clear();
            m_LegacyCmdBuffer.Clear();

            SetupContext(context);

            // Lighting & opaque-only effects
            int opaqueOnlyEffects = 0;
            bool hasCustomOpaqueOnlyEffects = HasOpaqueOnlyEffects(context);
            bool isAmbientOcclusionDeferred = ambientOcclusion.IsEnabledAndSupported(context) && ambientOcclusion.IsAmbientOnly(context);
            bool isAmbientOcclusionOpaque = ambientOcclusion.IsEnabledAndSupported(context) && !ambientOcclusion.IsAmbientOnly(context);
            bool isFogActive = fog.IsEnabledAndSupported(context);

            // Ambient-only AO is done in a separate command buffer, before reflections
            if (isAmbientOcclusionDeferred)
            {
                context.command = m_LegacyCmdBufferBeforeReflections;
                ambientOcclusion.RenderAmbientOnly(context);
            }
            else if (isAmbientOcclusionOpaque)
            {
                opaqueOnlyEffects++;
            }

            opaqueOnlyEffects += isFogActive ? 1 : 0;
            opaqueOnlyEffects += hasCustomOpaqueOnlyEffects ? 1 : 0;

            var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

            if (opaqueOnlyEffects > 0)
            {
                var cmd = m_LegacyCmdBufferOpaque;
                context.command = cmd;

                // We need to use the internal Blit method to copy the camera target or it'll fail
                // on tiled GPU as it won't be able to resolve
                int tempTarget0 = m_TargetPool.Get();
                cmd.GetTemporaryRT(tempTarget0, context.width, context.height, 24, FilterMode.Bilinear, sourceFormat);
                cmd.Blit(cameraTarget, tempTarget0);
                context.source = tempTarget0;

                int tempTarget1 = -1;

                if (opaqueOnlyEffects > 1)
                {
                    tempTarget1 = m_TargetPool.Get();
                    cmd.GetTemporaryRT(tempTarget1, context.width, context.height, 24, FilterMode.Bilinear, sourceFormat);
                    context.destination = tempTarget1;
                }
                else
                {
                    context.destination = cameraTarget;
                }

                if (isAmbientOcclusionOpaque)
                {
                    ambientOcclusion.RenderAfterOpaque(context);
                    opaqueOnlyEffects--;
                    var prevSource = context.source;
                    context.source = context.destination;
                    context.destination = opaqueOnlyEffects == 1 ? cameraTarget : prevSource;
                }

                // TODO: Insert SSR here

                if (isFogActive)
                {
                    fog.Render(context);
                    opaqueOnlyEffects--;
                    var prevSource = context.source;
                    context.source = context.destination;
                    context.destination = opaqueOnlyEffects == 1 ? cameraTarget : prevSource;
                }

                if (hasCustomOpaqueOnlyEffects)
                {
                    RenderOpaqueOnly(context);
                }

                if (opaqueOnlyEffects > 1)
                    cmd.ReleaseTemporaryRT(tempTarget1);

                cmd.ReleaseTemporaryRT(tempTarget0);
            }

            // Post-transparency stack
            // Same as before, first blit needs to use the builtin Blit command to properly handle
            // tiled GPUs
            int tempRt = m_TargetPool.Get();

            m_LegacyCmdBuffer.GetTemporaryRT(tempRt, context.width, context.height, 24, FilterMode.Bilinear, sourceFormat);
            m_LegacyCmdBuffer.SetGlobalTexture(ShaderIDs.MainTex, cameraTarget);
            m_LegacyCmdBuffer.Blit(cameraTarget, tempRt, RuntimeUtilities.copyMaterial, stopNaNPropagation ? 3 : 2);
            m_NaNKilled = stopNaNPropagation;

            context.command = m_LegacyCmdBuffer;
            context.source = tempRt;
            context.destination = cameraTarget;
            Render(context);
            m_LegacyCmdBuffer.ReleaseTemporaryRT(tempRt);
        }

        void OnPostRender()
        {
            // Unused in scriptable render pipelines
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            if (m_CurrentContext.IsTemporalAntialiasingActive())
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
            var flags = context.camera.depthTextureMode;

            foreach (var bundle in m_Bundles)
            {
                if (bundle.Value.settings.IsEnabledAndSupported(context))
                    flags |= bundle.Value.renderer.GetCameraFlags();
            }

            // Special case for AA & lighting effects
            if (context.IsTemporalAntialiasingActive())
                flags |= temporalAntialiasing.GetCameraFlags();

            if (ambientOcclusion.IsEnabledAndSupported(context) && !ambientOcclusion.IsAmbientOnly(context))
                flags |= ambientOcclusion.GetCameraFlags();

            if (fog.IsEnabledAndSupported(context))
                flags |= fog.GetCameraFlags();

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

        public bool HasOpaqueOnlyEffects(PostProcessRenderContext context)
        {
            return HasActiveEffects(PostProcessEvent.BeforeTransparent, context);
        }

        public bool HasActiveEffects(PostProcessEvent evt, PostProcessRenderContext context)
        {
            var list = sortedBundles[evt];

            foreach (var item in list)
            {
                if (item.bundle.settings.IsEnabledAndSupported(context))
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
            context.logHistogram = m_LogHistogram;
            SetLegacyCameraFlags(context);

            // Unsafe to keep this around but we need it for OnGUI events for debug views
            // Will be removed eventually
            m_CurrentContext = context;
        }

        void UpdateSettingsIfNeeded(PostProcessRenderContext context)
        {
            if (m_SettingsUpdateNeeded)
            {
                context.command.BeginSample("VolumeBlending");
                PostProcessManager.instance.UpdateSettings(this);
                context.command.EndSample("VolumeBlending");
                m_TargetPool.Reset();
            }

            m_SettingsUpdateNeeded = false;
        }

        // Renders before-transparent effects.
        // Make sure you check `HasOpaqueOnlyEffects()` before calling this method as it won't
        // automatically blit source into destination if no opaque effects are active.
        public void RenderOpaqueOnly(PostProcessRenderContext context)
        {
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                SetupContext(context);

            TextureLerper.instance.BeginFrame(context);

            // Update & override layer settings first (volume blending), will only be done once per
            // frame, either here or in Render() if there isn't any opaque-only effect to render.
            UpdateSettingsIfNeeded(context);

            RenderList(sortedBundles[PostProcessEvent.BeforeTransparent], context, "OpaqueOnly");
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
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                SetupContext(context);

            TextureLerper.instance.BeginFrame(context);
            var cmd = context.command;

            // Update & override layer settings first (volume blending) if the opaque only pass
            // hasn't been called this frame.
            UpdateSettingsIfNeeded(context);

            // Do a NaN killing pass if needed
            int lastTarget = -1;
            if (stopNaNPropagation && !m_NaNKilled)
            {
                lastTarget = m_TargetPool.Get();
                cmd.GetTemporaryRT(lastTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                cmd.BlitFullscreenTriangle(context.source, lastTarget, RuntimeUtilities.copySheet, 1);
                context.source = lastTarget;
                m_NaNKilled = true;
            }

            // Do temporal anti-aliasing first
            if (context.IsTemporalAntialiasingActive())
            {
                if (!RuntimeUtilities.scriptableRenderPipelineActive)
                {
                    var camera = context.camera;
                    camera.nonJitteredProjectionMatrix = camera.projectionMatrix;
                    camera.projectionMatrix = temporalAntialiasing.GetJitteredProjectionMatrix(camera);
                    camera.useJitteredProjectionMatrixForTransparentRendering = false;
                }

                var taaTarget = m_TargetPool.Get();
                var finalDestination = context.destination;
                cmd.GetTemporaryRT(taaTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                context.destination = taaTarget;
                temporalAntialiasing.Render(context);
                context.source = taaTarget;
                context.destination = finalDestination;

                if (lastTarget > -1)
                    cmd.ReleaseTemporaryRT(lastTarget);

                lastTarget = taaTarget;
            }

            bool hasBeforeStackEffects = HasActiveEffects(PostProcessEvent.BeforeStack, context);
            bool hasAfterStackEffects = HasActiveEffects(PostProcessEvent.AfterStack, context) && !breakBeforeColorGrading;
            bool needsFinalPass = (hasAfterStackEffects
                || (antialiasingMode == Antialiasing.FastApproximateAntialiasing) || (antialiasingMode == Antialiasing.SubpixelMorphologicalAntialiasing && subpixelMorphologicalAntialiasing.IsSupported()))
                && !breakBeforeColorGrading;

            // Right before the builtin stack
            if (hasBeforeStackEffects)
                lastTarget = RenderInjectionPoint(PostProcessEvent.BeforeStack, context, "BeforeStack", lastTarget);

            // Builtin stack
            lastTarget = RenderBuiltins(context, !needsFinalPass, lastTarget);

            // After the builtin stack but before the final pass (before FXAA & Dithering)
            if (hasAfterStackEffects)
                lastTarget = RenderInjectionPoint(PostProcessEvent.AfterStack, context, "AfterStack", lastTarget);

            // And close with the final pass
            if (needsFinalPass)
                RenderFinalPass(context, lastTarget);

            // Render debug monitors if needed
            monitors.Render(context);

            TextureLerper.instance.EndFrame();
            m_SettingsUpdateNeeded = true;
            m_NaNKilled = false;
        }

        int RenderInjectionPoint(PostProcessEvent evt, PostProcessRenderContext context, string marker, int releaseTargetAfterUse = -1)
        {
            int tempTarget = m_TargetPool.Get();
            var finalDestination = context.destination;

            var cmd = context.command;
            cmd.GetTemporaryRT(tempTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
            context.destination = tempTarget;
            RenderList(sortedBundles[evt], context, marker);
            context.source = tempTarget;
            context.destination = finalDestination;

            if (releaseTargetAfterUse > -1)
                cmd.ReleaseTemporaryRT(releaseTargetAfterUse);

            return tempTarget;
        }

        void RenderList(List<SerializedBundleRef> list, PostProcessRenderContext context, string marker)
        {
            var cmd = context.command;
            cmd.BeginSample(marker);

            // First gather active effects - we need this to manage render targets more efficiently
            m_ActiveEffects.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var effect = list[i].bundle;
                if (effect.settings.IsEnabledAndSupported(context))
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

        int RenderBuiltins(PostProcessRenderContext context, bool isFinalPass, int releaseTargetAfterUse = -1)
        {
            var uberSheet = context.propertySheets.Get(context.resources.shaders.uber);
            uberSheet.ClearKeywords();
            uberSheet.properties.Clear();
            context.uberSheet = uberSheet;
            context.autoExposureTexture = RuntimeUtilities.whiteTexture;

            var cmd = context.command;
            cmd.BeginSample("BuiltinStack");

            int tempTarget = -1;
            var finalDestination = context.destination;

            if (!isFinalPass)
            {
                // Render to an intermediate target as this won't be the final pass
                tempTarget = m_TargetPool.Get();
                cmd.GetTemporaryRT(tempTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                context.destination = tempTarget;
            }

            // Depth of field final combination pass used to be done in Uber which led to artifacts
            // when used at the same time as Bloom (because both effects used the same source, so
            // the stronger bloom was, the more DoF was eaten away in out of focus areas)
            int depthOfFieldTarget = RenderEffect<DepthOfField>(context, true);

            // Motion blur is a separate pass - could potentially be done after DoF depending on the
            // kind of results you're looking for...
            int motionBlurTarget = RenderEffect<MotionBlur>(context, true);

            // Prepare exposure histogram if needed
            if (ShouldGenerateLogHistogram(context))
                m_LogHistogram.Generate(context);

            // Uber effects
            RenderEffect<AutoExposure>(context);
            uberSheet.properties.SetTexture(ShaderIDs.AutoExposureTex, context.autoExposureTexture);

            RenderEffect<ChromaticAberration>(context);
            RenderEffect<Bloom>(context);
            RenderEffect<Vignette>(context);
            RenderEffect<Grain>(context);

            if (!breakBeforeColorGrading)
                RenderEffect<ColorGrading>(context);

            int pass = 0;

            if (isFinalPass)
            {
                uberSheet.EnableKeyword("FINALPASS");
                dithering.Render(context);

                if (context.flip && !context.isSceneView)
                    pass = 1;
            }

            cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, pass);

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

            if (breakBeforeColorGrading)
            {
                var sheet = context.propertySheets.Get(context.resources.shaders.discardAlpha);
                cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            }
            else
            {
                var uberSheet = context.propertySheets.Get(context.resources.shaders.finalPass);
                uberSheet.ClearKeywords();
                uberSheet.properties.Clear();
                context.uberSheet = uberSheet;
                int tempTarget = -1;

                if (antialiasingMode == Antialiasing.FastApproximateAntialiasing)
                {
                    uberSheet.EnableKeyword(fastApproximateAntialiasing.mobileOptimized
                        ? "FXAA_LOW"
                        : "FXAA"
                    );
                }
                else if (antialiasingMode == Antialiasing.SubpixelMorphologicalAntialiasing && subpixelMorphologicalAntialiasing.IsSupported())
                {
                    tempTarget = m_TargetPool.Get();
                    var finalDestination = context.destination;
                    context.command.GetTemporaryRT(tempTarget, context.width, context.height, 24, FilterMode.Bilinear, context.sourceFormat);
                    context.destination = tempTarget;
                    subpixelMorphologicalAntialiasing.Render(context);
                    context.source = tempTarget;
                    context.destination = finalDestination;
                }

                dithering.Render(context);

                cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, (context.flip && !context.isSceneView) ? 1 : 0);

                if (tempTarget > -1)
                    cmd.ReleaseTemporaryRT(tempTarget);
            }

            if (releaseTargetAfterUse > -1)
                cmd.ReleaseTemporaryRT(releaseTargetAfterUse);

            cmd.EndSample("FinalPass");
        }

        int RenderEffect<T>(PostProcessRenderContext context, bool useTempTarget = false)
            where T : PostProcessEffectSettings
        {
            var effect = GetBundle<T>();

            if (!effect.settings.IsEnabledAndSupported(context))
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

        bool ShouldGenerateLogHistogram(PostProcessRenderContext context)
        {
            bool autoExpo = GetBundle<AutoExposure>().settings.IsEnabledAndSupported(context);
            bool lightMeter = monitors.lightMeter.IsRequestedAndSupported();
            return autoExpo || lightMeter;
        }
    }
}
