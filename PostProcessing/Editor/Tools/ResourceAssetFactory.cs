using UnityEngine;
using UnityEngine.Rendering.PPSMobile;

namespace UnityEditor.Rendering.PPSMobile
{
    static class ResourceAssetFactory
    {
#if PPSM_DEBUG_MENUS
        [MenuItem("Tools/Post-processing/Create Resources Asset")]
#endif
        static void CreateAsset()
        {
            var asset = ScriptableObject.CreateInstance<PostProcessResources>();
            AssetDatabase.CreateAsset(asset, "Assets/PostProcessResources.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
