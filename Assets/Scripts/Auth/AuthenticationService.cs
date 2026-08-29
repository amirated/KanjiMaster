using System;
using System.Threading.Tasks;
using KanjiMaster.Progression;

namespace KanjiMaster.Auth
{
    /// <summary>
    /// App-level authentication orchestration. It selects the compile-time platform provider,
    /// acquires a credential, exchanges it for a Firebase user via the Firebase seam, and
    /// records the result in <see cref="IdentityService"/> (Firebase UID →
    /// <c>PlayerIdentity.userId</c>, state = Authenticated). The UI depends only on this and
    /// on <see cref="AuthOutcome"/> — never on Firebase types.
    ///
    /// Firebase is initialized LAZILY inside the Firebase seam, and only when a provider
    /// exists and authentication is actually requested. Guests never call this. There is no
    /// anonymous / fake-success path: with no provider it returns ProviderUnavailable.
    /// </summary>
    public class AuthenticationService
    {
        private readonly Func<IAuthenticationProvider> _providerFactory;
        private readonly IFirebaseAuthenticator _firebase;
        private readonly IdentityService _identity;

        public AuthenticationService(Func<IAuthenticationProvider> providerFactory,
            IFirebaseAuthenticator firebase, IdentityService identity)
        {
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _firebase = firebase ?? throw new ArgumentNullException(nameof(firebase));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        /// <summary>Attempt platform sign-in and, on success, mark the identity authenticated.</summary>
        public async Task<AuthOutcome> SignInWithPlatformAsync()
        {
            var provider = _providerFactory();
            if (provider == null) return AuthOutcome.ProviderUnavailable();

            ProviderCredentialResult cred;
            try { cred = await provider.AcquireCredentialAsync(); }
            catch { return AuthOutcome.Failed("Sign-in failed. Please try again."); }

            switch (cred.Status)
            {
                case ProviderCredentialStatus.Cancelled: return AuthOutcome.Cancelled();
                case ProviderCredentialStatus.Unavailable: return AuthOutcome.ProviderUnavailable();
                case ProviderCredentialStatus.Failed: return AuthOutcome.Failed(cred.Message ?? "Sign-in failed.");
            }

            var signIn = await _firebase.SignInAsync(cred.Credential);
            if (!signIn.Success) return AuthOutcome.Failed(signIn.Message ?? "Sign-in failed.");

            // Firebase UID is the canonical player id. The Firebase ID token (a separate,
            // short-lived credential for future backend calls) is deliberately NOT persisted.
            _identity.SetAuthenticated(signIn.FirebaseUid, cred.Credential.Provider);
            return AuthOutcome.Success(signIn.FirebaseUid, cred.Credential.Provider);
        }

        // --- app-wide default instance --------------------------------------------
        private static AuthenticationService _default;

        /// <summary>Shared instance used by the game (compile-time provider + real Firebase
        /// seam + the shared IdentityService). Settable for tests.</summary>
        public static AuthenticationService Default
        {
            get => _default ??= new AuthenticationService(
                AuthenticationProviderFactory.Create,
                new FirebaseAuthenticator(),
                IdentityService.Default);
            set => _default = value;
        }
    }
}
