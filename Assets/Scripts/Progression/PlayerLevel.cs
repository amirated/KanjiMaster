namespace KanjiMaster.Progression
{
    /// <summary>
    /// User-facing progression tiers. These are the primary names the game uses;
    /// the JLPT levels are the underlying content mapping (informational):
    ///   Novice = N5,  Apprentice = N4,  Adept = N3,  Expert = N2,  Master = N1,
    ///   Legend = beyond N1.
    ///
    /// All six exist in the model so nothing hard-codes "only N5/N4". Only Novice
    /// and Apprentice have content today; N3+ content is NOT part of this task.
    /// (Serialized as its integer value by JsonUtility.)
    /// </summary>
    public enum PlayerLevel
    {
        Novice = 0,     // N5
        Apprentice = 1, // N4
        Adept = 2,      // N3
        Expert = 3,     // N2
        Master = 4,     // N1
        Legend = 5,     // beyond N1
    }
}
