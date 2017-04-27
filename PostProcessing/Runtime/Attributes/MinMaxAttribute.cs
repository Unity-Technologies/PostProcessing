using System;

namespace UnityEngine.Experimental.PostProcessing
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MinMaxAttribute : Attribute
    {
        public readonly float min;
        public readonly float max;

        public MinMaxAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
