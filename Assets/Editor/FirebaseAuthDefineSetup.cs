#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace KanjiMaster.EditorTools
{
    /// <summary>
    /// Editor-only: keeps the <c>FIREBASE_AUTH</c> scripting define in sync with whether the
    /// Firebase Auth assembly is actually present, so the auth layer uses the real Firebase
    /// backend when the SDK is installed and its compile-safe fallback when it is not — with
    /// no manual Player Settings editing. Firebase's managed DLLs are auto-referenced, so
    /// once the define is on, KanjiRush.Runtime compiles against them. Touches only the
    /// scripting-define symbol; no scenes/prefabs/gameplay.
    /// </summary>
    [InitializeOnLoad]
    public static class FirebaseAuthDefineSetup
    {
        private const string Define = "FIREBASE_AUTH";

        static FirebaseAuthDefineSetup() => Sync();

        private static bool FirebaseAuthInstalled()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { if (asm.GetType("Firebase.Auth.FirebaseAuth") != null) return true; }
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
            bool installed = FirebaseAuthInstalled();

            if (installed && !has) defines.Add(Define);
            else if (!installed && has) defines.Remove(Define);
            else return; // already in sync — avoid a needless recompile

            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
        }
    }
}
#endif
