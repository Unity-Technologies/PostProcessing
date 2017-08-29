using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [CanEditMultipleObjects, CustomEditor(typeof(PostProcessProfile))]
    sealed class PostProcessProfileEditor : Editor
    {
        EffectListEditor m_EffectList;

        void OnEnable()
        {
            m_EffectList = new EffectListEditor(this);
            m_EffectList.Init(target as PostProcessProfile, serializedObject);
        }

        void OnDisable()
        {
            m_EffectList.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_EffectList.OnGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
