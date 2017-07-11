using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [CustomEditor(typeof(PostProcessDebug))]
    public sealed class PostProcessDebugEditor : BaseEditor<PostProcessDebug>
    {
        SerializedProperty m_PostProcessLayer;

        SerializedObject m_Monitors;
        SerializedProperty m_LightMeterEnabled;
        SerializedProperty m_LightMeterShowCurves;
        SerializedProperty m_HistogramEnabled;
        SerializedProperty m_HistogramChannel;
        SerializedProperty m_WaveformEnabled;
        SerializedProperty m_WaveformExposure;
        SerializedProperty m_VectorscopeEnabled;
        SerializedProperty m_VectorscopeExposure;

        void OnEnable()
        {
            m_PostProcessLayer = FindProperty(x => x.postProcessLayer);

            if (m_PostProcessLayer.objectReferenceValue != null)
                RebuildProperties();
        }

        void RebuildProperties()
        {
            if (m_PostProcessLayer.objectReferenceValue == null)
                return;

            m_Monitors = new SerializedObject(m_Target.postProcessLayer);

            m_LightMeterEnabled = m_Monitors.FindProperty("monitors.lightMeter.enabled");
            m_LightMeterShowCurves = m_Monitors.FindProperty("monitors.lightMeter.showCurves");

            m_HistogramEnabled = m_Monitors.FindProperty("monitors.histogram.enabled");
            m_HistogramChannel = m_Monitors.FindProperty("monitors.histogram.channel");

            m_WaveformEnabled = m_Monitors.FindProperty("monitors.waveform.enabled");
            m_WaveformExposure = m_Monitors.FindProperty("monitors.waveform.exposure");

            m_VectorscopeEnabled = m_Monitors.FindProperty("monitors.vectorscope.enabled");
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

            serializedObject.ApplyModifiedProperties();

            if (m_PostProcessLayer.objectReferenceValue == null)
                return;

            EditorGUILayout.Space();

            m_Monitors.Update();
            
            DoMonitorGUI(EditorUtilities.GetContent("Light Meter"), m_LightMeterEnabled, m_LightMeterShowCurves);
            DoMonitorGUI(EditorUtilities.GetContent("Histogram"), m_HistogramEnabled, m_HistogramChannel);
            DoMonitorGUI(EditorUtilities.GetContent("Waveform"), m_WaveformEnabled, m_WaveformExposure);
            DoMonitorGUI(EditorUtilities.GetContent("Vectoscope"), m_VectorscopeEnabled, m_VectorscopeExposure);

            m_Monitors.ApplyModifiedProperties();
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
