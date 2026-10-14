#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KanjiMaster.EditorTools
{
    /// <summary>
    /// Editor-only dev utilities for the local save. Lets you wipe the save so the next
    /// Play starts at Onboarding, or reveal the save folder. Not compiled into builds
    /// (Editor folder + UNITY_EDITOR guard). Touches no gameplay code.
    /// </summary>
    public static class SaveResetMenu
    {
        // Matches LocalPlayerProgressStore.DefaultFileName. Kept as a literal so this
        // Editor tool has no assembly-reference dependency on runtime code.
        private const string FileName = "player_progress.json";

        [MenuItem("Tools/KanjiMaster/Reset Save (Onboarding)")]
        public static void ResetSave()
        {
            string dir = Application.persistentDataPath;
            string basePath = Path.Combine(dir, FileName);

            // The live save + its transient sidecars. The user's manual ".backup" copy is
            // intentionally NOT deleted.
            string[] targets =
            {
                basePath,
                basePath + ".tmp",
                basePath + ".bak",
                basePath + ".corrupt",
            };

            bool anyExists = false;
            foreach (var p in targets) if (File.Exists(p)) { anyExists = true; break; }

            if (!anyExists)
            {
                Debug.Log($"[KanjiMaster] No save found in {dir}. Next Play already starts at Onboarding.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Reset Save?",
                    "This deletes the local save (player_progress.json and its .tmp/.bak/.corrupt " +
                    "sidecars) so the next Play starts at Onboarding.\n\n" +
                    "Your manual '.backup' file, if any, is NOT touched.\n\nContinue?",
                    "Reset", "Cancel"))
                return;

            int removed = 0;
            foreach (var p in targets)
                if (File.Exists(p)) { File.Delete(p); removed++; }

            Debug.Log($"[KanjiMaster] Reset save: removed {removed} file(s) from {dir}. " +
                      "Stop Play mode (if running) — the next Play starts at Onboarding.");
        }

        [MenuItem("Tools/KanjiMaster/Reveal Save Folder")]
        public static void RevealSaveFolder()
        {
            EditorUtility.RevealInFinder(Path.Combine(Application.persistentDataPath, FileName));
        }
    }
}
#endif
