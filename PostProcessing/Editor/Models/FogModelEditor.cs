using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    using Settings = FogModel.Settings;

    [PostProcessingModelEditor(typeof(FogModel), alwaysEnabled: true)]
    public class FogModelEditor : PostProcessingModelEditor
    {
        SerializedProperty m_SkyboxBehaviour;

        public override void OnEnable()
        {
            m_SkyboxBehaviour = FindSetting((Settings x) => x.skyboxBehaviour);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This effect adds fog compatibility to the deferred rendering path; actual fog settings should be set in the Lighting panel.", MessageType.Info);
            EditorGUILayout.PropertyField(m_SkyboxBehaviour, EditorGUIHelper.GetContent("Skybox Behaviour"));
            EditorGUI.indentLevel--;
        }
    }
}
