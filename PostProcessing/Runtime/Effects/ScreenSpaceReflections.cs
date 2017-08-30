using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class ScreenSpaceReflections
    {
        public enum Preset
        {
            Lower,
            Low,
            Medium,
            High,
            Higher,
            Ultra,
            Overkill,
            Custom
        }

        [Tooltip("Enables screen-space reflections.")]
        public bool enabled;

        [Tooltip("Choose a quality preset, or use \"Custom\" to fine tune it. Don't use a preset higher than \"Medium\" if you care about performances on consoles.")]
        public Preset preset = Preset.Medium;
        
        [Range(0, 128), Tooltip("Maximum iteration count.")]
        public int maximumIterationCount;

        [Tooltip("Downsamples the SSR buffer to maximize performances at the cost of a blurrier result.")]
        public bool downsampling = true;

        [Range(1f, 64f), Tooltip("Ray thickness. Lower values are more expensive but allow the effect to detect smaller details.")]
        public float thickness = 8f;

        [Tooltip("Maximum distance to traverse after which it will stop drawing reflections.")]
        public float maximumMarchDistance = 100f;

        [Range(0f, 1f), Tooltip("Fades reflections close to the near planes.")]
        public float distanceFade = 0.5f;

        [Range(0f, 1f), Tooltip("Fades reflections close to the screen borders.")]
        public float attenuation = 0.25f;

        class QualityPreset
        {
            public int maximumIterationCount;
            public float thickness;
            public bool downsampling;
        }

        QualityPreset[] m_Presets =
        {
            new QualityPreset { maximumIterationCount = 10, thickness = 32, downsampling = true  }, // Lower
            new QualityPreset { maximumIterationCount = 16, thickness = 32, downsampling = true  }, // Low
            new QualityPreset { maximumIterationCount = 32, thickness = 16, downsampling = true  }, // Medium
            new QualityPreset { maximumIterationCount = 48, thickness =  8, downsampling = true  }, // High
            new QualityPreset { maximumIterationCount = 16, thickness = 32, downsampling = false }, // Higher
            new QualityPreset { maximumIterationCount = 48, thickness = 16, downsampling = false }, // Ultra
            new QualityPreset { maximumIterationCount = 64, thickness = 12, downsampling = false }, // Overkill
        };

        RenderTexture m_Test;
        RenderTexture m_Resolve;
        RenderTexture m_History;
        int[] m_MipIDs;

        enum Pass
        {
            Test,
            Resolve,
            Reproject,
            Composite
        }

        internal bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled
                && context.camera.actualRenderingPath == RenderingPath.DeferredShading
                && SystemInfo.supportsMotionVectors
                && SystemInfo.supportsComputeShaders
                && SystemInfo.copyTextureSupport > CopyTextureSupport.None;
        }

        internal DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        internal void CheckRT(ref RenderTexture rt, int width, int height, RenderTextureFormat format, FilterMode filterMode, bool useMipMap)
        {
            if (rt == null || !rt.IsCreated() || rt.width != width || rt.height != height)
            {
                if (rt != null)
                    rt.Release();

                rt = new RenderTexture(width, height, 0, format)
                {
                    filterMode = filterMode,
                    useMipMap = useMipMap,
                    autoGenerateMips = false,
                    hideFlags = HideFlags.HideAndDontSave
                };

                rt.Create();
            }
        }

        internal void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Screen-space Reflections");

            // Get quality settings
            if (preset != Preset.Custom)
            {
                int id = (int)preset;
                maximumIterationCount = m_Presets[id].maximumIterationCount;
                thickness = m_Presets[id].thickness;
                downsampling = m_Presets[id].downsampling;
            }

            // Square POT target
            int size = Mathf.ClosestPowerOfTwo(Mathf.Min(context.width, context.height));

            if (downsampling)
                size >>= 1;

            // The gaussian pyramid compute works in blocks of 8x8 so make sure the last lod has a
            // minimum size of 8x8
            const int kMaxLods = 12;
            int lodCount = Mathf.FloorToInt(Mathf.Log(size, 2f) - 3f);
            lodCount = Mathf.Min(lodCount, kMaxLods);

            CheckRT(ref m_Test, size, size, context.sourceFormat, FilterMode.Point, false);
            CheckRT(ref m_Resolve, size, size, context.sourceFormat, FilterMode.Trilinear, true);
            CheckRT(ref m_History, size, size, context.sourceFormat, FilterMode.Bilinear, false);

            var noiseTex = context.resources.blueNoise256[0];
            var sheet = context.propertySheets.Get(context.resources.shaders.screenSpaceReflections);
            sheet.properties.SetTexture(ShaderIDs.Noise, noiseTex);

            var screenSpaceProjectionMatrix = new Matrix4x4();
            screenSpaceProjectionMatrix.SetRow(0, new Vector4(size * 0.5f, 0f, 0f, size * 0.5f));
            screenSpaceProjectionMatrix.SetRow(1, new Vector4(0f, size * 0.5f, 0f, size * 0.5f));
            screenSpaceProjectionMatrix.SetRow(2, new Vector4(0f, 0f, 1f, 0f));
            screenSpaceProjectionMatrix.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

            var projectionMatrix = GL.GetGPUProjectionMatrix(context.camera.projectionMatrix, false);
            screenSpaceProjectionMatrix *= projectionMatrix;

            sheet.properties.SetMatrix(ShaderIDs.ViewMatrix, context.camera.worldToCameraMatrix);
            sheet.properties.SetMatrix(ShaderIDs.InverseViewMatrix, context.camera.worldToCameraMatrix.inverse);
            sheet.properties.SetMatrix(ShaderIDs.InverseProjectionMatrix, projectionMatrix.inverse);
            sheet.properties.SetMatrix(ShaderIDs.ScreenSpaceProjectionMatrix, screenSpaceProjectionMatrix);
            sheet.properties.SetVector(ShaderIDs.Params, new Vector4(attenuation, distanceFade, maximumMarchDistance, lodCount));
            sheet.properties.SetVector(ShaderIDs.Params2, new Vector4((float)context.width / (float)context.height, (float)size / (float)noiseTex.width, thickness, maximumIterationCount));

            cmd.GetTemporaryRT(ShaderIDs.SSRResolveTemp, size, size, 0, FilterMode.Bilinear, context.sourceFormat);
            cmd.BlitFullscreenTriangle(context.source, m_Test, sheet, (int)Pass.Test);
            cmd.BlitFullscreenTriangle(context.source, ShaderIDs.SSRResolveTemp, sheet, (int)Pass.Resolve);

            sheet.properties.SetTexture(ShaderIDs.Test, m_Test);
            sheet.properties.SetTexture(ShaderIDs.History, m_History);

            cmd.BlitFullscreenTriangle(ShaderIDs.SSRResolveTemp, m_Resolve, sheet, (int)Pass.Reproject);
            cmd.ReleaseTemporaryRT(ShaderIDs.SSRResolveTemp);

            cmd.CopyTexture(m_Resolve, 0, 0, m_History, 0, 0);

            // Pre-cache mipmaps ids
            if (m_MipIDs == null || m_MipIDs.Length == 0)
            {
                m_MipIDs = new int[kMaxLods];

                for (int i = 0; i < kMaxLods; i++)
                    m_MipIDs[i] = Shader.PropertyToID("_SSRGaussianMip" + i);
            }

            var compute = context.resources.computeShaders.gaussianDownsample;
            int kernel = compute.FindKernel("KMain");

            var last = new RenderTargetIdentifier(m_Resolve);

            for (int i = 0; i < lodCount; i++)
            {
                size >>= 1;
                Assert.IsTrue(size > 0);

                cmd.GetTemporaryRT(m_MipIDs[i], size, size, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Default, 1, true);
                cmd.SetComputeTextureParam(compute, kernel, "_Source", last);
                cmd.SetComputeTextureParam(compute, kernel, "_Result", m_MipIDs[i]);
                cmd.SetComputeVectorParam(compute, "_Size", new Vector4(size, size, 1f / size, 1f / size));
                cmd.DispatchCompute(compute, kernel, size / 8, size / 8, 1);
                cmd.CopyTexture(m_MipIDs[i], 0, 0, m_Resolve, 0, i + 1);

                last = m_MipIDs[i];
            }

            for (int i = 0; i < lodCount; i++)
                cmd.ReleaseTemporaryRT(m_MipIDs[i]);
            
            sheet.properties.SetTexture(ShaderIDs.Resolve, m_Resolve);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Composite);
            cmd.EndSample("Screen-space Reflections");
        }

        internal void Release()
        {
            RuntimeUtilities.Destroy(m_Test);
            RuntimeUtilities.Destroy(m_Resolve);
            RuntimeUtilities.Destroy(m_History);
            m_Test = null;
            m_Resolve = null;
            m_History = null;
        }
    }
}
