using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public sealed class PostProcessResourceStripper : ScriptableObject
    {
        public const string DefaultStrippingConfigAssetPath = "Assets/PostProcessStrippingConfig.asset";

        PostProcessStrippingConfig stripping;
        PostProcessStrippingConfig defaultConfig;

        [SerializeField]
        PostProcessResources unstrippedResources;

        static PostProcessResourceStripper s_Instance;

        public static PostProcessResourceStripper instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = CreateInstance<PostProcessResourceStripper>();
                    s_Instance.defaultConfig = CreateInstance<PostProcessStrippingConfig>();
                }

                return s_Instance;
            }
        }

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        static void StripMultiScaleAO(PostProcessResources resources)
        {
            resources.computeShaders.multiScaleAODownsample1 = null;
            resources.computeShaders.multiScaleAODownsample2 = null;
            resources.computeShaders.multiScaleAORender = null;
            resources.computeShaders.multiScaleAOUpsample = null;
            resources.shaders.multiScaleAO = null;
        }

        static void StripScreenSpaceReflections(PostProcessResources resources)
        {
            resources.shaders.screenSpaceReflections = null;
            resources.computeShaders.gaussianDownsample = null;
        }

        static void StripDebugShaders(PostProcessResources resources)
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

        static string FindPostProcessStrippingConfigGUID()
        {
            var guids = AssetDatabase.FindAssets("t:PostProcessStrippingConfig", null);
            if (guids.Length > 0)
                return guids[0];
            else
                return null;
        }

        static public void EnsurePostProcessStrippingConfigAssetExists()
        {
            var guid = FindPostProcessStrippingConfigGUID();
            if (guid != null)
                return;

            AssetDatabase.CreateAsset(CreateInstance<PostProcessStrippingConfig>(), DefaultStrippingConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void LazyLoadStrippingConfig()
        {
            if (stripping != null)
                return;

            var guid = FindPostProcessStrippingConfigGUID();
            if (guid != null)
            {
                stripping = (PostProcessStrippingConfig) AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(PostProcessStrippingConfig));
            }

            if (stripping == null)
                stripping = defaultConfig;
        }

        void SetConfig(PostProcessStrippingConfig config)
        {
            if (config == stripping)
                return;

            if (defaultConfig == null)
                return;

            if (config == defaultConfig)
                return;

            if (config == null)
            {
                stripping = defaultConfig;
                return;
            }

            stripping = config;
        }

        void Apply(BuildTarget target, PostProcessResources resources)
        {
            if (defaultConfig == null)
                return;

            LazyLoadStrippingConfig();
            if (stripping == null)
                return;

            if (unstrippedResources == null)
                return;

            if (resources == null)
                return;

            resources.computeShaders = unstrippedResources.computeShaders.Clone();
            resources.shaders = unstrippedResources.shaders.Clone();

            // We don't support multi scale AO on mobile
            if (stripping.stripUnsupportedShaders &&
                (target == BuildTarget.Android || target == BuildTarget.iOS || target == BuildTarget.tvOS))
            {
                StripMultiScaleAO(resources);
            }

            if (stripping.stripDebugShaders)
            {
                StripDebugShaders(resources);
            }

            if (stripping.stripComputeShaders)
            {
                resources.computeShaders = new PostProcessResources.ComputeShaders();
                resources.shaders.autoExposure = null;
                StripScreenSpaceReflections(resources);
                StripMultiScaleAO(resources);
                StripDebugShaders(resources);
            }

            if (stripping.stripUnsupportedShaders && !RuntimeUtilities.supportsDeferredShading)
            {
                StripScreenSpaceReflections(resources);
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

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StripAll();
        }

        public static void Strip(PostProcessResources resources)
        {
            instance.Apply(EditorUserBuildSettings.activeBuildTarget, resources);
        }

        public static void StripAll(BuildTarget target)
        {
            var allResources = PostProcessResourcesFactory.AllResources();
            if (allResources == null)
                return;

            foreach (var resources in allResources)
                instance.Apply(EditorUserBuildSettings.activeBuildTarget, resources);
        }

        public static void StripAll()
        {
            StripAll(EditorUserBuildSettings.activeBuildTarget);
        }

        public static void StripAll(PostProcessStrippingConfig config)
        {
            instance.SetConfig(config);
            StripAll(EditorUserBuildSettings.activeBuildTarget);
        }
    }

#if UNITY_2017_1_OR_NEWER
    sealed class UpdateStrippingOnBuildTargetChange : IActiveBuildTargetChanged
    {
        public int callbackOrder { get { return 0; } }
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            PostProcessResourceStripper.StripAll(newTarget);
        }
    }

    sealed class UpdateStrippingBeforeBuild : IPreprocessBuild
    {
        public int callbackOrder { get { return 0; } }

    #if UNITY_2018_1_OR_NEWER
        public void OnPreprocessBuild(Build.Reporting.BuildReport report)
        {
            PostProcessResourceStripper.StripAll(report.summary.platform);
        }
    #else
        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            PostProcessResourceStripper.StripAll(target);
        }
    #endif
    }
#endif

    [InitializeOnLoad]
    public class SetupStripping
    {
        static SetupStripping()
        {
            PostProcessResourceStripper.EnsurePostProcessStrippingConfigAssetExists();
            PostProcessResourcesFactory.Init(PostProcessResourceStripper.Strip);
        }
    }
}
