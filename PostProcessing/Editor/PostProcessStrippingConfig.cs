using UnityEngine;
using UnityEditor.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public sealed class PostProcessStrippingConfig : ScriptableObject
    {
        public bool stripUnsupportedShaders = true;
        public bool stripDebugShaders = false;
        public bool stripComputeShaders = false;

        public void Awake()
        {
            PostProcessResourceStripper.Update();
        }

        public void OnValidate()
        {
            PostProcessResourceStripper.Update();
        }
    }
}
