namespace KanjiMaster.Progression
{
    /// <summary>
    /// The persistence boundary for <see cref="PlayerIdentity"/>. Implementations own HOW
    /// and WHERE identity is stored (local JSON today, an auth backend later) — nothing
    /// above this interface knows the storage details. Kept separate from
    /// <see cref="IPlayerProgressStore"/> so identity and progress persist independently.
    /// </summary>
    public interface IPlayerIdentityStore
    {
        /// <summary>True if a saved identity record currently exists.</summary>
        bool Exists();

        /// <summary>Load identity. Never throws and never returns null: on missing/corrupt
        /// data it returns a fresh default (<see cref="AuthState.Unknown"/>), preserving a
        /// bad file rather than overwriting it.</summary>
        PlayerIdentity Load();

        /// <summary>Persist the given identity. Atomic where the platform allows.</summary>
        void Save(PlayerIdentity identity);
    }
}
