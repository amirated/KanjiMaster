using System;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Persistence ENVELOPE for identity — mirrors the progress <c>SaveData</c> pattern:
    /// a small versioned wrapper around the domain model, so schema versioning stays in
    /// the persistence layer and <see cref="PlayerIdentity"/> stays a pure domain model.
    ///
    ///   { "version": 1, "identity": { …PlayerIdentity… } }
    /// </summary>
    [Serializable]
    public class IdentitySaveData
    {
        /// <summary>The first (and current) identity-envelope format.</summary>
        public const int CurrentVersion = 1;

        public int version;
        public PlayerIdentity identity;
    }
}
