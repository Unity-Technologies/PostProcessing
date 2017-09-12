using System;

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
        }

        [Serializable]
        public sealed class SMAALuts
        {
            public Texture2D area;
            public Texture2D search;
        }
        
        public Texture2D[] blueNoise64;
        public Texture2D[] blueNoise256;
        public SMAALuts smaaLuts;
        public Shaders shaders;
        public ComputeShaders computeShaders;
    }
}
