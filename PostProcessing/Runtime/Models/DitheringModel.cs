using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class DitheringModel : PostProcessingModel
    {
        [Serializable]
        public struct Settings
        {
            [Range(3,48),Tooltip("Bit depth of the display. This is affects how dithering is applied.")]
            public int colorDepth;
            
            [Tooltip("Automatically set the bit depth to match the display. (Currently doesn't do anything)")]
            public bool detectBitDepth;

            [Range(0,1),Tooltip("Blend between the dithered and non-dithered image.")]
            public float ditheringAmount;

            [Tooltip("Should the noise be static or animated? (Animated noise looks best in most cases.)")]
            public bool animatedNoise;

            [Tooltip("Should \"Dither Range Limit\" be determined automatically?")]
            public bool limitAutomatically;

            [Range(0,1),Tooltip("Balance between dithering and color correctness in dark areas.\n 0: Dither the entire image, but brightens the dark colors.\n 1: Preserves luminance in dark areas, but looks worse with low color depth.")]
            public float ditherRangeLimit;

            [Tooltip("Limits the output image color depth to the current setting.")]
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
