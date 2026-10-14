namespace KanjiMaster.Auth
{
    /// <summary>App-level result status of an authentication attempt.</summary>
    public enum AuthStatus
    {
        Success,
        Cancelled,           // the player backed out of the provider flow
        Failed,              // something went wrong (message is user-safe, never a raw exception)
        ProviderUnavailable, // no authentication provider on this platform/build yet
    }

    /// <summary>
    /// The application-level outcome of an authentication attempt. It never carries Firebase
    /// types or raw exception text — the UI reacts to <see cref="Status"/> only. The Firebase
    /// UID and provider are populated on <see cref="AuthStatus.Success"/> only.
    /// </summary>
    public sealed class AuthOutcome
    {
        public AuthStatus Status { get; }
        public string FirebaseUid { get; }
        public string Provider { get; }
        public string Message { get; } // user-facing text for Failed; null otherwise

        private AuthOutcome(AuthStatus status, string firebaseUid, string provider, string message)
        {
            Status = status;
            FirebaseUid = firebaseUid;
            Provider = provider;
            Message = message;
        }

        public bool IsSuccess => Status == AuthStatus.Success;

        public static AuthOutcome Success(string firebaseUid, string provider) =>
            new AuthOutcome(AuthStatus.Success, firebaseUid, provider, null);

        public static AuthOutcome Cancelled() =>
            new AuthOutcome(AuthStatus.Cancelled, null, null, null);

        public static AuthOutcome Failed(string message) =>
            new AuthOutcome(AuthStatus.Failed, null, null, message);

        public static AuthOutcome ProviderUnavailable() =>
            new AuthOutcome(AuthStatus.ProviderUnavailable, null, null, null);
    }
}
