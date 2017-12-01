using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [CanEditMultipleObjects, CustomEditor(typeof(PostProcessVolume))]
    public sealed class PostProcessVolumeEditor : BaseEditor<PostProcessVolume>
    {
        SerializedProperty m_Profile;

        SerializedProperty m_IsGlobal;
        SerializedProperty m_BlendRadius;
        SerializedProperty m_Weight;
        SerializedProperty m_Priority;

        EffectListEditor m_EffectList;

        void OnEnable()
        {
            m_Profile = FindProperty(x => x.sharedProfile);

            m_IsGlobal = FindProperty(x => x.isGlobal);
            m_BlendRadius = FindProperty(x => x.blendDistance);
            m_Weight = FindProperty(x => x.weight);
            m_Priority = FindProperty(x => x.priority);

            m_EffectList = new EffectListEditor(this);
            RefreshEffectListEditor(m_Target.sharedProfile);
        }

        void OnDisable()
        {
            if (m_EffectList != null)
                m_EffectList.Clear();
        }

        void RefreshEffectListEditor(PostProcessProfile asset)
        {
            m_EffectList.Clear();

            if (asset != null)
                m_EffectList.Init(asset, new SerializedObject(asset));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_IsGlobal);

            if (!m_IsGlobal.boolValue) // Blend radius is not needed for global volumes
                EditorGUILayout.PropertyField(m_BlendRadius);
            
            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_Priority);
            
            bool assetHasChanged = false;
            bool showCopy = m_Profile.objectReferenceValue != null;
            bool multiEdit = m_Profile.hasMultipleDifferentValues;

            // The layout system sort of break alignement when mixing inspector fields with custom
            // layouted fields, do the layout manually instead
            int buttonWidth = showCopy ? 45 : 60;
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * (showCopy ? 2 : 1), lineRect.height);
            var buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);
            var buttonCopyRect = new Rect(buttonNewRect.xMax, lineRect.y, buttonWidth, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, EditorUtilities.GetContent("Profile|A reference to a profile asset."));

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.BeginProperty(fieldRect, GUIContent.none, m_Profile);

                var profile = (PostProcessProfile)EditorGUI.ObjectField(fieldRect, m_Profile.objectReferenceValue, typeof(PostProcessProfile), false);

                if (scope.changed)
                {
                    assetHasChanged = true;
                    m_Profile.objectReferenceValue = profile;
                }

                EditorGUI.EndProperty();
            }

            using (new EditorGUI.DisabledScope(multiEdit))
            {
                if (GUI.Button(buttonNewRect, EditorUtilities.GetContent("New|Create a new profile."), showCopy ? EditorStyles.miniButtonLeft : EditorStyles.miniButton))
                {
                    // By default, try to put assets in a folder next to the currently active
                    // scene file. If the user isn't a scene, put them in root instead.
                    var targetName = m_Target.name;
                    var scene = m_Target.gameObject.scene;
                    var asset = ProfileFactory.CreatePostProcessProfile(scene, targetName);
                    m_Profile.objectReferenceValue = asset;
                    assetHasChanged = true;
                }

                if (showCopy && GUI.Button(buttonCopyRect, EditorUtilities.GetContent("Clone|Create a new profile and copy the content of the currently assigned profile."), EditorStyles.miniButtonRight))
                {
                    // Duplicate the currently assigned profile and save it as a new profile
                    var origin = (PostProcessProfile)m_Profile.objectReferenceValue;
                    var path = AssetDatabase.GetAssetPath(origin);
                    path = AssetDatabase.GenerateUniqueAssetPath(path);

                    var asset = Instantiate(origin);
                    asset.settings.Clear();
                    AssetDatabase.CreateAsset(asset, path);

                    foreach (var item in origin.settings)
                    {
                        var itemCopy = Instantiate(item);
                        itemCopy.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                        itemCopy.name = item.name;
                        asset.settings.Add(itemCopy);
                        AssetDatabase.AddObjectToAsset(itemCopy, asset);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    m_Profile.objectReferenceValue = asset;
                    assetHasChanged = true;
                }
            }

            EditorGUILayout.Space();

            if (m_Profile.objectReferenceValue == null)
            {
                if (assetHasChanged)
                    m_EffectList.Clear(); // Asset wasn't null before, do some cleanup

                EditorGUILayout.HelpBox("Assign a Post-process Profile to this volume using the \"Asset\" field or create one automatically by clicking the \"New\" button.\nAssets are automatically put in a folder next to your scene file. If you scene hasn't been saved yet they will be created at the root of the Assets folder.", MessageType.Info);
            }
            else
            {
                if (assetHasChanged)
                    RefreshEffectListEditor((PostProcessProfile)m_Profile.objectReferenceValue);

                if (!multiEdit)
                    m_EffectList.OnGUI();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
} 
