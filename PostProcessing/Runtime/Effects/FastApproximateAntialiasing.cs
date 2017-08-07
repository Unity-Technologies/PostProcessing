using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class FastApproximateAntialiasing
    {
        [Tooltip("Boost performances by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms.")]
        public bool mobileOptimized = false;

        [Tooltip("Keep alpha channel. This will slightly lower the effect quality but allows rendering against a transparent background.")]
        public bool keepAlpha = false;
    }
}
