using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.PostProcessing
{
    using VolumeManager = PostProcessVolumeManager;
    
    // TODO: Deal with unsupported collider types for editor/sceneview previz
    // TODO: Do outer skin previz for colliders (need mesh manipulation stuff)
    [ExecuteInEditMode]
    public sealed class PostProcessVolume : MonoBehaviour
    {
        [Tooltip("A global volume is applied to the whole scene.")]
        public bool isGlobal = false;
        
        [Min(0f), Tooltip("Outer distance to start blending from. A value of 0 means no blending and the volume overrides will be applied immediatly upon entry.")]
        public float blendDistance = 0f;
        
        [Tooltip("Volume priority in the stack. Higher number means higher priority. Negative values are supported.")]
        public float priority = 0f;

        public List<PostProcessEffectSettings> settings = new List<PostProcessEffectSettings>();

        // Editor only, doesn't have any use outside of it
        [NonSerialized]
        public bool isDirty;

        int m_PreviousLayer;
        float m_PreviousPriority;
        List<Collider> m_TempColliders;

        void OnEnable()
        {
            VolumeManager.instance.Register(this);
            m_PreviousLayer = gameObject.layer;
            m_TempColliders = new List<Collider>();
        }

        void OnDisable()
        {
            VolumeManager.instance.Unregister(this);
        }

        void Reset()
        {
            isDirty = true;
        }

        void Update()
        {
            // Unfortunately we need to track the current layer to update the volume manager in
            // real-time as the user could change it at any time in the editor or at runtime.
            // Because no event is raised when the layer changes, we have to track it on every
            // frame :/
            // TODO: Talk to the scripting team about dispatching an event if layer is changed
            int layer = gameObject.layer;
            if (layer != m_PreviousLayer)
            {
                VolumeManager.instance.UpdateVolumeLayer(this, m_PreviousLayer, layer);
                m_PreviousLayer = layer;
            }

            // Same for `priority`. We could use a property instead, but it doesn't play nice with
            // the serialization system. Using a custom Attribute/PropertyDrawer for a property is
            // possible but it doesn't work with Undo/Redo in the editor, which makes it useless.
            if (priority != m_PreviousPriority)
            {
                VolumeManager.instance.SetLayerDirty(layer);
                m_PreviousPriority = priority;
            }
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

            var effect = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
            effect.enabled.overrideState = true;
            effect.enabled.value = true;
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

        void OnDrawGizmos()
        {
            var colliders = m_TempColliders;
            GetComponents(colliders);
            if (isGlobal || colliders == null)
                return;

            Gizmos.color = new Color(0.2f, 0.8f, 0.1f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

            // Draw a separate gizmo for each collider
            foreach (var collider in colliders)
            {
                if (!collider.enabled)
                    continue;

                var type = collider.GetType();

                if (type == typeof(BoxCollider))
                {
                    var c = (BoxCollider)collider;
                    Gizmos.DrawCube(c.center, c.size);
                    Gizmos.DrawWireCube(c.center, c.size + new Vector3(blendDistance, blendDistance, blendDistance));
                }
            }

            colliders.Clear();
        }
    }
}
