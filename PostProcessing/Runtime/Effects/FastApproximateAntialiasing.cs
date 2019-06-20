using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.PPSMobile
{
    /// <summary>
    /// This class holds settings for the Fast Approximate Anti-aliasing (FXAA) effect.
    /// </summary>
#if UNITY_2017_1_OR_NEWER
    [UnityEngine.Scripting.Preserve]
#endif
    [Serializable]
    public sealed class FastApproximateAntialiasing
    {
        /// <summary>
        /// Set this to <c>true</c> if you need to keep the alpha channel untouched. Else it will
        /// use this channel to store internal data used to speed up and improve visual quality.
        /// </summary>
        [Tooltip("Keep alpha channel. This will slightly lower the effect quality but allows rendering against a transparent background.")]
        public bool keepAlpha = false;
    }
}
