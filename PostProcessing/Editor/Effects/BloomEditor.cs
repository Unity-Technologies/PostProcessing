using UnityEngine.Experimental.PostProcessing;

namespace UnityEditor.Experimental.PostProcessing
{
    [PostProcessEditor(typeof(Bloom))]
    public sealed class BloomEditor : PostProcessEffectEditor<Bloom>
    {
        SerializedParameterOverride m_Intensity;
        SerializedParameterOverride m_Threshold;
        SerializedParameterOverride m_SoftKnee;
        SerializedParameterOverride m_Diffusion;
        SerializedParameterOverride m_Color;
        SerializedParameterOverride m_MobileOptimized;
        
        SerializedParameterOverride m_LensTexture;
        SerializedParameterOverride m_LensIntensity;

        public override void OnEnable()
        {
            m_Intensity = FindParameterOverride(x => x.intensity);
            m_Threshold = FindParameterOverride(x => x.threshold);
            m_SoftKnee = FindParameterOverride(x => x.softKnee);
            m_Diffusion = FindParameterOverride(x => x.diffusion);
            m_Color = FindParameterOverride(x => x.color);
            m_MobileOptimized = FindParameterOverride(x => x.mobileOptimized);
            
            m_LensTexture = FindParameterOverride(x => x.lensTexture);
            m_LensIntensity = FindParameterOverride(x => x.lensIntensity);
        }

        public override void OnInspectorGUI()
        {
            EditorUtilities.DrawHeaderLabel("Bloom");
            
            PropertyField(m_Intensity);
            PropertyField(m_Threshold);
            PropertyField(m_SoftKnee);
            PropertyField(m_Diffusion);
            PropertyField(m_Color);
            PropertyField(m_MobileOptimized);
            
            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Lens Dirtiness");
                
            PropertyField(m_LensTexture);
            PropertyField(m_LensIntensity);
        }
    }
}
