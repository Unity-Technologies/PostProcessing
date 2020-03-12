namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Debug monitor types.
    /// </summary>
    public enum MonitorType
    {
        /// <summary>
        /// Light meter.
        /// </summary>
        LightMeter,

        /// <summary>
        /// Gamma histogram.
        /// </summary>
        Histogram,

        /// <summary>
        /// Waveform.
        /// </summary>
        Waveform,

        /// <summary>
        /// YUV vectorscope.
        /// </summary>
        Vectorscope
    }

    /// <summary>
    /// The base class for all debug monitors.
    /// </summary>
    public abstract class Monitor
    {
        /// <summary>
        /// The target texture to render this monitor to.
        /// </summary>
        public RenderTexture output { get; protected set; }

        internal bool requested = false;

        /// <summary>
        /// Checks if a monitor is supported and should be rendered.
        /// </summary>
        /// <param name="context">The current post-processing context.</param>
        /// <returns><c>true</c> if supported and enabled, <c>false</c> otherwise.</returns>
        public bool IsRequestedAndSupported(PostProcessRenderContext context)
        {
            return requested
                && SystemInfo.supportsComputeShaders
                && !RuntimeUtilities.isAndroidOpenGL
                && ShaderResourcesAvailable(context);
        }

        internal abstract bool ShaderResourcesAvailable(PostProcessRenderContext context);

        internal virtual bool NeedsHalfRes()
        {
            return false;
        }

        /// <summary>
        /// Validates the output texture.
        /// </summary>
        /// <param name="width">The output width.</param>
        /// <param name="height">The output height.</param>
        protected void CheckOutput(int width, int height)
        {
            if (output == null || !output.IsCreated() || output.width != width || output.height != height)
            {
                RuntimeUtilities.Destroy(output);
                output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    anisoLevel = 0,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false
                };
            }
        }

        internal virtual void OnEnable()
        {
        }

        internal virtual void OnDisable()
        {
            RuntimeUtilities.Destroy(output);
        }

        internal abstract void Render(PostProcessRenderContext context);
    }
}
