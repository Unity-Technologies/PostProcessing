using System;

namespace UnityEngine.Experimental.PostProcessing
{
    // This asset is used to store references to shaders and other resources we might need at
    // runtime without having to use a `Resources` folder. This allows for better memory management,
    // better dependency tracking and better interoperability with asset bundles.
    public sealed class PostProcessResources : ScriptableObject
    {
        // TODO: Remove this ugly hack
        internal static PostProcessResources instance;

        [Serializable]
        public sealed class Shaders
        {
            public Shader autoExposure;
            public Shader bloom;
            public Shader copy;
            public Shader depthOfField;
            public Shader finalPass;
            public Shader grainBaker;
            public Shader motionBlur;
            public Shader temporalAntialiasing;
            public Shader texture2dLerp;
            public Shader uber;
            public Shader lut2DBaker;
        }

        [Serializable]
        public sealed class ComputeShaders
        {
            public ComputeShader exposureHistogram;
            public ComputeShader lut3DBaker;
        }

        public Texture2D[] blueNoise;
        public Shaders shaders;
        public ComputeShaders computeShaders;

        void OnEnable()
        {
            instance = this;
        }

        void OnDisable()
        {
            instance = null;
        }

        /*
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Create Empty PostProcessResources Asset")]
        static void CreateAsset()
        {
            var asset = CreateInstance<PostProcessResources>();
            UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/PostProcessResources.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
        */
    }
}
