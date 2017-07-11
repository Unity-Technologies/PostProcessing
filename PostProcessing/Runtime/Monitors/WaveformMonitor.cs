using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class WaveformMonitor : Monitor
    {
        public float exposure = 0.12f;
        public int height = 256;

        ComputeBuffer m_Data;

        internal override void OnDisable()
        {
            base.OnDisable();

            if (m_Data != null)
                m_Data.Release();

            m_Data = null;
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
                context.width / 2,
                context.height / 2,
                RuntimeUtilities.isLinearColorSpace ? 1 : 0,
                0f
            );

            // Clear the buffer on every frame
            int kernel = compute.FindKernel("KWaveformClear");
            cmd.SetComputeBufferParam(compute, kernel, "_WaveformBuffer", m_Data);
            cmd.SetComputeVectorParam(compute, "_Params", parameters);
            cmd.SetComputeVectorParam(compute, "_BufferParams", new Vector4(width, height, 0f, 0f));
            cmd.DispatchCompute(compute, kernel, Mathf.CeilToInt(width / 16f), Mathf.CeilToInt(height / 16f), 1);

            // Gather all pixels and fill in our waveform
            kernel = compute.FindKernel("KWaveformGather");
            cmd.SetComputeBufferParam(compute, kernel, "_WaveformBuffer", m_Data);
            cmd.SetComputeTextureParam(compute, kernel, "_Source", ShaderIDs.HalfResFinalCopy);
            cmd.SetComputeVectorParam(compute, "_Params", parameters);
            cmd.DispatchCompute(compute, kernel, Mathf.CeilToInt(context.width / 2f), Mathf.CeilToInt(context.width / 2f / 256f), 1);

            // Generate the waveform texture
            var sheet = context.propertySheets.Get(context.resources.shaders.waveform);
            sheet.properties.SetVector(ShaderIDs.Params, new Vector4(width, height, exposure, 0f));
            sheet.properties.SetBuffer(ShaderIDs.WaveformBuffer, m_Data);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, output, sheet, 0);

            cmd.EndSample("Waveform");
        }
    }
}
