using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class WaveformMonitor : Monitor
    {
        public float exposure = 0.12f;
        public int height = 256;

        ComputeBuffer m_Data;

        int m_ThreadGroupSize;
        int m_ThreadGroupSizeX;
        int m_ThreadGroupSizeY;

        internal override void OnEnable()
        {
            m_ThreadGroupSizeX = 16;

            if (RuntimeUtilities.isAndroidOpenGL)
            {
                m_ThreadGroupSize = 128;
                m_ThreadGroupSizeY = 8;
            }
            else
            {
                m_ThreadGroupSize = 256;
                m_ThreadGroupSizeY = 16;
            }
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            if (m_Data != null)
                m_Data.Release();

            m_Data = null;
        }

        internal override bool NeedsHalfRes()
        {
            return true;
        }

        internal override void Render(PostProcessRenderContext context)
        {
            // Waveform show localized data, so width depends on the aspect ratio
            float ratio = (context.width / 2f) / (context.height / 2f);
            int width = Mathf.FloorToInt(height * ratio);

            CheckOutput(width, height);
            exposure = Mathf.Max(0f, exposure);

            int count = width * height;
            if (m_Data == null)
            {
                m_Data = new ComputeBuffer(count, sizeof(uint) << 2);
            }
            else if (m_Data.count < count)
            {
                m_Data.Release();
                m_Data = new ComputeBuffer(count, sizeof(uint) << 2);
            }

            var compute = context.resources.computeShaders.waveform;
            var cmd = context.command;
            cmd.BeginSample("Waveform");

            var parameters = new Vector4(
                width,
                height,
                RuntimeUtilities.isLinearColorSpace ? 1 : 0,
                0f
            );

            // Clear the buffer on every frame
            int kernel = compute.FindKernel("KWaveformClear");
            cmd.SetComputeBufferParam(compute, kernel, "_WaveformBuffer", m_Data);
            cmd.SetComputeVectorParam(compute, "_Params", parameters);
            cmd.DispatchCompute(compute, kernel, Mathf.CeilToInt(width / (float)m_ThreadGroupSizeX), Mathf.CeilToInt(height / (float)m_ThreadGroupSizeY), 1);

            // For performance reasons, especially on consoles, we'll just downscale the source
            // again to reduce VMEM stalls. Eventually the whole algorithm needs to be rewritten as
            // it's currently pretty naive.
            cmd.GetTemporaryRT(ShaderIDs.WaveformSource, width, height, 0, FilterMode.Bilinear, context.sourceFormat);
            cmd.BlitFullscreenTriangle(ShaderIDs.HalfResFinalCopy, ShaderIDs.WaveformSource);

            // Gather all pixels and fill in our waveform
            kernel = compute.FindKernel("KWaveformGather");
            cmd.SetComputeBufferParam(compute, kernel, "_WaveformBuffer", m_Data);
            cmd.SetComputeTextureParam(compute, kernel, "_Source", ShaderIDs.WaveformSource);
            cmd.SetComputeVectorParam(compute, "_Params", parameters);
            cmd.DispatchCompute(compute, kernel, width, Mathf.CeilToInt(height / (float)m_ThreadGroupSize), 1);
            cmd.ReleaseTemporaryRT(ShaderIDs.WaveformSource);

            // Generate the waveform texture
            var sheet = context.propertySheets.Get(context.resources.shaders.waveform);
            sheet.properties.SetVector(ShaderIDs.Params, new Vector4(width, height, exposure, 0f));
            sheet.properties.SetBuffer(ShaderIDs.WaveformBuffer, m_Data);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, output, sheet, 0);

            cmd.EndSample("Waveform");
        }
    }
}
