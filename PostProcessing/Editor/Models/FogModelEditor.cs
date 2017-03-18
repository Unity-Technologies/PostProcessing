using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    using Settings = FogModel.Settings;

    [PostProcessingModelEditor(typeof(FogModel), alwaysEnabled: true)]
    public class FogModelEditor : PostProcessingModelEditor
    {
        SerializedProperty m_ExcludeSkybox;
        SerializedProperty m_FadeToSkybox;

        public override void OnEnable()
        {
            m_ExcludeSkybox = FindSetting((Settings x) => x.excludeSkybox);
            m_FadeToSkybox = FindSetting((Settings x) => x.fadeToSkybox);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This effect adds fog compatibility to the deferred rendering path; actual fog settings should be set in the Lighting panel.", MessageType.Info);
            EditorGUILayout.PropertyField(m_ExcludeSkybox, EditorGUIHelper.GetContent("Exclude Skybox (deferred only)"));
            EditorGUILayout.PropertyField(m_FadeToSkybox, EditorGUIHelper.GetContent("Fade to Skybox"));
            EditorGUI.indentLevel--;
        }
    }
}
