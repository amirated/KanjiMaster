using System;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// The persistence ENVELOPE written to disk: a small versioned wrapper around the
    /// domain model. Keeping the version here (not inside <see cref="PlayerProgress"/>)
    /// separates the persistence representation from the runtime model, so the save
    /// format can evolve independently.
    ///
    ///   { "version": 1, "playerProgress": { …PlayerProgress… } }
    ///
    /// This is the outer SAVE-format version. It is distinct from
    /// <see cref="ProgressMigration.CurrentSchemaVersion"/>, which continues to migrate
    /// the INNER PlayerProgress shape (additive fields) and to load pre-envelope saves.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>The first versioned save-envelope format.</summary>
        public const int CurrentVersion = 1;

        public int version;
        public PlayerProgress playerProgress;
    }
}
