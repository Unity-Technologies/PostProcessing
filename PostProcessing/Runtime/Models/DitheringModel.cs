using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class DitheringModel : PostProcessingModel
    {
        [Serializable]
        public struct Settings
        {
            public int depthSelectionMode;

            public int bitsPerChannel_R, bitsPerChannel_G, bitsPerChannel_B;

            public bool animatedNoise;

            public float amount;

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
                        animatedNoise = true,
                        amount = 1f
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
