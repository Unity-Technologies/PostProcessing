using UnityEngine.Rendering;

namespace UnityEngine.Experimental.PostProcessing
{
    // Context object passed around all post-fx in a frame
    public sealed class PostProcessRenderContext
    {
        // -----------------------------------------------------------------------------------------
        // The following should be filled by the render pipeline

        // Camera currently rendering
        public Camera camera { get; set; }

        // The command buffer to fill in
        public CommandBuffer command { get; set; }

        // Source target (can't be the same as destination)
        public RenderTargetIdentifier source { get; set; }

        // Destination target (can't be the same as source)
        public RenderTargetIdentifier destination { get; set; }

        // Texture format used for the source target
        // We need this to be set explictely as we don't have any way of knowing if we're rendering
        // using  HDR or not as scriptable render pipelines may ignore the HDR toggle on camera
        // completely
        public RenderTextureFormat sourceFormat { get; set; }

        // Should we flip the last pass?
        public bool flip { get; set; }
        
        // -----------------------------------------------------------------------------------------
        // The following is auto-populated by the post-processing stack

        // Property sheet factory handled by the currently active PostProcessLayer
        public PropertySheetFactory propertySheets { get; internal set; }

        // Pre-computed bluenoise textures
        public BlueNoise blueNoise { get; internal set; }

        // Custom user data object (unused by builtin effects, feel free to store whatever you want
        // in this object)
        public object userData { get; set; }

        // Current camera width in pixels
        public int width
        {
            get { return camera.pixelWidth; }
        }

        // Current camera height in pixels
        public int height
        {
            get { return camera.pixelHeight; }
        }
        
        // Current antialiasing method set
        public PostProcessLayer.Antialiasing antialiasing { get; internal set; }

        // Mostly used to grab the jitter vector and other TAA-related values when an effect needs
        // to do temporal reprojection (see: Depth of Field)
        public TemporalAntialiasing temporalAntialiasing { get; internal set; }

        public void Reset()
        {
            camera = null;
            command = null;
            source = 0;
            destination = 0;
            sourceFormat = RenderTextureFormat.ARGB32;
            flip = false;

            propertySheets = null;
            userData = null;
            antialiasing = PostProcessLayer.Antialiasing.None;
            temporalAntialiasing = null;
            blueNoise = null;

            uberSheet = null;
            autoExposureTexture = null;
        }

        // Checks if TAA is enabled & supported
        public bool IsTemporalAntialiasingActive()
        {
            return antialiasing == PostProcessLayer.Antialiasing.TemporalAntialiasing
                && temporalAntialiasing.IsSupported();
        }

        // Internal values used for builtin effects
        internal PropertySheet uberSheet;
        internal Texture autoExposureTexture;
    }
}
