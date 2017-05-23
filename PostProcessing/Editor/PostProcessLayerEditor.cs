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

        SerializedProperty m_AntialiasingMode;
        SerializedProperty m_TaaJitterSpread;
        SerializedProperty m_TaaSharpen;
        SerializedProperty m_TaaStationaryBlending;
        SerializedProperty m_TaaMotionBlending;
        SerializedProperty m_FxaaMobileOptimized;

        SerializedProperty m_DebugDisplay;
        SerializedProperty m_DebugMonitor;

        static GUIContent[] s_AntialiasingMethodNames =
        {
            new GUIContent("No Anti-aliasing"),
            new GUIContent("Fast Approximate Anti-aliasing"),
            new GUIContent("Temporal Anti-aliasing")
        };

        void OnEnable()
        {
            m_VolumeTrigger = FindProperty(x => x.volumeTrigger);
            m_VolumeLayer = FindProperty(x => x.volumeLayer);

            m_AntialiasingMode = FindProperty(x => x.antialiasingMode);
            m_TaaJitterSpread = FindProperty(x => x.temporalAntialiasing.jitterSpread);
            m_TaaSharpen = FindProperty(x => x.temporalAntialiasing.sharpen);
            m_TaaStationaryBlending = FindProperty(x => x.temporalAntialiasing.stationaryBlending);
            m_TaaMotionBlending = FindProperty(x => x.temporalAntialiasing.motionBlending);
            m_FxaaMobileOptimized = FindProperty(x => x.fastApproximateAntialiasing.mobileOptimized);

            m_DebugDisplay = FindProperty(x => x.debugView.display);
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
                    EditorGUILayout.PrefixLabel(EditorUtilities.GetContent("Trigger|A transform that will act as a trigger for volume blending."));
                    EditorGUI.indentLevel--; // The editor adds an indentation after the prefix label, this removes it

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        m_VolumeTrigger.objectReferenceValue = (Transform)EditorGUILayout.ObjectField(m_VolumeTrigger.objectReferenceValue, typeof(Transform), false);
                        if (GUILayout.Button(EditorUtilities.GetContent("This|Assigns the current GameObject as a trigger."), EditorStyles.miniButton))
                            m_VolumeTrigger.objectReferenceValue = m_Target.transform;
                    }

                    EditorGUI.indentLevel++;
                }

                if (m_VolumeTrigger.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("No trigger has been set, the camera will only be affected by global volumes.", MessageType.Info);

                EditorGUILayout.PropertyField(m_VolumeLayer, EditorUtilities.GetContent("Layer|This camera will only be affected by volumes in the selected scene-layers."));

                int mask = m_VolumeLayer.intValue;
                if (mask == 0)
                    EditorGUILayout.HelpBox("No layer has been set, the trigger will never be affected by volumes.", MessageType.Warning);
                else if (mask == -1 || ((mask & 1) != 0))
                    EditorGUILayout.HelpBox("Do not use \"Everything\" or \"Default\" as a layer mask as it will slow down the volume blending process! Put post-processing volumes in their own dedicated layer for best performances.", MessageType.Warning);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(EditorUtilities.GetContent("Anti-aliasing"), EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                m_AntialiasingMode.intValue = EditorGUILayout.Popup(EditorUtilities.GetContent("Mode|The anti-aliasing method to use. FXAA is fast but low quality. TAA is a bit slower but higher quality."), m_AntialiasingMode.intValue, s_AntialiasingMethodNames);

                if (m_AntialiasingMode.intValue == (int)PostProcessLayer.Antialiasing.TemporalAntialiasing)
                {
                    EditorGUILayout.PropertyField(m_TaaJitterSpread);
                    EditorGUILayout.PropertyField(m_TaaStationaryBlending);
                    EditorGUILayout.PropertyField(m_TaaMotionBlending);
                    EditorGUILayout.PropertyField(m_TaaSharpen);
                }
                else if (m_AntialiasingMode.intValue == (int)PostProcessLayer.Antialiasing.FastApproximateAntialiasing)
                {
                    EditorGUILayout.PropertyField(m_FxaaMobileOptimized);
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(EditorUtilities.GetContent("Debug Layer"), EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_DebugDisplay, EditorUtilities.GetContent("Display|Toggle visibility of the debug layer on & off in the Game View."));

                if (m_DebugDisplay.boolValue)
                {
                    EditorGUILayout.PropertyField(m_DebugMonitor, EditorUtilities.GetContent("Monitor|The real-time monitor to display on the debug layer."));
                    EditorGUILayout.HelpBox("The debug layer only works on compute-shader enabled platforms.", MessageType.Info);
                    EditorGUILayout.HelpBox("Not implemented.", MessageType.Error);
                }
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
