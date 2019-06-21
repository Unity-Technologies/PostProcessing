using System.Collections.Generic;

namespace UnityEngine.Rendering.PPSMobile
{
    /// <summary>
    /// A context object passed around all post-processing effects in a frame.
    /// </summary>
    public sealed class PostProcessRenderContext
    {
        // -----------------------------------------------------------------------------------------
        // The following should be filled by the render pipeline

        Camera m_Camera;

        /// <summary>
        /// The camera currently being rendered.
        /// </summary>
        public Camera camera
        {
            get { return m_Camera; }
            set
            {
                m_Camera = value;

                width = m_Camera.pixelWidth;
                height = m_Camera.pixelHeight;

#if UNITY_2017_2_OR_NEWER
                m_sourceDescriptor.width = width;
                m_sourceDescriptor.height = height;
#endif
                screenWidth = width;
                screenHeight = height;
            }
        }


        /// <summary>
        /// The command buffer to fill render commands in.
        /// </summary>
        public CommandBuffer command { get; set; }

        /// <summary>
        /// The source target for this pass (can't be the same as <see cref="destination"/>).
        /// </summary>
        public RenderTargetIdentifier source { get; set; }

        /// <summary>
        /// The destination target for this pass (can't be the same as <see cref="source"/>).
        /// </summary>
        public RenderTargetIdentifier destination { get; set; }

        /// <summary>
        /// The texture format used for the source target.
        /// </summary>
        // We need this to be set explictely as we don't have any way of knowing if we're rendering
        // using  HDR or not as scriptable render pipelines may ignore the HDR toggle on camera
        // completely
        public RenderTextureFormat sourceFormat { get; set; }

        /// <summary>
        /// Should we flip the last pass?
        /// </summary>
        public bool flip { get; set; }

        // -----------------------------------------------------------------------------------------
        // The following is auto-populated by the post-processing stack

        /// <summary>
        /// The resource asset contains reference to external resources (shaders, textures...).
        /// </summary>
        public PostProcessResources resources { get; internal set; }

        /// <summary>
        /// The property sheet factory handled by the currently active <see cref="PostProcessLayer"/>.
        /// </summary>
        public PropertySheetFactory propertySheets { get; internal set; }

        /// <summary>
        /// A dictionary to store custom user data objects. This is handy to share data between
        /// custom effects.
        /// </summary>
        public Dictionary<string, object> userData { get; private set; }

        /// <summary>
        /// A reference to the internal debug layer.
        /// </summary>
        public PostProcessDebugLayer debugLayer { get; internal set; }

        /// <summary>
        /// The current camera width (in pixels).
        /// </summary>
        public int width { get; private set; }

        /// <summary>
        /// The current camera height (in pixels).
        /// </summary>
        public int height { get; private set; }

        /// <summary>
        /// Available XR rendering modes.
        /// </summary>
        public enum StereoRenderingMode
        {
            MultiPass = 0,
            SinglePass,
            SinglePassInstanced,
            SinglePassMultiview
        }

        /// <summary>
        /// The current rendering mode for XR.
        /// </summary>
        public StereoRenderingMode stereoRenderingMode { get; private set; }

        /// <summary>
        /// The width of the logical screen size.
        /// </summary>
        public int screenWidth { get; private set; }

        /// <summary>
        /// The height of the logical screen size.
        /// </summary>
        public int screenHeight { get; private set; }

        /// <summary>
        /// Are we currently rendering in the scene view?
        /// </summary>
        public bool isSceneView { get; internal set; }

        /// <summary>
        /// The current anti-aliasing method used by the camera.
        /// </summary>
        public PostProcessLayer.Antialiasing antialiasing { get; internal set; }

        // Internal values used for builtin effects
        // Beware, these may not have been set before a specific builtin effect has been executed
        internal PropertySheet uberSheet;
        internal Texture autoExposureTexture;
        internal LogHistogram logHistogram;
        internal Texture logLut;
        internal AutoExposure autoExposure;
        internal int bloomBufferNameID;
#if UNITY_2018_2_OR_NEWER
        internal bool physicalCamera;
#endif

        /// <summary>
        /// Resets the state of this context object. This is called by the render pipeline on every
        /// frame and allows re-using the same context object between frames without having to
        /// recreate a new one.
        /// </summary>
        public void Reset()
        {
            m_Camera = null;
            width = 0;
            height = 0;

#if UNITY_2017_2_OR_NEWER
            m_sourceDescriptor = new RenderTextureDescriptor(0, 0);
#endif
#if UNITY_2018_2_OR_NEWER
            physicalCamera = false;
#endif
            screenWidth = 0;
            screenHeight = 0;

            command = null;
            source = 0;
            destination = 0;
            sourceFormat = RenderTextureFormat.ARGB32;
            flip = false;

            resources = null;
            propertySheets = null;
            debugLayer = null;
            isSceneView = false;
            antialiasing = PostProcessLayer.Antialiasing.None;

            uberSheet = null;
            autoExposureTexture = null;
            logLut = null;
            autoExposure = null;
            bloomBufferNameID = -1;

            if (userData == null)
                userData = new Dictionary<string, object>();

            userData.Clear();
        }

        /// <summary>
        /// Checks if a specific debug overlay is enabled.
        /// </summary>
        /// <param name="overlay">The debug overlay to look for</param>
        /// <returns><c>true</c> if the specified debug overlay is enable, <c>false</c>
        /// otherwise</returns>
        public bool IsDebugOverlayEnabled(DebugOverlay overlay)
        {
            return debugLayer.debugOverlay == overlay;
        }

        /// <summary>
        /// Blit a source render target to the debug overlay target. This is a direct shortcut to
        /// <see cref="PostProcessDebugLayer.PushDebugOverlay"/>.
        /// </summary>
        /// <param name="cmd">The command buffer to send render commands to</param>
        /// <param name="source">The source target</param>
        /// <param name="sheet">The property sheet to use for the blit</param>
        /// <param name="pass">The pass to use for the property sheet</param>
        /// <seealso cref="PostProcessDebugLayer.PushDebugOverlay"/>
        public void PushDebugOverlay(CommandBuffer cmd, RenderTargetIdentifier source, PropertySheet sheet, int pass)
        {
            debugLayer.PushDebugOverlay(cmd, source, sheet, pass);
        }

        // TODO: Change w/h name to texture w/h in order to make
        // size usages explicit
#if UNITY_2017_2_OR_NEWER
        RenderTextureDescriptor m_sourceDescriptor;
        RenderTextureDescriptor GetDescriptor(int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default)
        {
            var modifiedDesc = new RenderTextureDescriptor(m_sourceDescriptor.width, m_sourceDescriptor.height,
                                                                                m_sourceDescriptor.colorFormat, depthBufferBits);
            modifiedDesc.dimension = m_sourceDescriptor.dimension;
            modifiedDesc.volumeDepth = m_sourceDescriptor.volumeDepth;
            modifiedDesc.vrUsage = m_sourceDescriptor.vrUsage;
            modifiedDesc.msaaSamples = m_sourceDescriptor.msaaSamples;
            modifiedDesc.memoryless = m_sourceDescriptor.memoryless;

            modifiedDesc.useMipMap = m_sourceDescriptor.useMipMap;
            modifiedDesc.autoGenerateMips = m_sourceDescriptor.autoGenerateMips;
            modifiedDesc.enableRandomWrite = m_sourceDescriptor.enableRandomWrite;
            modifiedDesc.shadowSamplingMode = m_sourceDescriptor.shadowSamplingMode;

            if (colorFormat != RenderTextureFormat.Default)
                modifiedDesc.colorFormat = colorFormat;

#if UNITY_2019_1_OR_NEWER
            if (readWrite == RenderTextureReadWrite.sRGB)
                modifiedDesc.sRGB = true;
            else if (readWrite == RenderTextureReadWrite.Linear)
                modifiedDesc.sRGB = false;
            else if (readWrite == RenderTextureReadWrite.Default)
                modifiedDesc.sRGB = QualitySettings.activeColorSpace != ColorSpace.Gamma;
#else
            modifiedDesc.sRGB = readWrite != RenderTextureReadWrite.Linear;
#endif

            return modifiedDesc;
        }
#endif

        /// <summary>
        /// Grabs a temporary render target with the current display size.
        /// </summary>
        /// <param name="cmd">The command buffer to grab a render target from</param>
        /// <param name="nameID">The shader property name for this texture</param>
        /// <param name="depthBufferBits">The number of bits to use for the depth buffer</param>
        /// <param name="colorFormat">The render texture format</param>
        /// <param name="readWrite">The color space conversion mode</param>
        /// <param name="filter">The texture filtering mode</param>
        /// <param name="widthOverride">Override the display width; use <c>0</c> to disable the override</param>
        /// <param name="heightOverride">Override the display height; use <c>0</c> to disable the override</param>
        public void GetScreenSpaceTemporaryRT(CommandBuffer cmd, int nameID,
                                            int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
                                            FilterMode filter = FilterMode.Bilinear, int widthOverride = 0, int heightOverride = 0)
        {
#if UNITY_2017_2_OR_NEWER
            var desc = GetDescriptor(depthBufferBits, colorFormat, readWrite);
            if (widthOverride > 0)
                desc.width = widthOverride;
            if (heightOverride > 0)
                desc.height = heightOverride;
          
            cmd.GetTemporaryRT(nameID, desc, filter);
#else
            int actualWidth = width;
            int actualHeight = height;
            if (widthOverride > 0)
                actualWidth = widthOverride;
            if (heightOverride > 0)
                actualHeight = heightOverride;

            cmd.GetTemporaryRT(nameID, actualWidth, actualHeight, depthBufferBits, filter, colorFormat, readWrite);
            // TODO: How to handle MSAA for XR in older versions?  Query cam?
            // TODO: Pass in vrUsage into the args
#endif
        }

        /// <summary>
        /// Grabs a temporary render target with the current display size.
        /// </summary>
        /// <param name="depthBufferBits">The number of bits to use for the depth buffer</param>
        /// <param name="colorFormat">The render texture format</param>
        /// <param name="readWrite">The color space conversion mode</param>
        /// <param name="widthOverride">Override the display width; use <c>0</c> to disable the override</param>
        /// <param name="heightOverride">Override the display height; use <c>0</c> to disable the override</param>
        /// <returns>A temporary render target</returns>
        public RenderTexture GetScreenSpaceTemporaryRT(int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default,
                                                        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default, int widthOverride = 0, int heightOverride = 0)
        {
#if UNITY_2017_2_OR_NEWER
            var desc = GetDescriptor(depthBufferBits, colorFormat, readWrite);
            if (widthOverride > 0)
                desc.width = widthOverride;
            if (heightOverride > 0)
                desc.height = heightOverride;

            return RenderTexture.GetTemporary(desc);
#else
            int actualWidth = width;
            int actualHeight = height;
            if (widthOverride > 0)
                actualWidth = widthOverride;
            if (heightOverride > 0)
                actualHeight = heightOverride;

            return RenderTexture.GetTemporary(actualWidth, actualHeight, depthBufferBits, colorFormat, readWrite);
#endif
        }
    }
}
