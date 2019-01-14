using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(GlobalFog))]
    internal sealed class GlobalFogEditor : PostProcessEffectEditor<GlobalFog>
    {
        SerializedParameterOverride m_distanceFog;
        SerializedParameterOverride m_excludeFarPixels;
        SerializedParameterOverride m_useRadialDistance;
        SerializedParameterOverride m_heightFog;
        SerializedParameterOverride m_height;
        SerializedParameterOverride m_heightDensity;
        SerializedParameterOverride m_startDistance;

        public override void OnEnable()
        {
            m_distanceFog = FindParameterOverride(x => x.distanceFog);
            m_excludeFarPixels = FindParameterOverride(x => x.excludeFarPixels);
            m_useRadialDistance = FindParameterOverride(x => x.useRadialDistance);
            m_heightFog = FindParameterOverride(x => x.heightFog);
            m_height = FindParameterOverride(x => x.height);
            m_heightDensity = FindParameterOverride(x => x.heightDensity);
            m_startDistance = FindParameterOverride(x => x.startDistance);
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_distanceFog);
            PropertyField(m_excludeFarPixels);
            PropertyField(m_useRadialDistance);
            PropertyField(m_heightFog);
            PropertyField(m_height);
            PropertyField(m_heightDensity);
            PropertyField(m_startDistance);
        }
    }
}