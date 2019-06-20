using UnityEngine.Rendering.PPSMobile;

namespace UnityEditor.Rendering.PPSMobile
{
    [CustomEditor(typeof(PostProcessProfile))]
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
            if (m_EffectList != null)
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
