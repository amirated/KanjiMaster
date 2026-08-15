using System;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Player profile. Intentionally minimal/empty: the game needs no identity data
    /// (name, avatar, auth, preferences) yet, so this is just a stable, serializable
    /// place to add such fields later without reshaping PlayerProgress.
    /// </summary>
    [Serializable]
    public class ProfileData
    {
    }

    /// <summary>
    /// The root persistent player-progress model — a pure, serializable DATA object
    /// (no UI, scenes, I/O, MonoBehaviours, or scoring/selection logic). Its top-level
    /// shape is deliberately stable so features slot into the right section:
    ///
    ///   PlayerProgress
    ///   ├── Profile
    ///   ├── Learning    (KanjiMastery, LevelProgress)
    ///   ├── Activity    (Sessions, Streak)
    ///   └── Progression (XP)
    ///
    /// Persistence (file/JSON/versioning/migration) is owned by the store/service
    /// layer, not this class.
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        /// <summary>Persistence schema version of this serialized object.
        /// See <see cref="ProgressMigration"/>. Stamped by the store on save.</summary>
        public int schemaVersion;

        public ProfileData profile = new ProfileData();
        public LearningProgress learning = new LearningProgress();
        public ActivityProgress activity = new ActivityProgress();
        public ProgressionProgress progression = new ProgressionProgress();

        /// <summary>Rebuild runtime indexes after JSON deserialization.</summary>
        public void RebuildIndexes() => learning?.kanjiMastery?.InvalidateIndex();
    }
}
