namespace KanjiMaster.Progression
{
    /// <summary>
    /// Player-facing progression tiers — the internal level identity ("levelId").
    /// The *display names* shown to the player live in <see cref="LevelDefinition"/> /
    /// <see cref="LevelCatalog"/>, not here, so labels can change without touching this
    /// enum. JLPT datasets are the underlying content mapping (informational):
    ///   RisingStar = N5,  YoungMaster = N4,  Adept = N3,  Expert = N2,
    ///   Master = N1,  Legend = beyond N1.
    ///
    /// All six exist so nothing hard-codes "only N5/N4", but only RisingStar and
    /// YoungMaster have content and a LevelDefinition today; N3+ is future work.
    /// Serialized as its integer value by JsonUtility, so the numeric values are
    /// STABLE — renaming a member does not invalidate existing saves.
    /// </summary>
    public enum PlayerLevel
    {
        RisingStar = 0,  // N5 — unlocked by default
        YoungMaster = 1, // N4 — unlocked by completing Rising Star
        Adept = 2,       // N3 (future)
        Expert = 3,      // N2 (future)
        Master = 4,      // N1 (future)
        Legend = 5,      // beyond N1 (future)
    }
}
