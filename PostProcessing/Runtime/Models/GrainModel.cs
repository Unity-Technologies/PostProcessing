using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class GrainModel : PostProcessingModel
    {
        [Serializable]
        public struct Settings
        {
            [Tooltip("Enable the use of colored grain.")]
            public bool colored;

            [Range(0f, 1f), Tooltip("Grain strength. Higher means more visible grain.")]
            public float intensity;

            [Range(0f, 2f), Tooltip("Grain weight for the red channel. Higher means more visible grain.")]
            public float weightR;

            [Range(0f, 2f), Tooltip("Grain weight for the green channel. Higher means more visible grain.")]
            public float weightG;

            [Range(0f, 2f), Tooltip("Grain weight for the blue channel. Higher means more visible grain.")]
            public float weightB;

            [Range(1.5f, 3f), Tooltip("Grain particle size in \"Filmic\" mode.")]
            public float size;

            [Range(0f, 1f), Tooltip("Controls the noisiness response curve based on scene luminance. Lower values mean less noise in dark areas.")]
            public float luminanceContribution;

            public static Settings defaultSettings
            {
                get
                {
                    return new Settings
                    {
                        colored = false,
                        intensity = 0.12f,
                        weightR = 1f,
                        weightG = 1f,
                        weightB = 1f,
                        size = 1.6f,
                        luminanceContribution = 0.75f
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
