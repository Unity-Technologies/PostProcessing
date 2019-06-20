using UnityEngine.Rendering.PPSMobile;

namespace UnityEditor.Rendering.PPSMobile
{
    [PostProcessEditor(typeof(LensDistortion))]
    internal sealed class LensDistortionEditor : DefaultPostProcessEffectEditor
    {
        public override void OnInspectorGUI()
        {
            if (RuntimeUtilities.isVREnabled)
                EditorGUILayout.HelpBox("Lens Distortion is automatically disabled when VR is enabled.", MessageType.Warning);

            base.OnInspectorGUI();
        }
    }
}
