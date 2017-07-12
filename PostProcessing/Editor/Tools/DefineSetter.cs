using System.Linq;

namespace UnityEditor.Rendering.PostProcessing
{
    [InitializeOnLoad]
    sealed class DefineSetter
    {
        const string k_Define = "UNITY_POST_PROCESSING_STACK_V2";
        
        static DefineSetter()
        {
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

            var list = defines.Split(';', ' ')
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            if (list.Contains(k_Define))
                return;

            list.Add(k_Define);
            defines = list.Aggregate((a, b) => a + ";" + b);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
        }
    }
}
