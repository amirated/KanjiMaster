namespace KanjiMaster.Auth
{
    /// <summary>
    /// Canonical authentication-provider identifiers. Stored in
    /// <c>PlayerIdentity.provider</c> — centralized here so provider names are not scattered
    /// as magic strings. The player id itself is always the Firebase UID, not these.
    /// </summary>
    public static class AuthProviders
    {
        public const string GooglePlayGames = "google_play_games"; // Android (future B3)
        public const string GameCenter = "game_center";            // iOS (future B4)
        public const string Google = "google";                     // web / cross-platform (future)
        public const string Apple = "apple";                       // web / cross-platform (future)
    }
}
