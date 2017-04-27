using System;

namespace UnityEditor.Experimental.PostProcessing
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DecoratorAttribute : Attribute
    {
        public readonly Type attributeType;

        public DecoratorAttribute(Type attributeType)
        {
            this.attributeType = attributeType;
        }
    }
}
