using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(Bloom))]
    public sealed class BloomEditor : PostProcessEffectEditor<Bloom>
    {
        SerializedParameterOverride m_Intensity;
        SerializedParameterOverride m_Threshold;
        SerializedParameterOverride m_SoftKnee;
        SerializedParameterOverride m_Diffusion;
        SerializedParameterOverride m_AnamorphicRatio;
        SerializedParameterOverride m_Color;
        SerializedParameterOverride m_MobileOptimized;

        SerializedParameterOverride m_DirtTexture;
        SerializedParameterOverride m_DirtIntensity;

        public override void OnEnable()
        {
            m_Intensity = FindParameterOverride(x => x.intensity);
            m_Threshold = FindParameterOverride(x => x.threshold);
            m_SoftKnee = FindParameterOverride(x => x.softKnee);
            m_Diffusion = FindParameterOverride(x => x.diffusion);
            m_AnamorphicRatio = FindParameterOverride(x => x.anamorphicRatio);
            m_Color = FindParameterOverride(x => x.color);
            m_MobileOptimized = FindParameterOverride(x => x.mobileOptimized);
            
            m_DirtTexture = FindParameterOverride(x => x.dirtTexture);
            m_DirtIntensity = FindParameterOverride(x => x.dirtIntensity);
        }

        public override void OnInspectorGUI()
        {
            EditorUtilities.DrawHeaderLabel("Bloom");

            PropertyField(m_Intensity);
            PropertyField(m_Threshold);
            PropertyField(m_SoftKnee);
            PropertyField(m_Diffusion);
            PropertyField(m_AnamorphicRatio);
            PropertyField(m_Color);
            PropertyField(m_MobileOptimized);

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Dirtiness");

            PropertyField(m_DirtTexture);
            PropertyField(m_DirtIntensity);

            if (RuntimeUtilities.isVREnabled)
            {
                if ((m_DirtIntensity.overrideState.boolValue && m_DirtIntensity.value.floatValue > 0f)
                 || (m_DirtTexture.overrideState.boolValue && m_DirtTexture.value.objectReferenceValue != null))
                    EditorGUILayout.HelpBox("Using a dirt texture in VR is not recommended.", MessageType.Warning);
            }
        }
    }
}
