using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class DitheringModel : PostProcessingModel
    {
        [Serializable]
        public struct Settings
        {
            public int depthSelectionMode; // 0: Single Slider, 1: RGB Sliders, 2: Automatic

            public int bitsPerChannel_R, bitsPerChannel_G, bitsPerChannel_B;

            public int ditheringMode; // 0: Disabled, 1: Static, 2: Animated

            public static Settings defaultSettings
            {
                get
                {
                    return new Settings
                    {
                        depthSelectionMode = 1,
                        bitsPerChannel_R = 11,
                        bitsPerChannel_G = 11,
                        bitsPerChannel_B = 10,
                        ditheringMode = 2
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
