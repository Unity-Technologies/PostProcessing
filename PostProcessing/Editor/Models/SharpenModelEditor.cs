using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
	using Settings = SharpenModel.Settings;

	[PostProcessingModelEditor(typeof(SharpenModel))]
	public class SharpenModelEditor : PostProcessingModelEditor
	{
		SerializedProperty m_Intensity;
		SerializedProperty m_Size;
		SerializedProperty m_Downsample;
		SerializedProperty m_Iterations;
		SerializedProperty m_Type;

		public override void OnEnable()
		{
			m_Intensity = FindSetting((Settings x) => x.intensity);
			m_Size = FindSetting((Settings x) => x.size);
			m_Downsample = FindSetting((Settings x) => x.downsample);
			m_Iterations = FindSetting((Settings x) => x.iterations);
			m_Type = FindSetting((Settings x) => x.type);
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.PropertyField(m_Intensity);
			EditorGUILayout.PropertyField(m_Size);
			EditorGUILayout.PropertyField(m_Downsample);
			EditorGUILayout.PropertyField(m_Iterations);
			EditorGUILayout.PropertyField(m_Type);
		}
	}
}