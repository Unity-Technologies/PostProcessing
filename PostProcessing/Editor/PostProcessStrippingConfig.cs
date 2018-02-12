using System;
using UnityEngine;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public sealed class PostProcessStrippingConfig : ScriptableObject
    {
        public bool stripUnsupportedShaders = true;
        public bool stripDebugShaders = false;
        public bool stripComputeShaders = false;

        [Serializable]
        public sealed class Result
        {
            public PostProcessResources.Shaders shaders;
            public PostProcessResources.ComputeShaders computeShaders;
        }

        public Result result;

        private PostProcessResources strippedResources;

        public void Awake()
        {
            PostProcessResourceStripper.StripAll(this);
            UpdateResult();
        }

        public void OnValidate()
        {
            PostProcessResourceStripper.StripAll(this);
            UpdateResult();
        }

        private void UpdateResult()
        {
            if (result == null)
                result = new Result();

            PostProcessResources resources = PostProcessResourcesFactory.StrippedDefaultResources();
            result.shaders = resources.shaders;
            result.computeShaders = resources.computeShaders;
            DestroyImmediate(resources);
        }
    }

}
