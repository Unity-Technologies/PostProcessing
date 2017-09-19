namespace UnityEngine.Rendering.PostProcessing
{
#if UNITY_2017_2_OR_NEWER
    using XRSettings = UnityEngine.XR.XRSettings;
#elif UNITY_5_6_OR_NEWER
    using XRSettings = UnityEngine.VR.VRSettings;
#endif

    // Context object passed around all post-fx in a frame
    public sealed class PostProcessRenderContext
    {
        // -----------------------------------------------------------------------------------------
        // The following should be filled by the render pipeline

        // Camera currently rendering
        Camera m_Camera;
        public Camera camera
        {
            get { return m_Camera; }
            set
            {
                m_Camera = value;

                if (XRSettings.isDeviceActive)
                {
#if UNITY_2017_2_OR_NEWER
                    RenderTextureDescriptor xrDesc = XRSettings.eyeTextureDesc;
                    width = xrDesc.width;
                    height = xrDesc.height;
#else
                    width = XRSettings.eyeTextureWidth; // double this for single-pass when we can't query eyeTextureDesc
                    height = XRSettings.eyeTextureHeight;
#endif

                    if (camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                        xrActiveEye = (int)Camera.StereoscopicEye.Right;

                    xrSingleEyeWidth = XRSettings.eyeTextureWidth;
                    xrSingleEyeHeight = XRSettings.eyeTextureHeight;
                }
                else
                {
                    width = m_Camera.pixelWidth;
                    height = m_Camera.pixelHeight;
                    xrSingleEyeWidth = width;
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
        // TODO: Make this more useful
        public object userData { get; set; }

        // Reference to the internal debug layer
        public PostProcessDebugLayer debugLayer { get; internal set; }

        // Current camera width in pixels
        public int width { get; private set; }

        // Current camera height in pixels
        public int height { get; private set; }

        // Current active rendering eye (for XR)
        public int xrActiveEye { get; private set; }

        // Current single eye width in pixels (for XR)
        public int xrSingleEyeWidth { get; private set; }

        // Current single eye height in pixels (for XR)
        public int xrSingleEyeHeight { get; private set; }

        // Are we currently rendering in the scene view?
        public bool isSceneView { get; internal set; }
        
        // Current antialiasing method set
        public PostProcessLayer.Antialiasing antialiasing { get; internal set; }

        // Mostly used to grab the jitter vector and other TAA-related values when an effect needs
        // to do temporal reprojection (see: Depth of Field)
        public TemporalAntialiasing temporalAntialiasing { get; internal set; }

        public void Reset()
        {
            m_Camera = null;
            width = 0;
            height = 0;

            xrActiveEye = (int)Camera.StereoscopicEye.Left;
            xrSingleEyeWidth = 0;
            xrSingleEyeHeight = 0;

            command = null;
            source = 0;
            destination = 0;
            sourceFormat = RenderTextureFormat.ARGB32;
            flip = false;

            resources = null;
            propertySheets = null;
            userData = null;
            debugLayer = null;
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

        // Checks if a specific debug overlay is enabled
        public bool IsDebugOverlayEnabled(DebugOverlay overlay)
        {
            return debugLayer.debugOverlay == overlay;
        }

        // Shortcut function
        public void PushDebugOverlay(CommandBuffer cmd, RenderTargetIdentifier source, PropertySheet sheet, int pass)
        {
            debugLayer.PushDebugOverlay(cmd, source, sheet, pass);
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
