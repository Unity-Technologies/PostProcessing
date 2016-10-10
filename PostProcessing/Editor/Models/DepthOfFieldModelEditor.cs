using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    using Settings = DepthOfFieldModel.Settings;

    [PostProcessingModelEditor(typeof(DepthOfFieldModel))]
    public class DepthOfFieldModelEditor : PostProcessingModelEditor
    {
        public override void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Work in progress.", MessageType.Warning);
        }
    }
}
