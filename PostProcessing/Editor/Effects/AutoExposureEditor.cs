using UnityEngine.Experimental.PostProcessing;

namespace UnityEditor.Experimental.PostProcessing
{
    [PostProcessEditor(typeof(AutoExposure))]
    public sealed class AutoExposureEditor : PostProcessEffectEditor<AutoExposure>
    {
        SerializedParameterOverride m_Filtering;
        
        SerializedParameterOverride m_MinLuminance;
        SerializedParameterOverride m_MaxLuminance;
        SerializedParameterOverride m_KeyValue;

        SerializedParameterOverride m_EyeAdaptation;
        SerializedParameterOverride m_SpeedUp;
        SerializedParameterOverride m_SpeedDown;

        public override void OnEnable()
        {
            m_Filtering = FindParameterOverride(x => x.filtering);
            
            m_MinLuminance = FindParameterOverride(x => x.minLuminance);
            m_MaxLuminance = FindParameterOverride(x => x.maxLuminance);
            m_KeyValue = FindParameterOverride(x => x.keyValue);
            
            m_EyeAdaptation = FindParameterOverride(x => x.eyeAdaptation);
            m_SpeedUp = FindParameterOverride(x => x.speedUp);
            m_SpeedDown = FindParameterOverride(x => x.speedDown);
        }

        public override void OnInspectorGUI()
        {
            EditorUtilities.DrawHeaderLabel("Exposure");

            PropertyField(m_Filtering);

            PropertyField(m_MinLuminance);
            PropertyField(m_MaxLuminance);
            PropertyField(m_KeyValue);
            
            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Adaptation");

            PropertyField(m_EyeAdaptation);

            if (m_EyeAdaptation.value.intValue == (int)EyeAdaptation.Progressive)
            {
                PropertyField(m_SpeedUp);
                PropertyField(m_SpeedDown);
            }
        }
    }
}
