using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [CustomEditor(typeof(PostProcessDebug))]
    public sealed class PostProcessDebugEditor : BaseEditor<PostProcessDebug>
    {
        SerializedProperty m_PostProcessLayer;
        SerializedProperty m_LightMeterEnabled;
        SerializedProperty m_HistogramEnabled;
        SerializedProperty m_WaveformEnabled;
        SerializedProperty m_VectorscopeEnabled;

        SerializedObject m_Monitors;
        SerializedProperty m_LightMeterShowCurves;
        SerializedProperty m_HistogramChannel;
        SerializedProperty m_WaveformExposure;
        SerializedProperty m_VectorscopeExposure;

        void OnEnable()
        {
            m_PostProcessLayer = FindProperty(x => x.postProcessLayer);
            m_LightMeterEnabled = FindProperty(x => x.lightMeter);
            m_HistogramEnabled = FindProperty(x => x.histogram);
            m_WaveformEnabled = FindProperty(x => x.waveform);
            m_VectorscopeEnabled = FindProperty(x => x.vectorscope);

            if (m_PostProcessLayer.objectReferenceValue != null)
                RebuildProperties();
        }

        void RebuildProperties()
        {
            if (m_PostProcessLayer.objectReferenceValue == null)
                return;

            m_Monitors = new SerializedObject(m_Target.postProcessLayer);

            m_LightMeterShowCurves = m_Monitors.FindProperty("monitors.lightMeter.showCurves");
            m_HistogramChannel = m_Monitors.FindProperty("monitors.histogram.channel");
            m_WaveformExposure = m_Monitors.FindProperty("monitors.waveform.exposure");
            m_VectorscopeExposure = m_Monitors.FindProperty("monitors.vectorscope.exposure");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_PostProcessLayer);

                if (changed.changed)
                    RebuildProperties();
            }

            if (m_PostProcessLayer.objectReferenceValue != null)
            {
                m_Monitors.Update();

                DoMonitorGUI(EditorUtilities.GetContent("Light Meter"), m_LightMeterEnabled, m_LightMeterShowCurves);
                DoMonitorGUI(EditorUtilities.GetContent("Histogram"), m_HistogramEnabled, m_HistogramChannel);
                DoMonitorGUI(EditorUtilities.GetContent("Waveform"), m_WaveformEnabled, m_WaveformExposure);
                DoMonitorGUI(EditorUtilities.GetContent("Vectoscope"), m_VectorscopeEnabled, m_VectorscopeExposure);

                m_Monitors.ApplyModifiedProperties();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DoMonitorGUI(GUIContent content, SerializedProperty prop, params SerializedProperty[] settings)
        {
            EditorGUILayout.PropertyField(prop, content);

            if (settings == null || settings.Length == 0)
                return;

            if (prop.boolValue)
            {
                EditorGUI.indentLevel++;
                foreach (var p in settings)
                    EditorGUILayout.PropertyField(p);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }
    }
}
