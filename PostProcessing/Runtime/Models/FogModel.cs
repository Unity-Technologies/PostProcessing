using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class FogModel : PostProcessingModel
    {
        [Serializable]
        public struct Settings
        {
            [Tooltip("Should the fog affect the skybox?")]
            public bool excludeSkybox;
            [Tooltip("Should the fog sample the skybox instead of using the fog color?")]
            public bool fadeToSkybox;

            public static Settings defaultSettings
            {
                get
                {
                    return new Settings
                    {
                        excludeSkybox = true,
                        fadeToSkybox = false
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
