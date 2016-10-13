using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    using Settings = GrainModel.Settings;

    [PostProcessingModelEditor(typeof(GrainModel))]
    public class GrainModelEditor : PostProcessingModelEditor
    {
        SerializedProperty m_Colored;
        SerializedProperty m_Intensity;
        SerializedProperty m_WeightR;
        SerializedProperty m_WeightG;
        SerializedProperty m_WeightB;
        SerializedProperty m_Size;
        SerializedProperty m_LuminanceContribution;

        public override void OnEnable()
        {
            m_Colored = FindSetting((Settings x) => x.colored);
            m_Intensity = FindSetting((Settings x) => x.intensity);
            m_WeightR = FindSetting((Settings x) => x.weightR);
            m_WeightG = FindSetting((Settings x) => x.weightG);
            m_WeightB = FindSetting((Settings x) => x.weightB);
            m_Size = FindSetting((Settings x) => x.size);
            m_LuminanceContribution = FindSetting((Settings x) => x.luminanceContribution);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_Intensity);
            EditorGUILayout.PropertyField(m_LuminanceContribution);
            EditorGUILayout.PropertyField(m_Size);
            EditorGUILayout.PropertyField(m_Colored);

            if (m_Colored.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Channel Weights", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_WeightR, EditorGUIHelper.GetContent("Red"));
                EditorGUILayout.PropertyField(m_WeightG, EditorGUIHelper.GetContent("Green"));
                EditorGUILayout.PropertyField(m_WeightB, EditorGUIHelper.GetContent("Blue"));
                EditorGUI.indentLevel--;
            }
        }
    }
}
