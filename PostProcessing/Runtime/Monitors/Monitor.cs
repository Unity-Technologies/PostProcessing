namespace UnityEngine.Rendering.PostProcessing
{
    public abstract class Monitor
    {
        public bool enabled = false;
        public RenderTexture output { get; protected set; }

        public bool IsEnabledAndSupported()
        {
            return enabled
                && SystemInfo.supportsComputeShaders;
        }

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
