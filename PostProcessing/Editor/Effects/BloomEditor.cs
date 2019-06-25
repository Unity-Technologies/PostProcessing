using UnityEngine.Rendering.PPSMobile;

namespace UnityEditor.Rendering.PPSMobile
{
    [PostProcessEditor(typeof(Bloom))]
    internal sealed class BloomEditor : PostProcessEffectEditor<Bloom>
    {
        SerializedParameterOverride m_Intensity;
        SerializedParameterOverride m_Threshold;
        SerializedParameterOverride m_SoftKnee;
        SerializedParameterOverride m_Clamp;
        SerializedParameterOverride m_Diffusion;
        SerializedParameterOverride m_AnamorphicRatio;
        SerializedParameterOverride m_Color;

        public override void OnEnable()
        {
            m_Intensity = FindParameterOverride(x => x.intensity);
            m_Threshold = FindParameterOverride(x => x.threshold);
            m_SoftKnee = FindParameterOverride(x => x.softKnee);
            m_Clamp = FindParameterOverride(x => x.clamp);
            m_Diffusion = FindParameterOverride(x => x.diffusion);
            m_AnamorphicRatio = FindParameterOverride(x => x.anamorphicRatio);
            m_Color = FindParameterOverride(x => x.color);
        }

        public override void OnInspectorGUI()
        {
            EditorUtilities.DrawHeaderLabel("Bloom");

            PropertyField(m_Intensity);
            PropertyField(m_Threshold);
            PropertyField(m_SoftKnee);
            PropertyField(m_Clamp);
            PropertyField(m_Diffusion);
            PropertyField(m_AnamorphicRatio);
            PropertyField(m_Color);
        }
    }
}
