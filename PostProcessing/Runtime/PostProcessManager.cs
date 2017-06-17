using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.PostProcessing
{
    // Singleton used to tracks all existing volumes in the scene
    // TODO: Deal with 2D volumes !
    public sealed class PostProcessManager
    {
        static PostProcessManager s_Instance;

        public static PostProcessManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PostProcessManager();

                return s_Instance;
            }
        }

        const int k_MaxLayerCount = 32; // Max amount of layers available in Unity
        readonly List<PostProcessVolume>[] m_Volumes;
        readonly bool[] m_SortNeeded;
        readonly List<PostProcessEffectSettings> m_BaseSettings;
        readonly List<Collider> m_TempColliders;

        public readonly Dictionary<Type, PostProcessAttribute> settingsTypes;

        PostProcessManager()
        {
            m_Volumes = new List<PostProcessVolume>[k_MaxLayerCount];
            m_SortNeeded = new bool[k_MaxLayerCount];
            m_BaseSettings = new List<PostProcessEffectSettings>();
            m_TempColliders = new List<Collider>(5);

            settingsTypes = new Dictionary<Type, PostProcessAttribute>();
            ReloadBaseTypes();
        }

#if UNITY_EDITOR
        // Called every time Unity recompile scripts in the editor. We need this to keep track of
        // any new custom effect the user might add to the project
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnEditorReload()
        {
            instance.ReloadBaseTypes();
        }
#endif

        void CleanBaseTypes()
        {
            settingsTypes.Clear();

            foreach (var settings in m_BaseSettings)
                RuntimeUtilities.Destroy(settings);

            m_BaseSettings.Clear();
        }

        // This will be called only once at runtime and everytime script reload kicks-in in the
        // editor as we need to keep track of any compatible post-processing effects in the project
        void ReloadBaseTypes()
        {
            CleanBaseTypes();

            // Rebuild the base type map
            var types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(
                                a => a.GetTypes()
                                .Where(
                                    t => t.IsSubclassOf(typeof(PostProcessEffectSettings))
                                    && t.IsDefined(typeof(PostProcessAttribute), false)
                                )
                            );

            foreach (var type in types)
            {
                settingsTypes.Add(type, type.GetAttribute<PostProcessAttribute>());

                // Create an instance for each effect type, these will be used for the lowest
                // priority global volume as we need a default state when exiting volume ranges
                var inst = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
                inst.SetDisabledState();
                inst.SetAllOverridesTo(true, false);
                m_BaseSettings.Add(inst);
            }
        }

        // Gets a list of all volumes currently affecting the given layer. Results aren't sorted.
        // Volume with weight of 0 or no profile set will be skipped. Results list won't be cleared.
        public void GetActiveVolumes(PostProcessLayer layer, List<PostProcessVolume> results)
        {
            // If no trigger is set, only global volumes will have influence
            int mask = layer.volumeLayer.value;
            var volumeTrigger = layer.volumeTrigger;
            bool onlyGlobal = volumeTrigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : volumeTrigger.position;

            for (int i = 0; i < k_MaxLayerCount; i++)
            {
                // Skip layers not in the mask
                if ((mask & (1 << i)) == 0)
                    continue;

                // Skip empty layers
                var volumes = m_Volumes[i];

                if (volumes == null)
                    continue;

                // Traverse all volumes
                foreach (var volume in volumes)
                {
                    // Skip disabled volumes and volumes without any data or weight
                    if (!volume.enabled || volume.profileRef == null || volume.weight <= 0f)
                        continue;

                    // Global volume always have influence
                    if (volume.isGlobal)
                    {
                        results.Add(volume);
                        continue;
                    }

                    if (onlyGlobal)
                        continue;

                    // If volume isn't global and has no collider, skip it as it's useless
                    var colliders = m_TempColliders;
                    volume.GetComponents(colliders);
                    if (colliders.Count == 0)
                        continue;

                    // Find closest distance to volume, 0 means it's inside it
                    float closestDistanceSqr = float.PositiveInfinity;

                    foreach (var collider in colliders)
                    {
                        if (!collider.enabled)
                            continue;

                        var closestPoint = collider.ClosestPoint(triggerPos); // 5.6-only API
                        var d = ((closestPoint - triggerPos) / 2f).sqrMagnitude;

                        if (d < closestDistanceSqr)
                            closestDistanceSqr = d;
                    }

                    colliders.Clear();
                    float blendDistSqr = volume.blendDistance * volume.blendDistance;

                    // Check for influence
                    if (closestDistanceSqr <= blendDistSqr)
                        results.Add(volume);
                }
            }
        }

        public PostProcessVolume GetHighestPriorityVolume(PostProcessLayer layer)
        {
            if (layer == null)
                throw new ArgumentNullException("layer");

            return GetHighestPriorityVolume(layer.volumeLayer);
        }

        public PostProcessVolume GetHighestPriorityVolume(LayerMask mask)
        {
            float highestPriority = float.NegativeInfinity;
            PostProcessVolume output = null;

            for (int i = 0; i < k_MaxLayerCount; i++)
            {
                // Skip layers not in the mask
                if ((mask & (1 << i)) == 0)
                    continue;

                // Skip empty layers
                var volumes = m_Volumes[i];

                if (volumes == null)
                    continue;

                foreach (var volume in volumes)
                {
                    if (volume.priority > highestPriority)
                    {
                        highestPriority = volume.priority;
                        output = volume;
                    }
                }
            }

            return output;
        }

        public PostProcessVolume QuickVolume(int layer, float priority, params PostProcessEffectSettings[] settings)
        {
            var gameObject = new GameObject()
            {
                name = "Quick Volume",
                layer = layer,
                hideFlags = HideFlags.HideAndDontSave
            };

            var volume = gameObject.AddComponent<PostProcessVolume>();
            volume.priority = priority;
            volume.isGlobal = true;
            var profile = volume.profile;

            foreach (var s in settings)
            {
                Assert.IsNotNull(s, "Trying to create a volume with null effects");
                profile.AddSettings(s);
            }

            return volume;
        }

        internal void SetLayerDirty(int layer)
        {
            Assert.IsTrue(layer >= 0 && layer <= k_MaxLayerCount, "Invalid layer bit");
            m_SortNeeded[layer] = true;
        }

        internal void UpdateVolumeLayer(PostProcessVolume volume, int prevLayer, int newLayer)
        {
            Assert.IsTrue(prevLayer >= 0 && prevLayer <= k_MaxLayerCount, "Invalid layer bit");
            Unregister(volume, prevLayer);
            Register(volume, newLayer);
        }

        void Register(PostProcessVolume volume, int layer)
        {
            var volumes = m_Volumes[layer];

            if (volumes == null)
            {
                volumes = new List<PostProcessVolume>();
                m_Volumes[layer] = volumes;
            }

            Assert.IsFalse(volumes.Contains(volume), "Volume has already been registered");
            volumes.Add(volume);
            SetLayerDirty(layer);
        }

        internal void Register(PostProcessVolume volume)
        {
            int layer = volume.gameObject.layer;
            Register(volume, layer);
        }

        void Unregister(PostProcessVolume volume, int layer)
        {
            var volumes = m_Volumes[layer];

            if (volumes == null)
                return;

            Assert.IsTrue(volumes.Contains(volume), "Trying to unregister a non-registered volume");
            volumes.Remove(volume);
        }

        internal void Unregister(PostProcessVolume volume)
        {
            int layer = volume.gameObject.layer;
            Unregister(volume, layer);
        }

        internal void UpdateSettings(PostProcessLayer postProcessLayer)
        {
            // Reset to base state
            postProcessLayer.OverrideSettings(m_BaseSettings, 1f);

            // If no trigger is set, only global volumes will have influence
            int mask = postProcessLayer.volumeLayer.value;
            var volumeTrigger = postProcessLayer.volumeTrigger;
            bool onlyGlobal = volumeTrigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : volumeTrigger.position;

            for (int i = 0; i < k_MaxLayerCount; i++)
            {
                // Skip layers not in the mask
                if ((mask & (1 << i)) == 0)
                    continue;

                // Skip empty layers
                var volumes = m_Volumes[i];

                if (volumes == null)
                    continue;

                // Sort the volume list if needed
                if (m_SortNeeded[i])
                {
                    SortByPriority(volumes);
                    m_SortNeeded[i] = false;
                }

                // Traverse all volumes
                foreach (var volume in volumes)
                {
                    // Skip disabled volumes and volumes without any data or weight
                    if (!volume.enabled || volume.profileRef == null || volume.weight <= 0f)
                        continue;

                    var settings = volume.profileRef.settings;

                    // Global volume always have influence
                    if (volume.isGlobal)
                    {
                        postProcessLayer.OverrideSettings(settings, volume.weight);
                        continue;
                    }

                    if (onlyGlobal)
                        continue;

                    // If volume isn't global and has no collider, skip it as it's useless
                    var colliders = m_TempColliders;
                    volume.GetComponents(colliders);
                    if (colliders.Count == 0)
                        continue;

                    // Find closest distance to volume, 0 means it's inside it
                    float closestDistanceSqr = float.PositiveInfinity;

                    foreach (var collider in colliders)
                    {
                        if (!collider.enabled)
                            continue;

                        var closestPoint = collider.ClosestPoint(triggerPos); // 5.6-only API
                        var d = ((closestPoint - triggerPos) / 2f).sqrMagnitude;

                        if (d < closestDistanceSqr)
                            closestDistanceSqr = d;
                    }

                    colliders.Clear();
                    float blendDistSqr = volume.blendDistance * volume.blendDistance;

                    // Volume has no influence, ignore it
                    // Note: Volume doesn't do anything when `closestDistanceSqr = blendDistSqr` but
                    //       we can't use a >= comparison as blendDistSqr could be set to 0 in which
                    //       case volume would have total influence
                    if (closestDistanceSqr > blendDistSqr)
                        continue;

                    // Volume has influence
                    float interpFactor = 1f;

                    if (blendDistSqr > 0f)
                        interpFactor = 1f - (closestDistanceSqr / blendDistSqr);

                    // No need to clamp01 the interpolation factor as it'll always be in [0;1[ range
                    postProcessLayer.OverrideSettings(settings, interpFactor * volume.weight);
                }
            }
        }

        // Custom insertion sort. First sort will be slower but after that it'll be faster than
        // using List<T>.Sort() which is also unstable by nature.
        // Sort order is ascending.
        static void SortByPriority(List<PostProcessVolume> volumes)
        {
            Assert.IsNotNull(volumes, "Trying to sort volumes of non-initialized layer");

            for (int i = 1; i < volumes.Count; i++)
            {
                var temp = volumes[i];
                int j = i - 1;

                while (j >= 0 && volumes[j].priority > temp.priority)
                {
                    volumes[j + 1] = volumes[j];
                    j--;
                }

                volumes[j + 1] = temp;
            }
        }
    }
}
