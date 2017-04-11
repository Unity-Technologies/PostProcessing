using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class FogModel : PostProcessingModel
    {
        [Serializable]
        public struct Settings
        {
            public enum SkyboxBehaviour
            {
                Include,
                Exclude,
                FadeTo
            }

            public SkyboxBehaviour skyboxBehaviour;

            public static Settings defaultSettings
            {
                get
                {
                    return new Settings
                    {
                        skyboxBehaviour = SkyboxBehaviour.Exclude,
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
