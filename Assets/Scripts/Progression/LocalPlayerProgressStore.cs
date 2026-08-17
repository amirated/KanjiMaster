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

            // Interpret the versioned envelope (or a legacy pre-envelope save).
            var result = SaveDataMigration.Load(json);
            switch (result.Status)
            {
                case SaveLoadStatus.Ok:
                    return result.Progress;

                case SaveLoadStatus.UnsupportedFutureVersion:
                    // Written by a newer app. Do NOT downgrade/overwrite it; preserve and
                    // fall back to a fresh default in memory.
                    return RecoverDefault("unsupported future save version (preserved, not overwritten)");

                case SaveLoadStatus.Empty:
                    return RecoverDefault("save file is empty");

                default: // Corrupt
                    return RecoverDefault("save could not be parsed or migrated");
            }
        }

        public void Save(PlayerProgress progress)
        {
            if (progress == null) return;
            // Serialize as the versioned envelope (version + PlayerProgress) as one object.
            string json = SaveDataMigration.Serialize(progress);
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

        private static PlayerProgress CreateDefault()
        {
            var p = new PlayerProgress { schemaVersion = ProgressMigration.CurrentSchemaVersion };
            LevelProgressionService.Ensure(p); // seed level states (Rising Star unlocked)
            return p;
        }

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
