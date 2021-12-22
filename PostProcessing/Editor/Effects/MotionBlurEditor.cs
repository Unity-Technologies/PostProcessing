using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(MotionBlur))]
    internal sealed class MotionBlurEditor : DefaultPostProcessEffectEditor
    {
        public override void OnInspectorGUI()
        {
            if (EditorUtilities.isVREnabled)
                EditorGUILayout.HelpBox("Motion Blur is available only for non-stereo cameras.", MessageType.Warning);

            base.OnInspectorGUI();
        }
    }
}
