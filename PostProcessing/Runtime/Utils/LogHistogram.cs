namespace UnityEngine.Rendering.PostProcessing
{   
    internal sealed class LogHistogram
    {
        public const int rangeMin = -9; // ev
        public const int rangeMax =  9; // ev
        
        // Don't forget to update 'ExposureHistogram.hlsl' if you change these values !
        const int k_Bins = 128;

        public ComputeBuffer data { get; private set; }

        public void Generate(PostProcessRenderContext context)
        {
            if (data == null)
                data = new ComputeBuffer(k_Bins, sizeof(uint));

            uint threadX, threadY, threadZ;
            var scaleOffsetRes = GetHistogramScaleOffsetRes(context);
            var compute = context.resources.computeShaders.exposureHistogram;
            var cmd = context.command;
            cmd.BeginSample("LogHistogram");

            // Clear the buffer on every frame as we use it to accumulate luminance values on each frame
            int kernel = compute.FindKernel("KEyeHistogramClear");
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", data);
            compute.GetKernelThreadGroupSizes(kernel, out threadX, out threadY, out threadZ);
            cmd.DispatchCompute(compute, kernel, Mathf.CeilToInt(k_Bins / (float)threadX), 1, 1);

            // Get a log histogram
            kernel = compute.FindKernel("KEyeHistogram");
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", data);
            cmd.SetComputeTextureParam(compute, kernel, "_Source", context.source);
            cmd.SetComputeVectorParam(compute, "_ScaleOffsetRes", scaleOffsetRes);

            compute.GetKernelThreadGroupSizes(kernel, out threadX, out threadY, out threadZ);
            cmd.DispatchCompute(compute, kernel,
                Mathf.CeilToInt(scaleOffsetRes.z / 2f / threadX),
                Mathf.CeilToInt(scaleOffsetRes.w / 2f / threadY),
                1
            );

            cmd.EndSample("LogHistogram");
        }

        public Vector4 GetHistogramScaleOffsetRes(PostProcessRenderContext context)
        {
            float diff = rangeMax - rangeMin;
            float scale = 1f / diff;
            float offset = -rangeMin * scale;
            return new Vector4(scale, offset, context.width, context.height);
        }

        public void Release()
        {
            if (data != null)
                data.Release();

            data = null;
        }
    }
}
