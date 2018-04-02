using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(Fog))]
    public sealed class FogEditor : PostProcessEffectEditor<Fog>
    {
        SerializedParameterOverride m_excludeSkybox;
        SerializedParameterOverride m_color;
        SerializedParameterOverride m_density;
        SerializedParameterOverride m_startDistance;
        SerializedParameterOverride m_endDistance;
        SerializedParameterOverride m_mode;

        public override void OnEnable()
        {
            m_excludeSkybox = FindParameterOverride(x => x.excludeSkybox);
            m_color = FindParameterOverride(x => x.color);
            m_density = FindParameterOverride(x => x.density);
            m_startDistance = FindParameterOverride(x => x.startDistance);
            m_endDistance = FindParameterOverride(x => x.endDistance);
            m_mode = FindParameterOverride(x => x.mode);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PropertyField(m_excludeSkybox);
            PropertyField(m_color);
            PropertyField(m_mode);
            if (m_mode.value.intValue == (int)FogMode.Linear)
            {
                PropertyField(m_startDistance);
                PropertyField(m_endDistance);
            }
            else if (m_mode.value.intValue == (int)FogMode.Exponential || m_mode.value.intValue == (int)FogMode.ExponentialSquared)
            {
                PropertyField(m_density);
            }
        }
    }
}
