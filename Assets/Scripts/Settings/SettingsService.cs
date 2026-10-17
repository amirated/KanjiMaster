using System;
using System.IO;
using UnityEngine;

namespace KanjiMaster.Settings
{
    /// <summary>
    /// Loads/saves <see cref="GameSettings"/> and is the single source of truth for app
    /// preferences. Services and UI read the properties here; the setters persist immediately and
    /// raise <see cref="Changed"/> so a toggle takes effect at once. JSON file at
    /// <c>Application.persistentDataPath/settings.json</c> (no PlayerPrefs), written atomically.
    /// </summary>
    public class SettingsService
    {
        public const string DefaultFileName = "settings.json";

        private readonly string _path;
        private GameSettings _current;

        /// <summary>Raised after any preference changes (and is persisted).</summary>
        public event Action Changed;

        public SettingsService(string filePath = null)
        {
            _path = string.IsNullOrEmpty(filePath)
                ? Path.Combine(Application.persistentDataPath, DefaultFileName)
                : filePath;
        }

        public GameSettings Current => _current ??= Load();

        public bool SoundEffectsEnabled
        {
            get => Current.soundEffectsEnabled;
            set { if (Current.soundEffectsEnabled != value) { Current.soundEffectsEnabled = value; Save(); } }
        }

        public bool HapticsEnabled
        {
            get => Current.hapticsEnabled;
            set { if (Current.hapticsEnabled != value) { Current.hapticsEnabled = value; Save(); } }
        }

        public void Save()
        {
            try
            {
                var json = JsonUtility.ToJson(new SettingsSaveData { settings = Current });
                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(_path)) File.Replace(tmp, _path, null);
                else File.Move(tmp, _path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Could not save settings: {e.Message}");
            }
            Changed?.Invoke();
        }

        private GameSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var data = JsonUtility.FromJson<SettingsSaveData>(File.ReadAllText(_path));
                    if (data?.settings != null) return data.settings;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Could not read settings, using defaults: {e.Message}");
            }
            return new GameSettings(); // defaults (all on)
        }

        // --- app-wide default instance -------------------------------------------
        private static SettingsService _default;

        /// <summary>Shared instance used by the game. Settable for tests.</summary>
        public static SettingsService Default
        {
            get => _default ??= new SettingsService();
            set => _default = value;
        }
    }
}
