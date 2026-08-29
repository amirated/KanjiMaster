using System;
using System.IO;
using UnityEngine;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Local JSON implementation of <see cref="IPlayerIdentityStore"/> — the ONLY class
    /// that knows the identity file path/JSON. Mirrors <see cref="LocalPlayerProgressStore"/>:
    /// its OWN file at Application.persistentDataPath/player_identity.json, a versioned
    /// envelope, atomic writes, and safe recovery (preserving a corrupt file). It never
    /// touches player_progress.json — identity and progress persist separately.
    /// </summary>
    public class LocalPlayerIdentityStore : IPlayerIdentityStore
    {
        public const string DefaultFileName = "player_identity.json";

        private readonly string _path;

        /// <param name="path">Override the save path (used by tests). Defaults to
        /// Application.persistentDataPath/player_identity.json.</param>
        public LocalPlayerIdentityStore(string path = null)
        {
            _path = path ?? Path.Combine(Application.persistentDataPath, DefaultFileName);
        }

        /// <summary>The absolute save path (exposed for diagnostics/tests only).</summary>
        public string FilePath => _path;

        public bool Exists() => File.Exists(_path);

        public PlayerIdentity Load()
        {
            if (!File.Exists(_path)) return CreateDefault(); // new / existing install with no identity yet

            string json;
            try { json = File.ReadAllText(_path); }
            catch (Exception e) { return RecoverDefault($"read failed: {e.Message}"); }

            if (string.IsNullOrWhiteSpace(json)) return RecoverDefault("identity file is empty");

            IdentitySaveData env;
            try { env = JsonUtility.FromJson<IdentitySaveData>(json); }
            catch { return RecoverDefault("identity could not be parsed"); }

            if (env == null || env.version < 1) return RecoverDefault("invalid identity version");
            if (env.version > IdentitySaveData.CurrentVersion)
                return RecoverDefault("unsupported future identity version (preserved, not overwritten)");

            // Note: JsonUtility always instantiates the nested object, so env.identity is
            // non-null; the version check above rejects non-envelope / malformed content.
            return env.identity ?? new PlayerIdentity();
        }

        public void Save(PlayerIdentity identity)
        {
            if (identity == null) return;
            var env = new IdentitySaveData { version = IdentitySaveData.CurrentVersion, identity = identity };
            string json = JsonUtility.ToJson(env, true);
            try { AtomicWrite(json); }
            catch (Exception e) { Debug.LogError($"[Identity] save failed: {e.Message}"); }
        }

        private void AtomicWrite(string json)
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string tmp = _path + ".tmp";
            File.WriteAllText(tmp, json);

            if (File.Exists(_path))
                File.Replace(tmp, _path, _path + ".bak");
            else
                File.Move(tmp, _path);
        }

        private static PlayerIdentity CreateDefault() => new PlayerIdentity(); // state = Unknown

        private PlayerIdentity RecoverDefault(string reason)
        {
            Debug.LogError($"[Identity] could not load identity ({reason}); starting fresh. " +
                           "Existing file preserved.");
            try
            {
                if (File.Exists(_path))
                    File.Copy(_path, _path + ".corrupt", overwrite: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Identity] could not preserve corrupt file: {e.Message}");
            }
            return CreateDefault();
        }
    }
}
