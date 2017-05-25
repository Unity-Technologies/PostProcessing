using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.PostProcessing;
using UnityEditorInternal;

namespace UnityEditor.Experimental.PostProcessing
{
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

        Dictionary<PostProcessEvent, ReorderableList> m_CustomLists;

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

            // Create a reorderable list for each injection event
            m_CustomLists = new Dictionary<PostProcessEvent, ReorderableList>();
            foreach (var evt in Enum.GetValues(typeof(PostProcessEvent)).Cast<PostProcessEvent>())
            {
                var bundles = m_Target.sortedBundles[evt];
                var listName = ObjectNames.NicifyVariableName(evt.ToString());

                var list = new ReorderableList(bundles, typeof(PostProcessBundle), true, true, false, false);
                list.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, listName);
                list.drawElementCallback = (rect, index, isActive, isFocused) => EditorGUI.LabelField(rect, ((PostProcessBundle)list.list[index]).attribute.menuItem);
                list.onReorderCallback = (l) => InternalEditorUtility.RepaintAllViews();

                m_CustomLists.Add(evt, list);
            }
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

            EditorGUILayout.Space();

            // Toolkit
            EditorUtilities.DrawSplitter();
            GlobalSettings.showLayerToolkit = EditorUtilities.DrawHeader("Toolkit", GlobalSettings.showLayerToolkit);

            if (GlobalSettings.showLayerToolkit)
            {
                EditorGUILayout.Space();

                using (new EditorGUI.DisabledScope(true))
                if (GUILayout.Button(EditorUtilities.GetContent("Export frame to EXR..."), EditorStyles.miniButton))
                {
                    // TODO: Export frame to EXR with options to disable postfx, stop postfx just before colorgrading, or output with postfx applied
                }

                if (GUILayout.Button(EditorUtilities.GetContent("Select all layer volumes|Selects all the volumes that will influence this layer."), EditorStyles.miniButton))
                {
                    var volumes = RuntimeUtilities.GetAllSceneObjects<PostProcessVolume>()
                        .Where(x => (m_VolumeLayer.intValue & (1 << x.gameObject.layer)) != 0)
                        .Select(x => x.gameObject)
                        .Cast<UnityEngine.Object>()
                        .ToArray();

                    if (volumes.Count() > 0)
                        Selection.objects = volumes;
                }

                using (new EditorGUI.DisabledScope(true))
                if (GUILayout.Button(EditorUtilities.GetContent("Open Volume Explorer"), EditorStyles.miniButton))
                {
                    // TODO: Volume explorer
                }

                EditorGUILayout.Space();
            }

            // Custom user effects sorter
            EditorUtilities.DrawSplitter();
            GlobalSettings.showCustomSorter = EditorUtilities.DrawHeader("Custom Effect Sorting", GlobalSettings.showCustomSorter);

            if (GlobalSettings.showCustomSorter)
            {
                EditorGUILayout.Space();

                bool anyList = false;
                foreach (var kvp in m_CustomLists)
                {
                    var list = kvp.Value;

                    // Skip empty lists to avoid polluting the inspector
                    if (list.count == 0)
                        continue;

                    list.DoLayoutList();
                    anyList = true;
                }

                if (!anyList)
                {
                    EditorGUILayout.HelpBox("No custom effect loaded.", MessageType.Info);
                    EditorGUILayout.Space();
                }
            }

            EditorUtilities.DrawSplitter();
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
