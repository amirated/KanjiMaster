using System;

namespace KanjiMaster.Settings
{
    /// <summary>
    /// Player-facing app preferences (not gameplay progress). Serialized to a small JSON file by
    /// <see cref="SettingsService"/> — the project's convention is JSON files, NOT PlayerPrefs.
    /// Defaults are "on" so a fresh install has sound and haptics enabled.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public bool soundEffectsEnabled = true;
        public bool hapticsEnabled = true;

        public GameSettings Clone() => new GameSettings
        {
            soundEffectsEnabled = soundEffectsEnabled,
            hapticsEnabled = hapticsEnabled,
        };
    }

    /// <summary>Versioned envelope on disk, mirroring the progress/identity save files.</summary>
    [Serializable]
    public class SettingsSaveData
    {
        public int version = 1;
        public GameSettings settings = new GameSettings();
    }
}
