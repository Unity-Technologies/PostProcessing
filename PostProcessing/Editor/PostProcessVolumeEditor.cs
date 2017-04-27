using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.PostProcessing;

namespace UnityEditor.Experimental.PostProcessing
{
    [CustomEditor(typeof(PostProcessVolume))]
    public sealed class PostProcessVolumeEditor : BaseEditor<PostProcessVolume>
    {
        SerializedProperty m_IsGlobal;
        SerializedProperty m_BlendRadius;
        SerializedProperty m_Priority;
        SerializedProperty m_Settings;

        Dictionary<Type, Type> m_EditorTypes; // SettingsType => EditorType
        List<PostProcessEffectBaseEditor> m_Editors;

        void OnEnable()
        {
            m_IsGlobal = FindProperty(x => x.isGlobal);
            m_BlendRadius = FindProperty(x => x.blendDistance);
            m_Priority = FindProperty(x => x.priority);
            m_Settings = FindProperty(x => x.settings);

            m_EditorTypes = new Dictionary<Type, Type>();
            m_Editors = new List<PostProcessEffectBaseEditor>();

            // Gets the list of all available postfx editors
            var editorTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(
                    a => a.GetTypes()
                    .Where(
                        t => t.IsSubclassOf(typeof(PostProcessEffectBaseEditor))
                          && t.IsDefined(typeof(PostProcessEditorAttribute), false)
                    )
                ).ToList();

            // Map them to their corresponding settings type
            foreach (var editorType in editorTypes)
            {
                var attribute = editorType.GetAttribute<PostProcessEditorAttribute>();
                m_EditorTypes.Add(attribute.settingsType, editorType);
            }

            // Create editors for existing settings
            for (int i = 0; i < m_Target.settings.Count; i++)
                CreateEditor(m_Target.settings[i], m_Settings.GetArrayElementAtIndex(i));

            // Keep track of undo/redo to redraw the inspector when that happens
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            foreach (var editor in m_Editors)
                editor.OnDisable();

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            // Seems like there's an issue with the inspector not repainting after some undo events
            // This will take care of that
            Repaint();
        }

        void CreateEditor(PostProcessEffectSettings settings, SerializedProperty property, int index = -1)
        {
            var settingsType = settings.GetType();
            Type editorType;

            if (!m_EditorTypes.TryGetValue(settingsType, out editorType))
                editorType = typeof(DefaultPostProcessEffectEditor);

            var editor = (PostProcessEffectBaseEditor)Activator.CreateInstance(editorType);
            editor.Init(settings);
            editor.baseProperty = property.Copy();

            if (index < 0)
                m_Editors.Add(editor);
            else
                m_Editors[index] = editor;
        }

        // Clears & recreate all editors - mainly used when the volume has been modified outside of
        // the editor (user scripts, inspector reset etc).
        void RefreshEditors()
        {
            // Disable all editors first
            foreach (var editor in m_Editors)
                editor.OnDisable();

            // Remove them
            m_Editors.Clear();

            // Recreate editors for existing settings, if any
            for (int i = 0; i < m_Target.settings.Count; i++)
                CreateEditor(m_Target.settings[i], m_Settings.GetArrayElementAtIndex(i));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_Target.isDirty)
            {
                RefreshEditors();
                m_Target.isDirty = false;
            }

            EditorGUILayout.PropertyField(m_IsGlobal);

            if (!m_IsGlobal.boolValue) // Blend radius is not needed for global volumes
                EditorGUILayout.PropertyField(m_BlendRadius);

            EditorGUILayout.PropertyField(m_Priority);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(EditorUtilities.GetContent("Overrides"), EditorStyles.boldLabel);

                if (GUILayout.Button("Add effect...", EditorStyles.miniButton))
                {
                    var menu = new GenericMenu();

                    var typeMap = PostProcessVolumeManager.instance.settingsTypes;
                    foreach (var kvp in typeMap)
                    {
                        var type = kvp.Key;
                        var title = EditorUtilities.GetContent(kvp.Value.menuItem);
                        bool exists = m_Target.HasSettings(type);

                        if (!exists)
                            menu.AddItem(title, false, () => AddEffectOverride(type));
                        else
                            menu.AddDisabledItem(title);
                    }

                    menu.ShowAsContext();
                }
            }

            // Override list
            for (int i = 0; i < m_Editors.Count; i++)
            {
                var editor = m_Editors[i];
                string title = editor.GetDisplayTitle();
                int id = i; // Needed for closure capture below

                EditorUtilities.DrawSplitter();
                bool displayContent = EditorUtilities.DrawHeader(
                    title,
                    editor.baseProperty,
                    editor.activeProperty,
                    () => ResetEffectOverride(editor.target.GetType(), id),
                    () => RemoveEffectOverride(id)
                );

                if (displayContent)
                {
                    using (new EditorGUI.DisabledScope(!editor.activeProperty.boolValue))
                        editor.OnInternalInspectorGUI();
                }
            }

            if (m_Editors.Count > 0)
            {
                EditorUtilities.DrawSplitter();
                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // TODO: Undo support
        void AddEffectOverride(Type type)
        {
            serializedObject.Update();

            var effect = (PostProcessEffectSettings)CreateInstance(type);
            effect.enabled.overrideState = true;
            effect.enabled.value = true;

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_Settings.arraySize++;
            var effectProp = m_Settings.GetArrayElementAtIndex(m_Settings.arraySize - 1);
            effectProp.objectReferenceValue = effect;

            // Create & store the internal editor object for this effect
            CreateEditor(effect, effectProp);

            serializedObject.ApplyModifiedProperties();
        }

        void RemoveEffectOverride(int id)
        {
            // Huh. Hack to keep foldout state on the next element...
            bool nextFoldoutState = false;
            if (id < m_Editors.Count - 1)
                nextFoldoutState = m_Editors[id + 1].baseProperty.isExpanded;

            // Remove from the cached editors list
            m_Editors[id].OnDisable();
            m_Editors.RemoveAt(id);

            serializedObject.Update();

            // Destroy the object itself first so it doesn't leak
            var property = m_Settings.GetArrayElementAtIndex(id);
            var settings = property.objectReferenceValue;
            RuntimeUtilities.Destroy(settings);

            // Unassign it (should be null already but serialization does funky things
            property.objectReferenceValue = null;

            // ... and remove the array index itself from the list
            m_Settings.DeleteArrayElementAtIndex(id);

            // Finally refresh editor reference to the serialized settings list
            for (int i = 0; i < m_Editors.Count; i++)
                m_Editors[i].baseProperty = m_Settings.GetArrayElementAtIndex(i).Copy();

            if (id < m_Editors.Count)
                m_Editors[id].baseProperty.isExpanded = nextFoldoutState;
            
            serializedObject.ApplyModifiedProperties();
        }

        // Reset is done by deleting and removing the object from the list and adding a new one in
        // the place as it was before
        void ResetEffectOverride(Type type, int id)
        {
            // Remove from the cached editors list
            m_Editors[id].OnDisable();
            m_Editors[id] = null;

            serializedObject.Update();

            // Destroy the object itself first so it doesn't leak
            var property = m_Settings.GetArrayElementAtIndex(id);
            var settings = property.objectReferenceValue;
            RuntimeUtilities.Destroy(settings);

            // Unassign it but down remove it from the array to keep the index available
            property.objectReferenceValue = null;

            // Create a new object
            var effect = (PostProcessEffectSettings)CreateInstance(type);
            effect.enabled.overrideState = true;
            effect.enabled.value = true;

            // Put it in the reserved space
            property.objectReferenceValue = effect;

            // Create & store the internal editor object for this effect
            CreateEditor(effect, property, id);

            serializedObject.ApplyModifiedProperties();
        }
    }
} 
