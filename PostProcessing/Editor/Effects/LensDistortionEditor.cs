using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(LensDistortion))]
    internal sealed class LensDistortionEditor : DefaultPostProcessEffectEditor
    {
        public override void OnInspectorGUI()
        {
            if (EditorUtilities.isVREnabled)
                EditorGUILayout.HelpBox("Lens Distortion is available only for non-stereo cameras.", MessageType.Warning);

            base.OnInspectorGUI();
        }
    }
}
