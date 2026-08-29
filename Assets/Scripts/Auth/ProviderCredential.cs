namespace KanjiMaster.Auth
{
    /// <summary>
    /// App-level credential material handed back by a platform provider — deliberately NO
    /// Firebase types, so <see cref="IAuthenticationProvider"/> stays Firebase-agnostic and
    /// unit-testable. The Firebase layer maps this to a Firebase credential. Which fields are
    /// populated depends on the provider (e.g. Play Games uses a server auth code; Google
    /// uses an id token + access token).
    /// </summary>
    public sealed class PlatformCredential
    {
        public string Provider { get; }
        public string IdToken { get; }
        public string AccessToken { get; }
        public string ServerAuthCode { get; }
        public string RawNonce { get; }

        public PlatformCredential(string provider, string idToken = null, string accessToken = null,
            string serverAuthCode = null, string rawNonce = null)
        {
            Provider = provider;
            IdToken = idToken;
            AccessToken = accessToken;
            ServerAuthCode = serverAuthCode;
            RawNonce = rawNonce;
        }
    }

    public enum ProviderCredentialStatus { Acquired, Cancelled, Failed, Unavailable }

    /// <summary>Result of a provider's credential-acquisition attempt.</summary>
    public sealed class ProviderCredentialResult
    {
        public ProviderCredentialStatus Status { get; }
        public PlatformCredential Credential { get; }
        public string Message { get; }

        private ProviderCredentialResult(ProviderCredentialStatus status, PlatformCredential credential, string message)
        {
            Status = status;
            Credential = credential;
            Message = message;
        }

        public static ProviderCredentialResult Acquired(PlatformCredential credential) =>
            new ProviderCredentialResult(ProviderCredentialStatus.Acquired, credential, null);
        public static ProviderCredentialResult Cancelled() =>
            new ProviderCredentialResult(ProviderCredentialStatus.Cancelled, null, null);
        public static ProviderCredentialResult Failed(string message) =>
            new ProviderCredentialResult(ProviderCredentialStatus.Failed, null, message);
        public static ProviderCredentialResult Unavailable() =>
            new ProviderCredentialResult(ProviderCredentialStatus.Unavailable, null, null);
    }
}
