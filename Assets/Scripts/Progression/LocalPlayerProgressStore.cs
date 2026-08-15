using System;
using System.IO;
using UnityEngine;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Local JSON implementation of <see cref="IPlayerProgressStore"/>. This is the
    /// ONLY class that knows the file path and JSON details — gameplay never touches
    /// the file. Uses Application.persistentDataPath, writes atomically, migrates on
    /// load, and fails safely on corrupt data (preserving the bad file).
    ///
    /// No PlayerPrefs, no database, no cloud. A future RemotePlayerProgressStore will
    /// implement the same interface without changing anything above it.
    /// </summary>
    public class LocalPlayerProgressStore : IPlayerProgressStore
    {
        public const string DefaultFileName = "player_progress.json";

        private readonly string _path;

        /// <param name="path">Override the save path (used by tests). Defaults to
        /// Application.persistentDataPath/player_progress.json.</param>
        public LocalPlayerProgressStore(string path = null)
        {
            _path = path ?? Path.Combine(Application.persistentDataPath, DefaultFileName);
        }

        /// <summary>The absolute save path (exposed for diagnostics/tests only).</summary>
        public string FilePath => _path;

        public bool Exists() => File.Exists(_path);

        public PlayerProgress Load()
        {
            if (!File.Exists(_path)) return CreateDefault(); // new player — not an error

            string json;
            try { json = File.ReadAllText(_path); }
            catch (Exception e) { return RecoverDefault($"read failed: {e.Message}"); }

            if (string.IsNullOrWhiteSpace(json)) return RecoverDefault("save file is empty");

            // Parse + migrate to the current schema (handles legacy flat saves too).
            var progress = ProgressMigration.FromJson(json);
            if (progress == null) return RecoverDefault("save could not be parsed or migrated");

            progress.RebuildIndexes(); // rebuild the runtime lookup after deserialization
            return progress;
        }

        public void Save(PlayerProgress progress)
        {
            if (progress == null) return;
            progress.schemaVersion = ProgressMigration.CurrentSchemaVersion; // stamp current version
            string json = JsonUtility.ToJson(progress, true);
            try { AtomicWrite(json); }
            catch (Exception e) { Debug.LogError($"[Progress] save failed: {e.Message}"); }
        }

        /// <summary>
        /// Atomic write: serialize to a temp file, then swap it into place. If a
        /// previous save exists it is moved to a ".bak" backup during the swap, so a
        /// failure can never destroy the last known-good save.
        /// </summary>
        private void AtomicWrite(string json)
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string tmp = _path + ".tmp";
            File.WriteAllText(tmp, json); // 1. write complete data to a temp file

            if (File.Exists(_path))
                File.Replace(tmp, _path, _path + ".bak"); // 2a. atomic replace + keep backup
            else
                File.Move(tmp, _path);                    // 2b. first save (nothing to replace)
        }

        private static PlayerProgress CreateDefault() =>
            new PlayerProgress { schemaVersion = ProgressMigration.CurrentSchemaVersion };

        /// <summary>Log the problem, preserve the unreadable file for later inspection,
        /// and fall back to fresh default progress. Does NOT overwrite the bad file.</summary>
        private PlayerProgress RecoverDefault(string reason)
        {
            Debug.LogError($"[Progress] could not load save ({reason}); starting fresh. " +
                           "Existing file preserved.");
            try
            {
                if (File.Exists(_path))
                    File.Copy(_path, _path + ".corrupt", overwrite: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Progress] could not preserve corrupt save: {e.Message}");
            }
            return CreateDefault();
        }
    }
}
