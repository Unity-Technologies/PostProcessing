using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(TiltShift))]
    internal sealed class TiltShiftEditor : PostProcessEffectEditor<TiltShift>
    {
        SerializedParameterOverride m_Mode;
        SerializedParameterOverride m_Quality;
        SerializedParameterOverride m_BlurArea;
        SerializedParameterOverride m_MaxBlurSize;
        SerializedParameterOverride m_DownSample;
        
        public override void OnEnable()
        {
            m_Mode = FindParameterOverride(x => x.mode);
            m_Quality = FindParameterOverride(x => x.quality);
            m_BlurArea = FindParameterOverride(x => x.blurArea);
            m_MaxBlurSize = FindParameterOverride(x => x.maxBlurSize);
            m_DownSample = FindParameterOverride(x => x.downsample);
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);
            PropertyField(m_Quality);
            PropertyField(m_BlurArea);
            PropertyField(m_MaxBlurSize);
            PropertyField(m_DownSample);
        }
    }
}

