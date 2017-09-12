using System;

namespace UnityEngine.Rendering.PostProcessing
{
    // Multi-scale volumetric obscurance
    // TODO: Fix VR support
    [Serializable]
    public sealed class MultiScaleVO : IAmbientOcclusionMethod
    {
        internal enum MipLevel { Original, L1, L2, L3, L4, L5, L6 }

        internal enum TextureType
        {
            Fixed, Half, Float,                        // 2D render texture
            FixedUAV, HalfUAV, FloatUAV,               // Read/write enabled
            FixedTiledUAV, HalfTiledUAV, FloatTiledUAV // Texture array
        }

        enum Pass
        {
            DepthCopy,
            CompositionDeferred,
            CompositionForward,
            DebugOverlay
        }

        AmbientOcclusion m_Settings;
        PropertySheet m_PropertySheet;
        RTHandle m_DepthCopy;
        RTHandle m_LinearDepth;
        RTHandle m_LowDepth1;
        RTHandle m_LowDepth2;
        RTHandle m_LowDepth3;
        RTHandle m_LowDepth4;
        RTHandle m_TiledDepth1;
        RTHandle m_TiledDepth2;
        RTHandle m_TiledDepth3;
        RTHandle m_TiledDepth4;
        RTHandle m_Occlusion1;
        RTHandle m_Occlusion2;
        RTHandle m_Occlusion3;
        RTHandle m_Occlusion4;
        RTHandle m_Combined1;
        RTHandle m_Combined2;
        RTHandle m_Combined3;
        RTHandle m_Result;

        // The arrays below are reused between frames to reduce GC allocation.
        readonly float[] m_SampleThickness =
        {
            Mathf.Sqrt(1f - 0.2f * 0.2f),
            Mathf.Sqrt(1f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.2f * 0.2f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.6f * 0.6f - 0.6f * 0.6f)
        };

        readonly float[] m_InvThicknessTable = new float[12];
        readonly float[] m_SampleWeightTable = new float[12];

        readonly RenderTargetIdentifier[] m_MRT =
        {
            BuiltinRenderTextureType.GBuffer0,    // Albedo, Occ
            BuiltinRenderTextureType.CameraTarget // Ambient
        };

        internal class RTHandle
        {
            // Base dimensions (shared between handles)
            static int s_BaseWidth;
            static int s_BaseHeight;

            public static void SetBaseDimensions(int w, int h)
            {
                s_BaseWidth = w;
                s_BaseHeight = h;
            }

            public int nameID { get; private set; }
            public int width { get; private set; }
            public int height { get; private set; }
            public int depth { get { return isTiled ? 16 : 1; } }
            public bool isTiled { get { return (int)m_Type > 5; } }
            public bool hasUAV { get { return (int)m_Type > 2; } }

            RenderTexture m_RT;
            TextureType m_Type;
            MipLevel m_Level;

            public RenderTargetIdentifier id
            {
                get
                {
                    return m_RT != null
                        ? new RenderTargetIdentifier(m_RT)
                        : new RenderTargetIdentifier(nameID);
                }
            }

            public Vector2 inverseDimensions
            {
                get { return new Vector2(1f / width, 1f / height); }
            }

            public RTHandle(string name, TextureType type, MipLevel level)
            {
                nameID = Shader.PropertyToID(name);
                m_Type = type;
                m_Level = level;
            }

            public void AllocateNow()
            {
                CalculateDimensions();
                bool reset = false;

                if (m_RT == null || !m_RT.IsCreated())
                {
                    // Initial allocation
                    m_RT = new RenderTexture(width, height, 0, renderTextureFormat, RenderTextureReadWrite.Linear) { hideFlags = HideFlags.DontSave };
                    reset = true;
                }
                else if (m_RT.width != width || m_RT.height != height)
                {
                    // Release and reallocate
                    m_RT.Release();
                    m_RT.width = width;
                    m_RT.height = height;
                    m_RT.format = renderTextureFormat;
                    reset = true;
                }

                if (!reset)
                    return;

                m_RT.filterMode = FilterMode.Point;
                m_RT.enableRandomWrite = hasUAV;

                // Should it be tiled?
                if (isTiled)
                {
                    m_RT.dimension = TextureDimension.Tex2DArray;
                    m_RT.volumeDepth = depth;
                }

                m_RT.Create();
            }

            public void PushAllocationCommand(CommandBuffer cmd)
            {
                CalculateDimensions();

#if UNITY_2017_1_OR_NEWER
                if (isTiled)
                {
                    cmd.GetTemporaryRTArray(
                        nameID, width, height, depth, 0,
                        FilterMode.Point, renderTextureFormat,
                        RenderTextureReadWrite.Linear, 1, hasUAV
                    );
                }
                else
#endif
                {
                    cmd.GetTemporaryRT(
                        nameID, width, height, 0,
                        FilterMode.Point, renderTextureFormat,
                        RenderTextureReadWrite.Linear, 1, hasUAV
                    );
                }
            }

            public void Destroy()
            {
                RuntimeUtilities.Destroy(m_RT);
                m_RT = null;
            }

            RenderTextureFormat renderTextureFormat
            {
                get
                {
                    switch ((int)m_Type % 3)
                    {
                        case 0: return RenderTextureFormat.R8;
                        case 1: return RenderTextureFormat.RHalf;
                        default: return RenderTextureFormat.RFloat;
                    }
                }
            }

            // Calculate width/height of the texture from the base dimensions.
            void CalculateDimensions()
            {
                int div = 1 << (int)m_Level;
                width  = (s_BaseWidth  + (div - 1)) / div;
                height = (s_BaseHeight + (div - 1)) / div;
            }
        }

        public MultiScaleVO(AmbientOcclusion settings)
        {
            m_Settings = settings;
        }

        public DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        void DoLazyInitialization(PostProcessRenderContext context)
        {
            var shader = context.resources.shaders.multiScaleAO;
            m_PropertySheet = context.propertySheets.Get(shader);
            m_PropertySheet.ClearKeywords();

            // Render texture handles
            if (m_Result == null)
            {
                m_DepthCopy = new RTHandle("DepthCopy", TextureType.Float, MipLevel.Original);
                m_LinearDepth = new RTHandle("LinearDepth", TextureType.HalfUAV, MipLevel.Original);

                m_LowDepth1 = new RTHandle("LowDepth1", TextureType.FloatUAV, MipLevel.L1);
                m_LowDepth2 = new RTHandle("LowDepth2", TextureType.FloatUAV, MipLevel.L2);
                m_LowDepth3 = new RTHandle("LowDepth3", TextureType.FloatUAV, MipLevel.L3);
                m_LowDepth4 = new RTHandle("LowDepth4", TextureType.FloatUAV, MipLevel.L4);

                m_TiledDepth1 = new RTHandle("TiledDepth1", TextureType.HalfTiledUAV, MipLevel.L3);
                m_TiledDepth2 = new RTHandle("TiledDepth2", TextureType.HalfTiledUAV, MipLevel.L4);
                m_TiledDepth3 = new RTHandle("TiledDepth3", TextureType.HalfTiledUAV, MipLevel.L5);
                m_TiledDepth4 = new RTHandle("TiledDepth4", TextureType.HalfTiledUAV, MipLevel.L6);

                m_Occlusion1 = new RTHandle("Occlusion1", TextureType.FixedUAV, MipLevel.L1);
                m_Occlusion2 = new RTHandle("Occlusion2", TextureType.FixedUAV, MipLevel.L2);
                m_Occlusion3 = new RTHandle("Occlusion3", TextureType.FixedUAV, MipLevel.L3);
                m_Occlusion4 = new RTHandle("Occlusion4", TextureType.FixedUAV, MipLevel.L4);

                m_Combined1 = new RTHandle("Combined1", TextureType.FixedUAV, MipLevel.L1);
                m_Combined2 = new RTHandle("Combined2", TextureType.FixedUAV, MipLevel.L2);
                m_Combined3 = new RTHandle("Combined3", TextureType.FixedUAV, MipLevel.L3);

                m_Result = new RTHandle("AmbientOcclusion", TextureType.FixedUAV, MipLevel.Original);
            }
        }

        void RebuildCommandBuffers(PostProcessRenderContext context)
        {
            var cmd = context.command;

            // Update the base dimensions and reallocate static RTs.
            RTHandle.SetBaseDimensions(
                context.width * (RuntimeUtilities.isSinglePassStereoEnabled ? 2 : 1),
                context.height
            );

            m_PropertySheet.properties.SetVector(ShaderIDs.AOColor, Color.white - m_Settings.color.value);

#if !UNITY_2017_1_OR_NEWER
             m_TiledDepth1.AllocateNow();
             m_TiledDepth2.AllocateNow();
             m_TiledDepth3.AllocateNow();
             m_TiledDepth4.AllocateNow();
#endif

            m_Result.AllocateNow();

            PushDownsampleCommands(context, cmd);

            m_Occlusion1.PushAllocationCommand(cmd);
            m_Occlusion2.PushAllocationCommand(cmd);
            m_Occlusion3.PushAllocationCommand(cmd);
            m_Occlusion4.PushAllocationCommand(cmd);

            float tanHalfFovH = CalculateTanHalfFovHeight(context);
            PushRenderCommands(context, cmd, m_TiledDepth1, m_Occlusion1, tanHalfFovH);
            PushRenderCommands(context, cmd, m_TiledDepth2, m_Occlusion2, tanHalfFovH);
            PushRenderCommands(context, cmd, m_TiledDepth3, m_Occlusion3, tanHalfFovH);
            PushRenderCommands(context, cmd, m_TiledDepth4, m_Occlusion4, tanHalfFovH);

            m_Combined1.PushAllocationCommand(cmd);
            m_Combined2.PushAllocationCommand(cmd);
            m_Combined3.PushAllocationCommand(cmd);

            PushUpsampleCommands(context, cmd, m_LowDepth4, m_Occlusion4, m_LowDepth3, m_Occlusion3, m_Combined3);
            PushUpsampleCommands(context, cmd, m_LowDepth3, m_Combined3, m_LowDepth2, m_Occlusion2, m_Combined2);
            PushUpsampleCommands(context, cmd, m_LowDepth2, m_Combined2, m_LowDepth1, m_Occlusion1, m_Combined1);
            PushUpsampleCommands(context, cmd, m_LowDepth1, m_Combined1, m_LinearDepth, null, m_Result);

            if (context.IsDebugOverlayEnabled(DebugOverlay.AmbientOcclusion))
                context.PushDebugOverlay(cmd, m_Result.id, m_PropertySheet, (int)Pass.DebugOverlay);
        }

        // Calculate values in _ZBuferParams (built-in shader variable)
        // We can't use _ZBufferParams in compute shaders, so this function is
        // used to give the values in it to compute shaders.
        Vector4 CalculateZBufferParams(Camera camera)
        {
            float fpn = camera.farClipPlane / camera.nearClipPlane;

            if (SystemInfo.usesReversedZBuffer)
                return new Vector4(fpn - 1f, 1f, 0f, 0f);

            return new Vector4(1f - fpn, fpn, 0f, 0f);
        }

        float CalculateTanHalfFovHeight(PostProcessRenderContext context)
        {
            return 1f / context.camera.projectionMatrix[0, 0];
        }

        void PushDownsampleCommands(PostProcessRenderContext context, CommandBuffer cmd)
        {
            // Make a copy of the depth texture, or reuse the resolved depth
            // buffer (it's only available in some specific situations).
            var useDepthCopy = !RuntimeUtilities.IsResolvedDepthAvailable(context.camera);
            if (useDepthCopy)
            {
                m_DepthCopy.PushAllocationCommand(cmd);
                cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_DepthCopy.id, m_PropertySheet, (int)Pass.DepthCopy);
            }

            // Temporary buffer allocations.
            m_LinearDepth.PushAllocationCommand(cmd);
            m_LowDepth1.PushAllocationCommand(cmd);
            m_LowDepth2.PushAllocationCommand(cmd);
            m_LowDepth3.PushAllocationCommand(cmd);
            m_LowDepth4.PushAllocationCommand(cmd);

#if UNITY_2017_1_OR_NEWER
            m_TiledDepth1.PushAllocationCommand(cmd);
            m_TiledDepth2.PushAllocationCommand(cmd);
            m_TiledDepth3.PushAllocationCommand(cmd);
            m_TiledDepth4.PushAllocationCommand(cmd);
#endif

            // 1st downsampling pass.
            var cs = context.resources.computeShaders.multiScaleAODownsample1;
            int kernel = cs.FindKernel("main");

            cmd.SetComputeTextureParam(cs, kernel, "LinearZ", m_LinearDepth.id);
            cmd.SetComputeTextureParam(cs, kernel, "DS2x", m_LowDepth1.id);
            cmd.SetComputeTextureParam(cs, kernel, "DS4x", m_LowDepth2.id);
            cmd.SetComputeTextureParam(cs, kernel, "DS2xAtlas", m_TiledDepth1.id);
            cmd.SetComputeTextureParam(cs, kernel, "DS4xAtlas", m_TiledDepth2.id);
            cmd.SetComputeVectorParam(cs, "ZBufferParams", CalculateZBufferParams(context.camera));

            cmd.SetComputeTextureParam(cs, kernel, "Depth", useDepthCopy
                ? m_DepthCopy.id
                : BuiltinRenderTextureType.ResolvedDepth
            );

            cmd.DispatchCompute(cs, kernel, m_TiledDepth2.width, m_TiledDepth2.height, 1);

            if (useDepthCopy) cmd.ReleaseTemporaryRT(m_DepthCopy.nameID);

            // 2nd downsampling pass.
            cs = context.resources.computeShaders.multiScaleAODownsample2;
            kernel = cs.FindKernel("main");

            cmd.SetComputeTextureParam(cs, kernel, "DS4x", m_LowDepth2.id);
            cmd.SetComputeTextureParam(cs, kernel, "DS8x", m_LowDepth3.id);
            cmd.SetComputeTextureParam(cs, kernel, "DS16x", m_LowDepth4.id);
            cmd.SetComputeTextureParam(cs, kernel, "DS8xAtlas", m_TiledDepth3.id);
            cmd.SetComputeTextureParam(cs, kernel, "DS16xAtlas", m_TiledDepth4.id);

            cmd.DispatchCompute(cs, kernel, m_TiledDepth4.width, m_TiledDepth4.height, 1);
        }

        void PushRenderCommands(PostProcessRenderContext context, CommandBuffer cmd, RTHandle source, RTHandle dest, float tanHalfFovH)
        {
            // Here we compute multipliers that convert the center depth value into (the reciprocal
            // of) sphere thicknesses at each sample location. This assumes a maximum sample radius
            // of 5 units, but since a sphere has no thickness at its extent, we don't need to
            // sample that far out. Only samples whole integer offsets with distance less than 25
            // are used. This means that there is no sample at (3, 4) because its distance is
            // exactly 25 (and has a thickness of 0.)

            // The shaders are set up to sample a circular region within a 5-pixel radius.
            const float kScreenspaceDiameter = 10;

            // SphereDiameter = CenterDepth * ThicknessMultiplier. This will compute the thickness
            // of a sphere centered at a specific depth. The ellipsoid scale can stretch a sphere
            // into an ellipsoid, which changes the characteristics of the AO.
            // TanHalfFovH: Radius of sphere in depth units if its center lies at Z = 1
            // ScreenspaceDiameter: Diameter of sample sphere in pixel units
            // ScreenspaceDiameter / BufferWidth: Ratio of the screen width that the sphere actually covers
            // Note about the "2.0f * ": Diameter = 2 * Radius
            float thicknessMultiplier = 2f * tanHalfFovH * kScreenspaceDiameter / source.width;
            if (!source.isTiled) thicknessMultiplier *= 2f;
            if (RuntimeUtilities.isSinglePassStereoEnabled) thicknessMultiplier *= 2f;

            // This will transform a depth value from [0, thickness] to [0, 1].
            float inverseRangeFactor = 1f / thicknessMultiplier;

            // The thicknesses are smaller for all off-center samples of the sphere. Compute
            // thicknesses relative to the center sample.
            for (int i = 0; i < 12; i++)
                m_InvThicknessTable[i] = inverseRangeFactor / m_SampleThickness[i];

            // These are the weights that are multiplied against the samples because not all samples
            // are equally important. The farther the sample is from the center location, the less
            // they matter. We use the thickness of the sphere to determine the weight.  The scalars
            // in front are the number of samples with this weight because we sum the samples
            // together before multiplying by the weight, so as an aggregate all of those samples
            // matter more. After generating this table, the weights are normalized.
            m_SampleWeightTable[ 0] = 4 * m_SampleThickness[ 0];    // Axial
            m_SampleWeightTable[ 1] = 4 * m_SampleThickness[ 1];    // Axial
            m_SampleWeightTable[ 2] = 4 * m_SampleThickness[ 2];    // Axial
            m_SampleWeightTable[ 3] = 4 * m_SampleThickness[ 3];    // Axial
            m_SampleWeightTable[ 4] = 4 * m_SampleThickness[ 4];    // Diagonal
            m_SampleWeightTable[ 5] = 8 * m_SampleThickness[ 5];    // L-shaped
            m_SampleWeightTable[ 6] = 8 * m_SampleThickness[ 6];    // L-shaped
            m_SampleWeightTable[ 7] = 8 * m_SampleThickness[ 7];    // L-shaped
            m_SampleWeightTable[ 8] = 4 * m_SampleThickness[ 8];    // Diagonal
            m_SampleWeightTable[ 9] = 8 * m_SampleThickness[ 9];    // L-shaped
            m_SampleWeightTable[10] = 8 * m_SampleThickness[10];    // L-shaped
            m_SampleWeightTable[11] = 4 * m_SampleThickness[11];    // Diagonal

            // Zero out the unused samples.
            // FIXME: should we support SAMPLE_EXHAUSTIVELY mode?
            m_SampleWeightTable[0] = 0;
            m_SampleWeightTable[2] = 0;
            m_SampleWeightTable[5] = 0;
            m_SampleWeightTable[7] = 0;
            m_SampleWeightTable[9] = 0;

            // Normalize the weights by dividing by the sum of all weights
            var totalWeight = 0f;

            foreach (float w in m_SampleWeightTable)
                totalWeight += w;

            for (int i = 0; i < m_SampleWeightTable.Length; i++)
                m_SampleWeightTable[i] /= totalWeight;

            // Set the arguments for the render kernel.
            var cs = context.resources.computeShaders.multiScaleAORender;
            int kernel = cs.FindKernel("main_interleaved");

            cmd.SetComputeFloatParams(cs, "gInvThicknessTable", m_InvThicknessTable);
            cmd.SetComputeFloatParams(cs, "gSampleWeightTable", m_SampleWeightTable);
            cmd.SetComputeVectorParam(cs, "gInvSliceDimension", source.inverseDimensions);
            cmd.SetComputeVectorParam(cs, "AdditionalParams", new Vector2(-1f / m_Settings.thicknessModifier.value, m_Settings.intensity.value));
            cmd.SetComputeTextureParam(cs, kernel, "DepthTex", source.id);
            cmd.SetComputeTextureParam(cs, kernel, "Occlusion", dest.id);

            // Calculate the thread group count and add a dispatch command with them.
            uint xsize, ysize, zsize;
            cs.GetKernelThreadGroupSizes(kernel, out xsize, out ysize, out zsize);

            cmd.DispatchCompute(
                cs, kernel,
                (source.width  + (int)xsize - 1) / (int)xsize,
                (source.height + (int)ysize - 1) / (int)ysize,
                (source.depth  + (int)zsize - 1) / (int)zsize
            );
        }

        void PushUpsampleCommands(
            PostProcessRenderContext context,
            CommandBuffer cmd,
            RTHandle lowResDepth, RTHandle interleavedAO,
            RTHandle highResDepth, RTHandle highResAO,
            RTHandle dest
        )
        {
            var cs = context.resources.computeShaders.multiScaleAOUpsample;
            int kernel = cs.FindKernel((highResAO == null) ? "main" : "main_blendout");

            float stepSize = 1920f / lowResDepth.width;
            float bTolerance = 1f - Mathf.Pow(10f, m_Settings.blurTolerance.value) * stepSize;
            bTolerance *= bTolerance;
            float uTolerance = Mathf.Pow(10f, m_Settings.upsampleTolerance.value);
            float noiseFilterWeight = 1f / (Mathf.Pow(10f, m_Settings.noiseFilterTolerance.value) + uTolerance);

            cmd.SetComputeVectorParam(cs, "InvLowResolution", lowResDepth.inverseDimensions);
            cmd.SetComputeVectorParam(cs, "InvHighResolution", highResDepth.inverseDimensions);
            cmd.SetComputeVectorParam(cs, "AdditionalParams", new Vector4(noiseFilterWeight, stepSize, bTolerance, uTolerance));

            cmd.SetComputeTextureParam(cs, kernel, "LoResDB", lowResDepth.id);
            cmd.SetComputeTextureParam(cs, kernel, "HiResDB", highResDepth.id);
            cmd.SetComputeTextureParam(cs, kernel, "LoResAO1", interleavedAO.id);

            if (highResAO != null)
                cmd.SetComputeTextureParam(cs, kernel, "HiResAO", highResAO.id);

            cmd.SetComputeTextureParam(cs, kernel, "AoResult", dest.id);

            int xcount = (highResDepth.width  + 17) / 16;
            int ycount = (highResDepth.height + 17) / 16;
            cmd.DispatchCompute(cs, kernel, xcount, ycount, 1);
        }

        public void RenderAfterOpaque(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion");
            DoLazyInitialization(context);

            var sheet = m_PropertySheet;

            // In forward fog is applied at the object level in the grometry pass so we need to
            // apply it to AO as well or it'll drawn on top of the fog effect.
            // Not needed in Deferred.
            if (context.camera.actualRenderingPath == RenderingPath.Forward && RenderSettings.fog)
            {
                sheet.EnableKeyword("APPLY_FORWARD_FOG");
                sheet.properties.SetVector(
                    ShaderIDs.FogParams,
                    new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance)
                );
            }

            RebuildCommandBuffers(context);
            cmd.SetGlobalTexture(ShaderIDs.MSVOcclusionTexture, m_Result.id);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, BuiltinRenderTextureType.CameraTarget, m_PropertySheet, (int)Pass.CompositionForward);
            cmd.EndSample("Ambient Occlusion");
        }

        public void RenderAmbientOnly(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion Render");
            DoLazyInitialization(context);
            RebuildCommandBuffers(context);
            cmd.EndSample("Ambient Occlusion Render");
        }

        public void CompositeAmbientOnly(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion Composite");
            cmd.SetGlobalTexture(ShaderIDs.MSVOcclusionTexture, m_Result.id);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_MRT, BuiltinRenderTextureType.CameraTarget, m_PropertySheet, (int)Pass.CompositionDeferred);
            cmd.EndSample("Ambient Occlusion Composite");
        }

        public void Release()
        {
            if (m_Result != null)
            {
#if !UNITY_2017_1_OR_NEWER
                m_TiledDepth1.Destroy();
                m_TiledDepth2.Destroy();
                m_TiledDepth3.Destroy();
                m_TiledDepth4.Destroy();
#endif
                m_Result.Destroy();
            }
            
            m_TiledDepth1 = null;
            m_TiledDepth2 = null;
            m_TiledDepth3 = null;
            m_TiledDepth4 = null;
            m_Result = null;

        }
    }
}
