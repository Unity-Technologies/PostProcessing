using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System;

namespace UnityEditor.Rendering.PostProcessing
{
    public sealed class PostProcessResourceStripper : ScriptableObject
    {
        [SerializeField] PostProcessResources resources;
        [SerializeField] PostProcessResources unstrippedResources;
        [SerializeField] PostProcessStrippingConfig stripping;

        public const string DefaultStrippingConfigAssetPath = "Assets/PostProcessStrippingConfig.asset";
        bool enabled = true;

        static PostProcessResourceStripper s_Instance;

        public static PostProcessResourceStripper instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = CreateInstance<PostProcessResourceStripper>();
                    s_Instance.unstrippedResources.changeHandler = Update;
                }

                return s_Instance;
            }
        }

        static string FindPostProcessStrippingConfigGUID()
        {
            var guids = AssetDatabase.FindAssets("t:PostProcessStrippingConfig", null);
            if (guids.Length > 0)
                return guids[0];
            else
                return null;
        }

        public static string EnsurePostProcessStrippingConfigAssetExists()
        {
            var guid = FindPostProcessStrippingConfigGUID();
            if (guid != null)
                return guid;

            bool wasEnabled = instance.enabled;
            instance.enabled = false;
            AssetDatabase.CreateAsset(CreateInstance<PostProcessStrippingConfig>(), DefaultStrippingConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            instance.enabled = wasEnabled;
            return FindPostProcessStrippingConfigGUID();
        }

        void LazyLoadStrippingConfig()
        {
            if (stripping != null)
                return;

            var guid = EnsurePostProcessStrippingConfigAssetExists();
            if (guid != null)
            {
                bool wasEnabled = instance.enabled;
                instance.enabled = false;
                stripping = (PostProcessStrippingConfig)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(PostProcessStrippingConfig));
                instance.enabled = wasEnabled;
            }
        }

        void OnDestroy()
        {
            unstrippedResources.changeHandler = null;
        }

        void StripMultiScaleAO()
        {
            resources.computeShaders.multiScaleAODownsample1 = null;
            resources.computeShaders.multiScaleAODownsample2 = null;
            resources.computeShaders.multiScaleAORender = null;
            resources.computeShaders.multiScaleAOUpsample = null;
            resources.shaders.multiScaleAO = null;
        }

        void StripScreenSpaceReflections()
        {
            resources.shaders.screenSpaceReflections = null;
            resources.computeShaders.gaussianDownsample = null;
        }

        void StripAutoExposure()
        {
            resources.computeShaders.autoExposure = null;
            resources.computeShaders.exposureHistogram = null;
        }

        void StripDebugShaders()
        {
            resources.shaders.lightMeter = null;
            resources.shaders.gammaHistogram = null;
            resources.shaders.waveform = null;
            resources.shaders.vectorscope = null;
            resources.shaders.debugOverlays = null;

            resources.computeShaders.gammaHistogram = null;
            resources.computeShaders.waveform = null;
            resources.computeShaders.vectorscope = null;
        }

        void Apply(BuildTarget target)
        {
            if (!enabled)
                return;

            if (resources == null)
                return;

            if (unstrippedResources == null)
                return;

            LazyLoadStrippingConfig();
            if (stripping == null)
                return;

            resources.computeShaders = unstrippedResources.computeShaders.Clone();
            resources.shaders = unstrippedResources.shaders.Clone();

            // We don't support multi scale AO on mobile
            if (stripping.stripUnsupportedShaders &&
                (target == BuildTarget.Android || target == BuildTarget.iOS || target == BuildTarget.tvOS))
            {
                StripMultiScaleAO();
            }

            if (stripping.stripDebugShaders)
            {
                StripDebugShaders();
            }

            if (stripping.stripComputeShaders)
            {
                resources.computeShaders = new PostProcessResources.ComputeShaders();
                StripScreenSpaceReflections();
                StripMultiScaleAO();
                StripAutoExposure();
                StripDebugShaders();
            }

            if (stripping.stripUnsupportedShaders && !RuntimeUtilities.supportsDeferredShading)
            {
                StripScreenSpaceReflections();
                resources.shaders.deferredFog = null;
                if (!RuntimeUtilities.supportsDepthNormals)
                    resources.shaders.scalableAO = null;
            }

            if (stripping.stripUnsupportedShaders && !SystemInfo.supportsMotionVectors)
            {
                resources.shaders.motionBlur = null;
                resources.shaders.temporalAntialiasing = null;
                resources.shaders.screenSpaceReflections = null;
                resources.computeShaders.gaussianDownsample = null;
            }
        }

        public static void Update()
        {
            Update(EditorUserBuildSettings.activeBuildTarget);
        }

        public static void Update(BuildTarget target)
        {
            instance.Apply(EditorUserBuildSettings.activeBuildTarget);
        }
    }

#if UNITY_2017_1_OR_NEWER
    sealed class UpdateStrippingOnBuildTargetChange : IActiveBuildTargetChanged
    {
        public int callbackOrder { get { return 0; } }
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            PostProcessResourceStripper.Update(newTarget);
        }
    }

    sealed class UpdateStrippingBeforeBuild : IPreprocessBuild
    {
        public int callbackOrder { get { return 0; } }

    #if UNITY_2018_1_OR_NEWER
        public void OnPreprocessBuild(Build.Reporting.BuildReport report)
        {
            PostProcessResourceStripper.Update(report.summary.platform);
        }

    #else
        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            PostProcessResourceStripper.Update(target);
        }

    #endif
    }
#endif

    public class SetupStrippingConfig : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // check if the config asset is missing, but only after other .asset files have been imported
            if (Array.FindIndex(importedAssets, asset => asset != null && asset.Contains("PostProcessResources.asset")) > -1)
                PostProcessResourceStripper.EnsurePostProcessStrippingConfigAssetExists();
        }
    }
}
