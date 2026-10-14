#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace KanjiMaster.EditorTools
{
    /// <summary>
    /// Editor-only: keeps the <c>LEANTWEEN_PRESENT</c> scripting define in sync with
    /// whether the LeanTween assembly is actually present. So <c>UITween</c> uses the real
    /// LeanTween backend when LeanTween is installed and the no-motion fallback when it is
    /// not — without manual Player Settings edits, and without ever *silently* falling back
    /// while LeanTween is installed. Runs on domain load and when the active build target
    /// changes. Touches nothing but the scripting-define symbol; no scenes/prefabs/gameplay.
    /// </summary>
    [InitializeOnLoad]
    public static class LeanTweenDefineSetup
    {
        private const string Define = "LEANTWEEN_PRESENT";

        static LeanTweenDefineSetup() => Sync();

        private static bool LeanTweenInstalled()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { if (asm.GetType("LeanTween") != null) return true; }
                catch { /* dynamic/reflection-only assemblies — ignore */ }
            }
            return false;
        }

        private static void Sync()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown) return;
            var target = NamedBuildTarget.FromBuildTargetGroup(group);

            var defines = PlayerSettings.GetScriptingDefineSymbols(target)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            bool has = defines.Contains(Define);
            bool installed = LeanTweenInstalled();

            if (installed && !has) defines.Add(Define);
            else if (!installed && has) defines.Remove(Define);
            else return; // already in sync — avoid a needless recompile

            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
        }
    }
}
#endif
