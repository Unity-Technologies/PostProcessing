using UnityEngine;
using UnityEngine.Experimental.PostProcessing;

namespace UnityEditor.Experimental.PostProcessing
{
    [PostProcessEditor(typeof(Bloom))]
    public sealed class BloomEditor : PostProcessEffectEditor<Bloom>
    {
        SerializedParameterOverride m_Intensity;
        SerializedParameterOverride m_Threshold;
        SerializedParameterOverride m_SoftKnee;
        SerializedParameterOverride m_ResponseCurve;
        
        SerializedParameterOverride m_LensTexture;
        SerializedParameterOverride m_LensIntensity;

        public override void OnEnable()
        {
            m_Intensity = FindParameterOverride(x => x.intensity);
            m_Threshold = FindParameterOverride(x => x.threshold);
            m_SoftKnee = FindParameterOverride(x => x.softKnee);
            m_ResponseCurve = FindParameterOverride(x => x.responseCurve);
            
            m_LensTexture = FindParameterOverride(x => x.lensTexture);
            m_LensIntensity = FindParameterOverride(x => x.lensIntensity);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Bloom", EditorStyles.miniLabel);
            
            PropertyField(m_Intensity);
            PropertyField(m_Threshold);
            PropertyField(m_SoftKnee);
            PropertyField(m_ResponseCurve);

            EditorGUILayout.LabelField("Lens Dirtiness", EditorStyles.miniLabel);
                
            PropertyField(m_LensTexture);
            PropertyField(m_LensIntensity);
        }
    }
}
