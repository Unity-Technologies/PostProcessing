using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    public sealed class PostProcessResourcesFactory : ScriptableObject
    {
        static PostProcessResourcesFactory s_Instance;
        static private StripResources strip;

        [SerializeField] private PostProcessResources unstrippedResources;
        List<WeakReference> trackedResources = new List<WeakReference>();

        public delegate void StripResources(PostProcessResources resources);

        public static void Init(StripResources stripHandler)
        {
            strip = stripHandler;
        }

        public static PostProcessResources[] AllResources()
        {
            EnsureInstance();
            return s_Instance.AllResourcesImpl();
        }

        public static PostProcessResources StrippedDefaultResources()
        {
            EnsureInstance();
            return Stripped(s_Instance.unstrippedResources);
        }

        public static PostProcessResources Stripped(PostProcessResources res)
        {
            EnsureInstance();
            return s_Instance.StrippedImpl(res);
        }

        private PostProcessResources StrippedImpl(PostProcessResources res)
        {
            PurgeTrackedResources();

            PostProcessResources result = null;

            if (res == unstrippedResources)
                result = unstrippedResources.StrippableClone();
            else
                result = res;

            Track(result);

            if (strip != null)
                strip(result);

            return result;
        }

        public PostProcessResources[] AllResourcesImpl()
        {
            PurgeTrackedResources();
            PostProcessResources[] activeResources = new PostProcessResources[trackedResources.Count];
            for (int i = 0; i < trackedResources.Count; ++i)
                activeResources[i] = (PostProcessResources) trackedResources[i].Target;

            return activeResources;
        }

        private static void EnsureInstance()
        {
            if (s_Instance == null)
               s_Instance = CreateInstance<PostProcessResourcesFactory>();
        }

        private void PurgeTrackedResources()
        {
            trackedResources.RemoveAll(r => !r.IsAlive);
        }

        private void Track(PostProcessResources res)
        {
            foreach(var weakRef in trackedResources)
            {
                PostProcessResources trackedResource = (PostProcessResources) weakRef.Target;
                if (trackedResource == res)
                    return;
            }
            trackedResources.Add(new WeakReference(res));
        }
    }
}
