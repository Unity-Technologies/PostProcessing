using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(AmbientOcclusion))]
    public sealed class AmbientOcclusionEditor : PostProcessEffectEditor<AmbientOcclusion>
    {
        SerializedParameterOverride m_Mode;
        SerializedParameterOverride m_Intensity;
        SerializedParameterOverride m_Color;
        SerializedParameterOverride m_AmbientOnly;
        SerializedParameterOverride m_ThicknessModifier;
        SerializedParameterOverride m_Quality;
        SerializedParameterOverride m_Radius;

        public override void OnEnable()
        {
            m_Mode = FindParameterOverride(x => x.mode);
            m_Intensity = FindParameterOverride(x => x.intensity);
            m_Color = FindParameterOverride(x => x.color);
            m_AmbientOnly = FindParameterOverride(x => x.ambientOnly);
            m_ThicknessModifier = FindParameterOverride(x => x.thicknessModifier);
            m_Quality = FindParameterOverride(x => x.quality);
            m_Radius = FindParameterOverride(x => x.radius);
        }

        public override void OnInspectorGUI()
        {
            if (RuntimeUtilities.scriptableRenderPipelineActive)
            {
                EditorGUILayout.HelpBox("This effect doesn't work with scriptable render pipelines yet.", MessageType.Warning);
                return;
            }

            PropertyField(m_Mode);
            PropertyField(m_Intensity);

            int aoMode = m_Mode.value.intValue;

            if (aoMode == (int)AmbientOcclusionMode.ScalableAmbientObscurance)
            {
                PropertyField(m_Radius);
                PropertyField(m_Quality);
            }
            else if (aoMode == (int)AmbientOcclusionMode.MultiScaleVolumetricObscurance)
            {
                if (!SystemInfo.supportsComputeShaders)
                    EditorGUILayout.HelpBox("Multi-scale volumetric obscurance requires compute shader support.", MessageType.Warning);

                PropertyField(m_ThicknessModifier);
            }
            
            PropertyField(m_Color);

            if (Camera.main != null && Camera.main.actualRenderingPath == RenderingPath.DeferredShading && Camera.main.allowHDR)
                PropertyField(m_AmbientOnly);
        }
    }
}
