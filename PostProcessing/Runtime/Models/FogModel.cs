using System;

namespace UnityEngine.PostProcessing
{
    [Serializable]
    public class FogModel : PostProcessingModel
    {
        [Serializable]
        public struct Settings
        {
            [Tooltip("Controls the color of that fog drawn in the scene.")]
            public Color color;

            [Tooltip("Controls the mathematical function determining the way fog accumulates with distance from the camera. Options are Linear, Exponential and Exponential Squared.")]
            public FogMode mode;

            [Tooltip("Controls the density of the fog effect in the Scene when using Exponential or Exponential Squared modes.")]
            public float density;

            [Tooltip("Controls the distance from the camera where the fog will start in the scene.")]
            public float start;

            [Tooltip("Controls the distance from the camera where the fog will completely obscure objects in the Scene.")]
            public float end;

            [Tooltip("Should the fog affect the skybox?")]
            public bool excludeSkybox;

            public static Settings defaultSettings
            {
                get
                {
                    return new Settings
                    {
                        color = new Color32(102, 108, 113, 154),
                        mode = FogMode.Exponential,
                        density = 0.001f,
                        start = 0f,
                        end = 600f,
                        excludeSkybox = true
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
