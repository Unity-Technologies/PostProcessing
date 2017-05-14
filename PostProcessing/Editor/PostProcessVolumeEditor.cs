using System.IO;
using UnityEngine;
using UnityEngine.Experimental.PostProcessing;
using UnityEngine.SceneManagement;

namespace UnityEditor.Experimental.PostProcessing
{
    [CustomEditor(typeof(PostProcessVolume))]
    public sealed class PostProcessVolumeEditor : BaseEditor<PostProcessVolume>
    {
        SerializedProperty m_Profile;

        SerializedProperty m_IsGlobal;
        SerializedProperty m_BlendRadius;
        SerializedProperty m_Priority;

        EffectListEditor m_EffectList;

        void OnEnable()
        {
            m_Profile = FindProperty(x => x.sharedProfile);

            m_IsGlobal = FindProperty(x => x.isGlobal);
            m_BlendRadius = FindProperty(x => x.blendDistance);
            m_Priority = FindProperty(x => x.priority);

            m_EffectList = new EffectListEditor(this);
            RefreshEffectListEditor(m_Target.sharedProfile);
        }

        void OnDisable()
        {
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

            EditorGUILayout.PropertyField(m_Priority);
            
            bool assetHasChanged = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(EditorUtilities.GetContent("Profile|A reference to a profile asset."));

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        m_Profile.objectReferenceValue = (PostProcessProfile)EditorGUILayout.ObjectField(m_Profile.objectReferenceValue, typeof(PostProcessProfile), false);
                        assetHasChanged = scope.changed;
                    }

                    if (GUILayout.Button(EditorUtilities.GetContent("New|Create a new profile."), EditorStyles.miniButton))
                    {
                        // By default, try to put assets in a folder next to the currently active
                        // scene file. If the user isn't a scene, put them in root instead.
                        var targetName = m_Target.name;
                        var scene = SceneManager.GetActiveScene();
                        var path = string.Empty;

                        if (string.IsNullOrEmpty(scene.path))
                        {
                            path = "Assets/";
                        }
                        else
                        {
                            var scenePath = Path.GetDirectoryName(scene.path);
                            var extPath = scene.name + "_Profiles";
                            var profilePath = scenePath + "/" + extPath;

                            if (!AssetDatabase.IsValidFolder(profilePath))
                                AssetDatabase.CreateFolder(scenePath, extPath);

                            path = profilePath + "/";
                        }

                        path += targetName + " Profile.asset";
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                        
                        var asset = CreateInstance<PostProcessProfile>();
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        m_Profile.objectReferenceValue = asset;
                        assetHasChanged = true;
                    }
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

                m_EffectList.OnGUI();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
} 
