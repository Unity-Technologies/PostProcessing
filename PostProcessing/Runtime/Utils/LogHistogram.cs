namespace UnityEngine.Rendering.PostProcessing
{   
    public sealed class LogHistogram
    {
        public const int rangeMin = -9; // ev
        public const int rangeMax =  9; // ev
        
        // Don't forget to update 'ExposureHistogram.hlsl' if you change these values !
        const int k_Bins = 128;
        int threadX;
        int threadY;

        public ComputeBuffer data { get; private set; }

        public void Generate(PostProcessRenderContext context)
        {
			if (data == null)
			{
                bool isAndroidOpenGL = Application.platform == RuntimePlatform.Android && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan;
                threadX = isAndroidOpenGL ? 16 : 16;
                threadY = isAndroidOpenGL ? 8 : 16;
				data = new ComputeBuffer (k_Bins, sizeof(uint));
			}
            
            var scaleOffsetRes = GetHistogramScaleOffsetRes(context);
            var compute = context.resources.computeShaders.exposureHistogram;
            var cmd = context.command;
            cmd.BeginSample("LogHistogram");

            // Clear the buffer on every frame as we use it to accumulate luminance values on each frame
            int kernel = compute.FindKernel("KEyeHistogramClear");
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", data);
            cmd.DispatchCompute(compute, kernel, Mathf.CeilToInt(k_Bins / (float)threadX), 1, 1);

            // Get a log histogram
            kernel = compute.FindKernel("KEyeHistogram");
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", data);
            cmd.SetComputeTextureParam(compute, kernel, "_Source", context.source);
            cmd.SetComputeVectorParam(compute, "_ScaleOffsetRes", scaleOffsetRes);
            cmd.DispatchCompute(compute, kernel,
                Mathf.CeilToInt(scaleOffsetRes.z / (float)threadX),
                Mathf.CeilToInt(scaleOffsetRes.w / (float)threadY),
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
