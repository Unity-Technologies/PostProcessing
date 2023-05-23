using System;
using System.Linq;
#if UNITY_2021_3_OR_NEWER
using UnityEditor.Build;
#endif

namespace UnityEditor.Rendering.PostProcessing
{
    [InitializeOnLoad]
    sealed class DefineSetter
    {
        const string k_Define = "UNITY_POST_PROCESSING_STACK_V2";

        static DefineSetter()
        {
#if UNITY_2021_3_OR_NEWER
            var targets = Enum.GetValues(typeof(NamedBuildTarget))
                .Cast<NamedBuildTarget>()
                .Where(x => x != NamedBuildTarget.Unknown && !IsObsolete(x));
#else
            var targets = Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(x => x != BuildTargetGroup.Unknown)
                .Where(x => !IsObsolete(x));
#endif

            foreach (var target in targets)
            {
#if UNITY_2021_3_OR_NEWER
                var defines = PlayerSettings.GetScriptingDefineSymbols(target).Trim();
#else
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();
#endif

                var list = defines.Split(';', ' ')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                if (list.Contains(k_Define))
                    continue;

                list.Add(k_Define);
                defines = list.Aggregate((a, b) => a + ";" + b);

#if UNITY_2021_3_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(target, defines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
#endif
            }
        }

#if UNITY_2021_3_OR_NEWER
        static bool IsObsolete(NamedBuildTarget namedBuildTarget)
        {
            var attrs = typeof(NamedBuildTarget)
                .GetField(namedBuildTarget.ToString())
                .GetCustomAttributes(typeof(ObsoleteAttribute), false);

            return attrs != null && attrs.Length > 0;
        }
#else
        static bool IsObsolete(BuildTargetGroup group)
        {
            var attrs = typeof(BuildTargetGroup)
                .GetField(group.ToString())
                .GetCustomAttributes(typeof(ObsoleteAttribute), false);

            return attrs != null && attrs.Length > 0;
        }
#endif
    }
}