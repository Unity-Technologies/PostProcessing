using UnityEngine;
using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    using Settings = FogModel.Settings;

    [PostProcessingModelEditor(typeof(FogModel))]
    public class FogModelEditor : PostProcessingModelEditor
    {
        SerializedProperty m_Color;
        SerializedProperty m_Mode;
        SerializedProperty m_Density;
        SerializedProperty m_Start;
        SerializedProperty m_End;

        public override void OnEnable()
        {
            m_Color = FindSetting((Settings x) => x.color);
            m_Mode = FindSetting((Settings x) => x.mode);
            m_Density = FindSetting((Settings x) => x.density);
            m_Start = FindSetting((Settings x) => x.start);
            m_End = FindSetting((Settings x) => x.end);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_Color);
            EditorGUILayout.PropertyField(m_Mode);

            EditorGUI.indentLevel++;
            if (m_Mode.intValue == (int)FogMode.Linear)
            {
                EditorGUILayout.PropertyField(m_Start);
                EditorGUILayout.PropertyField(m_End);
            }
            else
            {
                EditorGUILayout.PropertyField(m_Density);
            }
            EditorGUI.indentLevel--;
        }
    }
}
