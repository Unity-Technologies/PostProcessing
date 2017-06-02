namespace UnityEngine.Experimental.PostProcessing
{   
    public sealed class LogHistogram
    {
        public static readonly int rangeMin = -9; // ev
        public static readonly int rangeMax =  9; // ev
        
        // Don't forget to update 'ExposureHistogram.hlsl' if you change these values !
        const int k_Bins = 128;
        const int k_ThreadX = 16;
        const int k_ThreadY = 16;

        public ComputeBuffer data { get; private set; }

        public void Generate(PostProcessRenderContext context)
        {
            if (data == null)
                data = new ComputeBuffer(k_Bins, sizeof(uint));

            var compute = context.resources.computeShaders.exposureHistogram;
            var cmd = context.command;
            cmd.BeginSample("LogHistogram");

            // Downscale the framebuffer, we don't need an absolute precision to compute the average
            // luminance (and it'll make it a tiny bit more stable - bonus side-effect)
            var scaleOffsetRes = GetHistogramScaleOffsetRes(context);

            cmd.GetTemporaryRT(Uniforms._AutoExposureCopyTex,
                (int)scaleOffsetRes.z,
                (int)scaleOffsetRes.w,
                0,
                FilterMode.Bilinear,
                context.sourceFormat
            );
            cmd.BlitFullscreenTriangle(context.source, Uniforms._AutoExposureCopyTex);

            // Clear the buffer on every frame as we use it to accumulate luminance values on each frame
            int kernel = compute.FindKernel("KEyeHistogramClear");
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", data);
            cmd.DispatchCompute(compute, kernel, Mathf.CeilToInt(k_Bins / (float)k_ThreadX), 1, 1);

            // Get a log histogram
            kernel = compute.FindKernel("KEyeHistogram");
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", data);
            cmd.SetComputeTextureParam(compute, kernel, "_Source", Uniforms._AutoExposureCopyTex);
            cmd.SetComputeVectorParam(compute, "_ScaleOffsetRes", scaleOffsetRes);
            cmd.DispatchCompute(compute, kernel,
                Mathf.CeilToInt(scaleOffsetRes.z / (float)k_ThreadX),
                Mathf.CeilToInt(scaleOffsetRes.w / (float)k_ThreadY),
                1
            );

            // Cleanup
            cmd.ReleaseTemporaryRT(Uniforms._AutoExposureCopyTex);

            cmd.EndSample("LogHistogram");
        }

        public Vector4 GetHistogramScaleOffsetRes(PostProcessRenderContext context)
        {
            float diff = rangeMax - rangeMin;
            float scale = 1f / diff;
            float offset = -rangeMin * scale;
            return new Vector4(scale, offset, Mathf.Floor(context.width / 2f), Mathf.Floor(context.height / 2f));
        }

        public void Release()
        {
            if (data != null)
                data.Release();

            data = null;
        }
    }
}
