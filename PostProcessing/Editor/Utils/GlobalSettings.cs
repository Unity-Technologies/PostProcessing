namespace UnityEditor.Experimental.PostProcessing
{
    static class GlobalSettings
    {
        static class Keys
        {
            internal const string trackballSensitivity = "PostProcessing.Trackball.Sensitivity";
            internal const string currentChannelMixer  = "PostProcessing.ChannelMixer.CurrentChannel";
            internal const string currentCurve         = "PostProcessing.Curve.Current";
            internal const string showLayerToolkit     = "PostProcessing.Layer.showLayerToolkit";
            internal const string showCustomSorter     = "PostProcessing.Layer.ShowCustomSorter";
        }

        static bool m_Loaded = false;

        static float m_TrackballSensitivity = 0.2f;
        internal static float trackballSensitivity
        {
            get { return m_TrackballSensitivity; }
            set { TrySave(ref m_TrackballSensitivity, value, Keys.trackballSensitivity); }
        }

        static int m_CurrentChannelMixer = 0;
        internal static int currentChannelMixer
        {
            get { return m_CurrentChannelMixer; }
            set { TrySave(ref m_CurrentChannelMixer, value, Keys.currentChannelMixer); }
        }

        static int m_CurrentCurve = 0;
        internal static int currentCurve
        {
            get { return m_CurrentCurve; }
            set { TrySave(ref m_CurrentCurve, value, Keys.currentCurve); }
        }

        static bool m_ShowCustomSorter = false;
        internal static bool showCustomSorter
        {
            get { return m_ShowCustomSorter; }
            set { TrySave(ref m_ShowCustomSorter, value, Keys.showCustomSorter); }
        }

        static bool m_ShowLayerToolkit = false;
        internal static bool showLayerToolkit
        {
            get { return m_ShowLayerToolkit; }
            set { TrySave(ref m_ShowLayerToolkit, value, Keys.showLayerToolkit); }
        }

        static GlobalSettings()
        {
            Load();
        }

        [PreferenceItem("PostProcessing")]
        static void PreferenceGUI()
        {
            if (!m_Loaded)
                Load();

            EditorGUILayout.Space();

            trackballSensitivity = EditorGUILayout.Slider("Trackballs Sensitivity", trackballSensitivity, 0.05f, 1f);
        }

        static void Load()
        {
            m_TrackballSensitivity = EditorPrefs.GetFloat(Keys.trackballSensitivity, 0.2f);
            m_CurrentChannelMixer  = EditorPrefs.GetInt(Keys.currentChannelMixer, 0);
            m_CurrentCurve         = EditorPrefs.GetInt(Keys.currentCurve, 0);
            m_ShowLayerToolkit     = EditorPrefs.GetBool(Keys.showLayerToolkit, false);
            m_ShowCustomSorter     = EditorPrefs.GetBool(Keys.showCustomSorter, false);

            m_Loaded = true;
        }

        static void TrySave<T>(ref T field, T newValue, string key)
        {
            if (field.Equals(newValue))
                return;

            if (typeof(T) == typeof(float))
                EditorPrefs.SetFloat(key, (float)(object)newValue);
            else if (typeof(T) == typeof(int))
                EditorPrefs.SetInt(key, (int)(object)newValue);
            else if (typeof(T) == typeof(bool))
                EditorPrefs.SetBool(key, (bool)(object)newValue);
            else if (typeof(T) == typeof(string))
                EditorPrefs.SetString(key, (string)(object)newValue);

            field = newValue;
        }
    }
}
