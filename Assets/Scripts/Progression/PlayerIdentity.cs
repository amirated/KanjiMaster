using System;

namespace KanjiMaster.Progression
{
    /// <summary>Whether the player has chosen/completed an identity.</summary>
    public enum AuthState
    {
        Unknown = 0,       // never chosen (new player, or existing install with no identity file)
        Guest = 1,         // continuing locally as a guest
        Authenticated = 2, // signed in to an account (future auth)
    }

    /// <summary>
    /// The player's IDENTITY — deliberately SEPARATE from <see cref="PlayerProgress"/>.
    /// Identity answers "who is this player / are they signed in"; progress answers "what
    /// have they learned". They are different concepts with different lifecycles and
    /// (later) different backends, so they are persisted as separate objects.
    ///
    /// Pure DATA: no persistence-schema field lives here (the store wraps it in a versioned
    /// envelope), no I/O, no UI. Serialized by JsonUtility (the enum stores as its int).
    /// </summary>
    [Serializable]
    public class PlayerIdentity
    {
        public AuthState state = AuthState.Unknown;

        /// <summary>Stable local id for a guest, assigned on first "Continue as Guest".
        /// Lets a future flow associate existing local progress with a new account.</summary>
        public string guestId;

        /// <summary>Set only when <see cref="AuthState.Authenticated"/> (future auth).</summary>
        public string userId;

        /// <summary>Auth provider when authenticated (future), e.g. "google" / "apple".</summary>
        public string provider;

        public bool IsAuthenticated => state == AuthState.Authenticated;
        public bool IsGuest => state == AuthState.Guest;
    }
}
