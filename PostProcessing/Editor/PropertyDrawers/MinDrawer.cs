using UnityEngine;

namespace UnityEditor.PostProcessing
{
#if !UNITY_2018_3_OR_NEWER
    using MinAttribute = UnityEngine.PostProcessing.MinAttribute;
    [CustomPropertyDrawer(typeof(MinAttribute))]
#else
    using MinAttribute = UnityEngine.MinAttribute;
    [CustomPropertyDrawer(typeof(MinAttribute))]
#endif
    sealed class MinDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MinAttribute attribute = (MinAttribute)base.attribute;

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int v = EditorGUI.IntField(position, label, property.intValue);
                property.intValue = (int)Mathf.Max(v, attribute.min);
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                float v = EditorGUI.FloatField(position, label, property.floatValue);
                property.floatValue = Mathf.Max(v, attribute.min);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use Min with float or int.");
            }
        }
    }
}
