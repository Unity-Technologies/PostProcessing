using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class DitheringModel : PostProcessingModel
    {
        [Serializable]
        public struct Settings
        {
            [Range(3,42),Tooltip("Display bit depth. This is affects how dithering is applied.")]
            public int colorDepth;
            
            [Tooltip("Automatically set the bit depth to match the display. (Currently doesn't do anything)")]
            public bool detectBitDepth;

            [Range(0,1),Tooltip("Blend between the dithered and non-dithered image.")]
            public float ditheringAmount;

            [Tooltip("Should the noise be static or animated? (Animated noise looks best in most cases.)")]
            public bool animatedNoise;

            [Tooltip("Should \"Dither Range Limit\" be determined automatically?\n(Note: While this option will give good results, it is based on arbitrary math, so the results aren't guaranteed to be optimal.)")]
            public bool limitAutomatically;

            [Range(0,1),Tooltip("Balance between dithering and color correctness in dark areas.\n 0: Dither everywhere, but brightens the dark colors.\n 1: Preserves color luminance, but looks worse with low color depth.")]
            public float ditherRangeLimit;

            [Tooltip("Limit the output image color depth to the current setting. (Check this option when \"Detect Bit Depth\" is turned off. This is mostly intended for testing.)")]
            public bool previewColorDepth;

            public static Settings defaultSettings
            {
                get
                {
                    return new Settings
                    {
                        colorDepth = 32,
                        detectBitDepth = true,
                        ditheringAmount = 1f,
                        animatedNoise = true,
                        limitAutomatically = true,
                        ditherRangeLimit=1,
                        previewColorDepth = true
                    };
                }
            }
        }

        [SerializeField]
        Settings m_Settings = Settings.defaultSettings;
        public Settings settings
        {
            get { return m_Settings; }
            set { m_Settings = value; }
        }

        public override void Reset()
        {
            m_Settings = Settings.defaultSettings;
        }
    }
}
