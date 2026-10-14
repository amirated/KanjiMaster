using System;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Application-facing access to <see cref="PlayerIdentity"/>. Loads once (cached, spans
    /// scenes), exposes the current identity, and saves through an
    /// <see cref="IPlayerIdentityStore"/> — mirroring <see cref="PlayerProgressService"/>
    /// so the storage backend is replaceable. It owns the small set of identity
    /// transitions (guest / authenticated) and NEVER touches PlayerProgress.
    /// </summary>
    public class IdentityService
    {
        private readonly IPlayerIdentityStore _store;
        private PlayerIdentity _current;

        public IdentityService(IPlayerIdentityStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IPlayerIdentityStore Store => _store;

        /// <summary>The cached identity, lazily loaded via the store. Settable for tests.</summary>
        public PlayerIdentity Current
        {
            get => _current ??= _store.Load();
            set => _current = value;
        }

        public void Save() => _store.Save(Current);
        public void Reload() => _current = _store.Load();

        public bool IsAuthenticated => Current.IsAuthenticated;
        public bool IsGuest => Current.state == AuthState.Guest;

        /// <summary>
        /// Continue as a local guest and persist it. Assigns a stable guestId on the first
        /// call (kept on later calls, so a returning guest keeps the same id). Sets state to
        /// Guest. Does NOT touch PlayerProgress — existing local progress is retained as-is.
        /// </summary>
        public void ContinueAsGuest()
        {
            var id = Current;
            if (string.IsNullOrEmpty(id.guestId))
                id.guestId = Guid.NewGuid().ToString("N");
            id.state = AuthState.Guest;
            Save();
        }

        /// <summary>
        /// Mark the player authenticated with a real account and persist it. This is the
        /// connection point a future authentication provider calls ON SUCCESS. It is NOT
        /// invoked during Phase A (no provider is integrated). Progress is untouched;
        /// associating existing local progress with the account is a later task.
        /// </summary>
        public void SetAuthenticated(string userId, string provider)
        {
            var id = Current;
            id.state = AuthState.Authenticated;
            id.userId = userId;
            id.provider = provider;
            Save();
        }

        // --- app-wide default instance (local JSON store) --------------------------
        private static IdentityService _default;

        /// <summary>Shared instance used by the game. Backed by the local identity store;
        /// replaceable (tests, or a future composition root).</summary>
        public static IdentityService Default
        {
            get => _default ??= new IdentityService(new LocalPlayerIdentityStore());
            set => _default = value;
        }
    }
}
