namespace KanjiMaster.Progression
{
    /// <summary>
    /// The persistence boundary for player progress. Implementations own HOW and
    /// WHERE <see cref="PlayerProgress"/> is stored (local JSON today, a backend
    /// later) — nothing above this interface should know the storage details.
    ///
    /// It deals in the whole <see cref="PlayerProgress"/> object, never individual
    /// kanji scores. It contains no gameplay logic: no mastery scoring, no question
    /// selection, no XP.
    /// </summary>
    public interface IPlayerProgressStore
    {
        /// <summary>True if a saved progress record currently exists.</summary>
        bool Exists();

        /// <summary>
        /// Load progress, migrated to the current schema. Never throws and never
        /// returns null: on missing/corrupt/unmigratable data it returns a fresh
        /// default <see cref="PlayerProgress"/> (implementations should preserve the
        /// bad file for inspection rather than overwrite it).
        /// </summary>
        PlayerProgress Load();

        /// <summary>Persist the given progress. Should be atomic where the platform allows.</summary>
        void Save(PlayerProgress progress);
    }
}
