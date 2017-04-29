using System;

namespace UnityEngine.Experimental.PostProcessing
{
    [Serializable]
    public sealed class FastApproximateAntialiasing
    {
        [Tooltip("Boost performances by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms.")]
        public bool mobileOptimized = false;
    }
}
