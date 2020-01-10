using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This asset is used to store references to shaders and other resources we might need at
    /// runtime without having to use a `Resources` folder. This allows for better memory management,
    /// better dependency tracking and better interoperability with asset bundles.
    /// </summary>
    public sealed class PostProcessResources : ScriptableObject
    {
        /// <summary>
        /// All the shaders used by post-processing.
        /// </summary>
        [Serializable]
        public sealed class Shaders
        {
            /// <summary>
            /// The shader used for the bloom effect.
            /// </summary>
            public Shader bloom;

            /// <summary>
            /// The shader used for internal blit copies.
            /// </summary>
            public Shader copy;

            /// <summary>
            /// The shader used for built-in blit copies in the built-in pipeline.
            /// </summary>
            public Shader copyStd;

            /// <summary>
            /// The shader used for built-in blit copies in the built-in pipeline when using a
            /// texture array (for stereo rendering).
            /// </summary>
            public Shader copyStdFromTexArray;

            /// <summary>
            /// The shader used for built-in blit copies in the built-in pipeline when using a
            /// double-wide texture (for stereo rendering).
            /// </summary>
            public Shader copyStdFromDoubleWide;

            /// <summary>
            /// The shader used to kill the alpha.
            /// </summary>
            public Shader discardAlpha;

            /// <summary>
            /// The shader used for the depth of field effect.
            /// </summary>
            public Shader depthOfField;

            /// <summary>
            /// The shader used for the final pass.
            /// </summary>
            public Shader finalPass;

            /// <summary>
            /// The shader used to generate the grain texture.
            /// </summary>
            public Shader grainBaker;

            /// <summary>
            /// The shader used for the motion blur effect.
            /// </summary>
            public Shader motionBlur;

            /// <summary>
            /// The shader used for the temporal anti-aliasing effect.
            /// </summary>
            public Shader temporalAntialiasing;

            /// <summary>
            /// The shader used for the sub-pixel morphological anti-aliasing effect.
            /// </summary>
            public Shader subpixelMorphologicalAntialiasing;

            /// <summary>
            /// The shader use by the volume manager to interpolate between two 2D textures.
            /// </summary>
            public Shader texture2dLerp;

            /// <summary>
            /// The uber shader that combine several effects into one.
            /// </summary>
            public Shader uber;

            /// <summary>
            /// The shader used to bake the 2D lookup table for color grading.
            /// </summary>
            public Shader lut2DBaker;

            /// <summary>
            /// The shader used to draw the light meter monitor.
            /// </summary>
            public Shader lightMeter;

            /// <summary>
            /// The shader used to draw the histogram monitor.
            /// </summary>
            public Shader gammaHistogram;

            /// <summary>
            /// The shader used to draw the waveform monitor.
            /// </summary>
            public Shader waveform;

            /// <summary>
            /// The shader used to draw the vectorscope monitor.
            /// </summary>
            public Shader vectorscope;

            /// <summary>
            /// The shader used to draw debug overlays.
            /// </summary>
            public Shader debugOverlays;

            /// <summary>
            /// The shader used for the deferred fog effect.
            /// </summary>
            public Shader deferredFog;

            /// <summary>
            /// The shader used for the scalable ambient occlusion effect.
            /// </summary>
            public Shader scalableAO;

            /// <summary>
            /// The shader used for the multi-scale ambient occlusion effect.
            /// </summary>
            public Shader multiScaleAO;

            /// <summary>
            /// The shader used for the screen-space reflection effect.
            /// </summary>
            public Shader screenSpaceReflections;

            /// <summary>
            /// Returns a copy of this class and its content.
            /// </summary>
            /// <returns>A copy of this class and its content.</returns>
            public Shaders Clone()
            {
                return (Shaders)MemberwiseClone();
            }
        }

        /// <summary>
        /// All the compute shaders used by post-processing.
        /// </summary>
        [Serializable]
        public sealed class ComputeShaders
        {
            /// <summary>
            /// The compute shader used for the auto-exposure effect.
            /// </summary>
            public ComputeShader autoExposure;

            /// <summary>
            /// The compute shader used to compute an histogram of the current frame.
            /// </summary>
            public ComputeShader exposureHistogram;

            /// <summary>
            /// The compute shader used to bake the 3D lookup table for color grading.
            /// </summary>
            public ComputeShader lut3DBaker;

            /// <summary>
            /// The compute shader used by the volume manager to interpolate between two 3D textures.
            /// </summary>
            public ComputeShader texture3dLerp;

            /// <summary>
            /// The compute shader used to compute the histogram monitor.
            /// </summary>
            public ComputeShader gammaHistogram;

            /// <summary>
            /// The compute shader used to compute the waveform monitor.
            /// </summary>
            public ComputeShader waveform;

            /// <summary>
            /// The compute shader used to compute the vectorscope monitor.
            /// </summary>
            public ComputeShader vectorscope;

            /// <summary>
            /// The compute shader used for the first downsampling pass of MSVO.
            /// </summary>
            public ComputeShader multiScaleAODownsample1;

            /// <summary>
            /// The compute shader used for the second downsampling pass of MSVO.
            /// </summary>
            public ComputeShader multiScaleAODownsample2;

            /// <summary>
            /// The compute shader used for the render pass of MSVO.
            /// </summary>
            public ComputeShader multiScaleAORender;

            /// <summary>
            /// The compute shader used for the upsampling pass of MSVO.
            /// </summary>
            public ComputeShader multiScaleAOUpsample;

            /// <summary>
            /// The compute shader used to a fast gaussian downsample.
            /// </summary>
            public ComputeShader gaussianDownsample;

            /// <summary>
            /// Returns a copy of this class and its content.
            /// </summary>
            /// <returns>A copy of this class and its content.</returns>
            public ComputeShaders Clone()
            {
                return (ComputeShaders)MemberwiseClone();
            }
        }

        /// <summary>
        /// A set of textures needed by the sub-pixel morphological anti-aliasing effect.
        /// </summary>
        [Serializable]
        public sealed class SMAALuts
        {
            /// <summary>
            /// The area lookup table.
            /// </summary>
            public Texture2D area;

            /// <summary>
            /// The search lookup table.
            /// </summary>
            public Texture2D search;
        }

        /// <summary>
        /// A set of 64x64, single-channel blue noise textures.
        /// </summary>
        public Texture2D[] blueNoise64;

        /// <summary>
        /// A set of 256x256, single-channel blue noise textures.
        /// </summary>
        public Texture2D[] blueNoise256;

        /// <summary>
        /// Lookup tables used by the sub-pixel morphological anti-aliasing effect.
        /// </summary>
        public SMAALuts smaaLuts;

        /// <summary>
        /// All the shaders used by post-processing.
        /// </summary>
        public Shaders shaders;

        /// <summary>
        /// All the compute shaders used by post-processing.
        /// </summary>
        public ComputeShaders computeShaders;

#if UNITY_EDITOR
        /// <summary>
        /// A delegate used to track resource changes.
        /// </summary>
        public delegate void ChangeHandler();

        /// <summary>
        /// Set this callback to be notified of resource changes.
        /// </summary>
        public ChangeHandler changeHandler;

        void OnValidate()
        {
            if (changeHandler != null)
                changeHandler();
        }
#endif
    }
}
