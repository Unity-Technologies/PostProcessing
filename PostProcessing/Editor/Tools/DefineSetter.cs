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
            var targets = Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(x => x != BuildTargetGroup.Unknown)
                .Where(x => !IsObsolete(x));

            foreach (var target in targets)
            {
#if UNITY_2021_3_OR_NEWER
                var namedTarget = NamedBuildTarget.FromBuildTargetGroup(target);
                var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget).Trim();
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
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
#endif
            }
        }

        static bool IsObsolete(BuildTargetGroup group)
        {
            var attrs = typeof(BuildTargetGroup)
                .GetField(group.ToString())
                .GetCustomAttributes(typeof(ObsoleteAttribute), false);

            return attrs != null && attrs.Length > 0;
        }
    }
}