namespace UnityEngine.Rendering.PostProcessing
{
    // Context object passed around all post-fx in a frame
    public sealed class PostProcessRenderContext
    {
        // -----------------------------------------------------------------------------------------
        // The following should be filled by the render pipeline

        // Camera currently rendering
        private Camera m_camera;
        public Camera camera
        {
            get
            {
                return this.m_camera;
            }

            set
            {
                this.m_camera = value;

                if (XR.XRSettings.isDeviceActive)
                {
                    RenderTextureDescriptor xrDesc = XR.XRSettings.eyeTextureDesc;
                    m_width = xrDesc.width;
                    m_height = xrDesc.height;

                    m_xrSinglePass = (xrDesc.vrUsage == VRTextureUsage.TwoEyes);

                    if (camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                        m_xrActiveEye = (int)Camera.StereoscopicEye.Right;

                    if (m_xrSinglePass && (xrDesc.dimension != TextureDimension.Tex2DArray))
                        m_xrSingleEyeWidth = m_width / 2;
                    else
                        m_xrSingleEyeWidth = m_width;
                }
                else
                {
                    m_width = m_camera.pixelWidth;
                    m_height = m_camera.pixelHeight;
                    m_xrSingleEyeWidth = m_width;
                }
            }
        }


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

        // Contains references to external resources (shaders, builtin textures...)
        public PostProcessResources resources { get; internal set; }

        // Property sheet factory handled by the currently active PostProcessLayer
        public PropertySheetFactory propertySheets { get; internal set; }

        // Custom user data object (unused by builtin effects, feel free to store whatever you want
        // in this object)
        public object userData { get; set; }

        // Current camera width in pixels
        private int m_width;
        public int width
        {
            get { return m_width; }
        }

        // Current camera height in pixels
        private int m_height;
        public int height
        {
            get { return m_height; }
        }

        // Is XR running in single-pass stereo mode?
        private bool m_xrSinglePass;
        public bool xrSinglePass
        {
            get { return m_xrSinglePass; }
        }

        // Current active rendering eye (for XR)
        private int m_xrActiveEye;
        public int xrActiveEye
        {
            get { return m_xrActiveEye; }
        }

        // Current single eye width in pixels (for XR)
        private int m_xrSingleEyeWidth;
        public int singleEyeWidth
        {
            get { return m_xrSingleEyeWidth; }
        }

        // Are we currently rendering in the scene view?
        public bool isSceneView { get; internal set; }
        
        // Current antialiasing method set
        public PostProcessLayer.Antialiasing antialiasing { get; internal set; }

        // Mostly used to grab the jitter vector and other TAA-related values when an effect needs
        // to do temporal reprojection (see: Depth of Field)
        public TemporalAntialiasing temporalAntialiasing { get; internal set; }

        public void Reset()
        {
            m_camera = null;
            m_width = 0;
            m_height = 0;

            m_xrSinglePass = false;
            m_xrActiveEye = (int)Camera.StereoscopicEye.Left;
            m_xrSingleEyeWidth = 0;

            command = null;
            source = 0;
            destination = 0;
            sourceFormat = RenderTextureFormat.ARGB32;
            flip = false;

            resources = null;
            propertySheets = null;
            userData = null;
            isSceneView = false;
            antialiasing = PostProcessLayer.Antialiasing.None;
            temporalAntialiasing = null;

            uberSheet = null;
            autoExposureTexture = null;
            logLut = null;
            autoExposure = null;
        }

        // Checks if TAA is enabled & supported
        public bool IsTemporalAntialiasingActive()
        {
            return antialiasing == PostProcessLayer.Antialiasing.TemporalAntialiasing
                && !isSceneView
                && temporalAntialiasing.IsSupported();
        }

        // Internal values used for builtin effects
        // Beware, these may not have been set before a specific builtin effect has been executed
        internal PropertySheet uberSheet;
        internal Texture autoExposureTexture;
        internal LogHistogram logHistogram;
        internal Texture logLut;
        internal AutoExposure autoExposure;
    }
}
