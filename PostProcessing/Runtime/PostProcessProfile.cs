using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.PostProcessing
{
    public sealed class PostProcessProfile : ScriptableObject
    {
        [Tooltip("A list of all settings & overrides.")]
        public List<PostProcessEffectSettings> settings = new List<PostProcessEffectSettings>();

        // Editor only, doesn't have any use outside of it
        [NonSerialized]
        public bool isDirty = true;

        public void Reset()
        {
            isDirty = true;
        }

        public T AddSettings<T>()
            where T : PostProcessEffectSettings
        {
            return (T)AddSettings(typeof(T));
        }

        public PostProcessEffectSettings AddSettings(Type type)
        {
            if (HasSettings(type))
                throw new InvalidOperationException("Effect already exists in the stack");

            var effect = (PostProcessEffectSettings)CreateInstance(type);
            effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            effect.name = type.Name;
            effect.enabled.overrideState = true;
            effect.enabled.value = true;
            settings.Add(effect);
            isDirty = true;
            return effect;
        }

        public PostProcessEffectSettings AddSettings(PostProcessEffectSettings effect)
        {
            if (HasSettings(settings.GetType()))
                throw new InvalidOperationException("Effect already exists in the stack");

            settings.Add(effect);
            isDirty = true;
            return effect;
        }

        public void RemoveSettings<T>()
            where T : PostProcessEffectSettings
        {
            RemoveSettings(typeof(T));
        }

        public void RemoveSettings(Type type)
        {
            int toRemove = -1;

            for (int i = 0; i < settings.Count; i++)
            {
                if (settings.GetType() == type)
                {
                    toRemove = i;
                    break;
                }
            }

            if (toRemove < 0)
                throw new InvalidOperationException("Effect doesn't exist in the stack");

            settings.RemoveAt(toRemove);
            isDirty = true;
        }

        public bool HasSettings<T>()
            where T : PostProcessEffectSettings
        {
            return HasSettings(typeof(T));
        }

        public bool HasSettings(Type type)
        {
            foreach (var setting in settings)
            {
                if (setting.GetType() == type)
                    return true;
            }

            return false;
        }

        public bool HasSettings<T>(out T outSetting)
            where T : PostProcessEffectSettings
        {
            var type = typeof(T);
            outSetting = null;

            foreach (var setting in settings)
            {
                if (setting.GetType() == type)
                {
                    outSetting = (T)setting;
                    return true;
                }
            }

            return false;
        }
    }
}
