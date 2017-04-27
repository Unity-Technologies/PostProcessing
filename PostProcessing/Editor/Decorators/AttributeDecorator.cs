using System;
using UnityEngine;

namespace UnityEditor.Experimental.PostProcessing
{
    public abstract class AttributeDecorator
    {
        public abstract bool OnGUI(SerializedProperty property, bool overrideState, GUIContent title, Attribute attribute);
    }
}
