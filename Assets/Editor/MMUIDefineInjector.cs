using UnityEditor;
using UnityEditor.Build;

namespace Aiara.EditorTools
{
    [InitializeOnLoad]
    public static class MMUIDefineInjector
    {
        private static readonly string[] RequiredDefines = new[] { "MM_UI", "TMP_PRESENT" };

        static MMUIDefineInjector()
        {
            EnsureDefineSymbols();
        }

        private static void EnsureDefineSymbols()
        {
            BuildTargetGroup activeGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (activeGroup == BuildTargetGroup.Unknown)
            {
                return;
            }

            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(activeGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            bool changed = false;
            foreach (string symbol in RequiredDefines)
            {
                if (!defines.Contains(symbol))
                {
                    defines = string.IsNullOrEmpty(defines) ? symbol : defines + ";" + symbol;
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
            }
        }
    }
}
