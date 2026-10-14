using System;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Application-facing access to player progress. Loads once (cached, so it spans
    /// scenes), exposes the current <see cref="PlayerProgress"/>, and saves through
    /// an <see cref="IPlayerProgressStore"/> — so the storage implementation (local
    /// JSON now, backend later) is replaceable without touching gameplay code.
    ///
    /// It does NOT do mastery scoring, question generation, game scoring, or UI.
    /// </summary>
    public class PlayerProgressService
    {
        private readonly IPlayerProgressStore _store;
        private PlayerProgress _current;

        public PlayerProgressService(IPlayerProgressStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IPlayerProgressStore Store => _store;

        /// <summary>The cached progress, lazily loaded via the store. Settable for tests.</summary>
        public PlayerProgress Current
        {
            get => _current ??= _store.Load();
            set => _current = value;
        }

        /// <summary>Persist the current progress through the store.</summary>
        public void Save() => _store.Save(Current);

        /// <summary>Discard the cache and reload from the store.</summary>
        public void Reload() => _current = _store.Load();

        // --- app-wide default instance (local JSON store) --------------------------
        private static PlayerProgressService _default;

        /// <summary>Shared instance used by the game. Backed by the local JSON store;
        /// replaceable (e.g. by tests, or a future composition root).</summary>
        public static PlayerProgressService Default
        {
            get => _default ??= new PlayerProgressService(new LocalPlayerProgressStore());
            set => _default = value;
        }
    }
}
