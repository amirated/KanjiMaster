using System.IO;
using UnityEngine;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Local JSON persistence for <see cref="PlayerProgress"/> — survives scene
    /// changes and app restarts. No backend, no auth. Stored under
    /// Application.persistentDataPath so it is device-local and easy to extend
    /// (add fields to PlayerProgress and they serialize automatically).
    /// </summary>
    public static class ProgressStore
    {
        public const string DefaultFileName = "player_progress.json";

        public static string DefaultPath =>
            Path.Combine(Application.persistentDataPath, DefaultFileName);

        public static void Save(PlayerProgress progress, string path = null)
        {
            path ??= DefaultPath;
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(progress, true));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"ProgressStore: save failed — {e.Message}");
            }
        }

        public static PlayerProgress Load(string path = null)
        {
            path ??= DefaultPath;
            try
            {
                if (File.Exists(path))
                {
                    var progress = JsonUtility.FromJson<PlayerProgress>(File.ReadAllText(path));
                    if (progress != null)
                    {
                        progress.InvalidateIndex();
                        return progress;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"ProgressStore: load failed — {e.Message}");
            }
            return new PlayerProgress();
        }
    }
}
