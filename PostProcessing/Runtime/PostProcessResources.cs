using System;

namespace UnityEngine.Experimental.PostProcessing
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
        }

        [Serializable]
        public sealed class ComputeShaders
        {
            public ComputeShader exposureHistogram;
            public ComputeShader lut3DBaker;
            public ComputeShader texture3dLerp;
        }

        [Serializable]
        public sealed class SMAALuts
        {
            public Texture2D area;
            public Texture2D search;
        }
        
        public Texture2D[] blueNoise;
        public SMAALuts smaaLuts;
        public Shaders shaders;
        public ComputeShaders computeShaders;
    }
}
