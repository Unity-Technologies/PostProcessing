using UnityEngine;
using UnityEngine.Experimental.PostProcessing;

namespace UnityEditor.Experimental.PostProcessing
{
    // TODO: Add button to select all volumes on the same layer
    // TODO: Add button to open the volume manager
    [CustomEditor(typeof(PostProcessLayer))]
    public sealed class PostProcessLayerEditor : BaseEditor<PostProcessLayer>
    {
        SerializedProperty m_VolumeTrigger;
        SerializedProperty m_VolumeLayer;
        SerializedProperty m_ShowDebugUI;

        SerializedProperty m_AntialiasingMode;
        SerializedProperty m_TaaJitterSpread;
        SerializedProperty m_TaaSharpen;
        SerializedProperty m_TaaStationaryBlending;
        SerializedProperty m_TaaMotionBlending;

        SerializedProperty m_DebugMonitor;

        static string[] s_AntialiasingMethodNames =
        {
            "No Anti-aliasing",
            "Fast Approximate Anti-aliasing",
            "Temporal Anti-aliasing"
        };

        void OnEnable()
        {
            m_VolumeTrigger = FindProperty(x => x.volumeTrigger);
            m_VolumeLayer = FindProperty(x => x.volumeLayer);
            m_ShowDebugUI = FindProperty(x => x.showDebugUI);

            m_AntialiasingMode = FindProperty(x => x.antialiasingMode);
            m_TaaJitterSpread = FindProperty(x => x.temporalAntialiasing.jitterSpread);
            m_TaaSharpen = FindProperty(x => x.temporalAntialiasing.sharpen);
            m_TaaStationaryBlending = FindProperty(x => x.temporalAntialiasing.stationaryBlending);
            m_TaaMotionBlending = FindProperty(x => x.temporalAntialiasing.motionBlending);

            m_DebugMonitor = FindProperty(x => x.debugView.monitor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(EditorUtilities.GetContent("Volume blending"), EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Trigger");
                    EditorGUI.indentLevel--; // The editor adds an indentation after the prefix label, this removes it

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        m_VolumeTrigger.objectReferenceValue = (Transform)EditorGUILayout.ObjectField(m_VolumeTrigger.objectReferenceValue, typeof(Transform), false);
                        if (GUILayout.Button(EditorUtilities.GetContent("This|Assigns the current GameObject as a trigger."), EditorStyles.miniButton))
                            m_VolumeTrigger.objectReferenceValue = m_Target.transform;
                    }
                    
                    EditorGUI.indentLevel++;
                }

                EditorGUILayout.PropertyField(m_VolumeLayer, EditorUtilities.GetContent("Layer"));

                int mask = m_VolumeLayer.intValue;
                if (mask == 0)
                    EditorGUILayout.HelpBox("No layer has been set, the trigger will never be affected by volumes.", MessageType.Info);
                else if (mask == -1 || ((mask & 1) != 0))
                    EditorGUILayout.HelpBox("Do not use \"Everything\" or \"Default\" as a layer mask as it will slow down the volume blending process! Put post-processing volumes in their own dedicated layer for best performances.", MessageType.Warning);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(EditorUtilities.GetContent("Anti-aliasing"), EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                m_AntialiasingMode.intValue = EditorGUILayout.Popup("Mode", m_AntialiasingMode.intValue, s_AntialiasingMethodNames);

                if (m_AntialiasingMode.intValue == (int)PostProcessLayer.Antialiasing.TemporalAntialiasing)
                {
                    EditorGUILayout.PropertyField(m_TaaJitterSpread);
                    EditorGUILayout.PropertyField(m_TaaStationaryBlending);
                    EditorGUILayout.PropertyField(m_TaaMotionBlending);
                    EditorGUILayout.PropertyField(m_TaaSharpen);

                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_ShowDebugUI);

            if (m_ShowDebugUI.boolValue)
            {
                EditorGUI.indentLevel++;
                {
                    EditorGUILayout.PropertyField(m_DebugMonitor);
                    EditorGUILayout.HelpBox("The debug UI only works on compute-shader enabled platforms.", MessageType.Info);
                    EditorGUILayout.HelpBox("Currently non-implemented.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
