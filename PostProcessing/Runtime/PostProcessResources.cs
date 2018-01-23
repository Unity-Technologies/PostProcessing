using System;

#if UNITY_EDITOR && UNITY_2017_1_OR_NEWER
using UnityEditor;
using UnityEditor.Build;
#endif

namespace UnityEngine.Rendering.PostProcessing
{
    // This asset is used to store references to shaders and other resources we might need at
    // runtime without having to use a `Resources` folder. This allows for better memory management,
    // better dependency tracking and better interoperability with asset bundles.

    public sealed class PostProcessResources : ScriptableObject
    {
        [Serializable]
        public sealed class Shaders
        {
            public Shader autoExposure;
            public Shader bloom;
            public Shader copy;
            public Shader copyStd;
            public Shader discardAlpha;
            public Shader depthOfField;
            public Shader finalPass;
            public Shader grainBaker;
            public Shader motionBlur;
            public Shader temporalAntialiasing;
            public Shader subpixelMorphologicalAntialiasing;
            public Shader texture2dLerp;
            public Shader uber;
            public Shader lut2DBaker;
            public Shader lightMeter;
            public Shader gammaHistogram;
            public Shader waveform;
            public Shader vectorscope;
            public Shader debugOverlays;
            public Shader deferredFog;
            public Shader scalableAO;
            public Shader multiScaleAO;
            public Shader screenSpaceReflections;

            public Shaders Clone() 
            {
                return (Shaders) MemberwiseClone();
            }
        }

        [Serializable]
        public sealed class ComputeShaders
        {
            public ComputeShader exposureHistogram;
            public ComputeShader lut3DBaker;
            public ComputeShader texture3dLerp;
            public ComputeShader gammaHistogram;
            public ComputeShader waveform;
            public ComputeShader vectorscope;
            public ComputeShader multiScaleAODownsample1;
            public ComputeShader multiScaleAODownsample2;
            public ComputeShader multiScaleAORender;
            public ComputeShader multiScaleAOUpsample;
            public ComputeShader gaussianDownsample;

            public ComputeShaders Clone() 
            {
                return (ComputeShaders) MemberwiseClone();
            }
        }

        [Serializable]
        public sealed class SMAALuts
        {
            public Texture2D area;
            public Texture2D search;
        }

        [Serializable]
        public sealed class BuildStripping
        {
            public bool enabled = true;
            public bool stripDebug = false;
            public bool stripCompute = false;
        }
                
        
        public Texture2D[] blueNoise64;
        public Texture2D[] blueNoise256;
        public SMAALuts smaaLuts;
        public Shaders shaders;
        public ComputeShaders computeShaders;
        public BuildStripping shaderStripping;
    }

#if UNITY_EDITOR && UNITY_2017_1_OR_NEWER
    class ResourcesBuildProcessor : IPostprocessBuild, IPreprocessBuild
    {
        public int callbackOrder { get { return 0; } }

        PostProcessResources resources = null;
        PostProcessResources.ComputeShaders unmodifiedComputeShaders = null;
        PostProcessResources.Shaders unmodifiedShaders = null;

        public ResourcesBuildProcessor()
        {
            string postProcessResourcesGUID = "d82512f9c8e5d4a4d938b575d47f88d4";
            var assetPath = AssetDatabase.GUIDToAssetPath(postProcessResourcesGUID);
            resources = (PostProcessResources) AssetDatabase.LoadAssetAtPath(assetPath, typeof(PostProcessResources));
        }

        private void StripMultiScaleAO()
        {
            resources.computeShaders.multiScaleAODownsample1 = null;
            resources.computeShaders.multiScaleAODownsample2 = null;
            resources.computeShaders.multiScaleAORender = null;
            resources.computeShaders.multiScaleAOUpsample = null;
            resources.shaders.multiScaleAO = null;
        }

        private void StripScreenSpaceReflections()
        {
            resources.shaders.screenSpaceReflections = null;
            resources.computeShaders.gaussianDownsample = null;
        }

        private void StripDebugShaders()
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

        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            if (resources != null && resources.shaderStripping.enabled)
            {
                unmodifiedComputeShaders = resources.computeShaders;
                unmodifiedShaders = resources.shaders;

                resources.computeShaders = unmodifiedComputeShaders.Clone();
                resources.shaders = unmodifiedShaders.Clone();

                // We don't support multi scale AO on mobile
                if (target == BuildTarget.Android || target == BuildTarget.iOS || target == BuildTarget.tvOS)
                {
                    StripMultiScaleAO();
                }

                if (!RuntimeUtilities.supportsDeferredShading)
                {
                    StripScreenSpaceReflections();
                    resources.shaders.deferredFog = null;
                    if (!RuntimeUtilities.supportsDepthNormals)
                        resources.shaders.scalableAO = null;
                }

                if (!RuntimeUtilities.supportsMotionVectors)
                {
                    resources.shaders.motionBlur = null;
                    resources.shaders.temporalAntialiasing = null;
                    resources.shaders.screenSpaceReflections = null;
                    resources.computeShaders.gaussianDownsample = null;
                }

                if (resources.shaderStripping.stripDebug)
                {
                    StripDebugShaders();
                }

                if (resources.shaderStripping.stripCompute)
                {
                    resources.computeShaders = new PostProcessResources.ComputeShaders();
                    resources.shaders.autoExposure = null;
                    StripScreenSpaceReflections();
                    StripMultiScaleAO();
                    StripDebugShaders();
                }
            }
        }

        public void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (resources != null)
            {
                if (unmodifiedComputeShaders != null)
                {
                    resources.computeShaders = unmodifiedComputeShaders;
                    unmodifiedComputeShaders = null;
                }

                if (unmodifiedShaders != null)
                {
                    resources.shaders = unmodifiedShaders;
                    unmodifiedShaders = null;
                }
            }
        }
    }
#endif
}
