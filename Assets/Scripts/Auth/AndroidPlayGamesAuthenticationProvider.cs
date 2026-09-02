using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace KanjiMaster.Auth
{
    /// <summary>
    /// Android platform provider (<see cref="IAuthenticationProvider"/>): signs the user into
    /// Google Play Games and obtains a SERVER-SIDE auth code. It returns only an app-level
    /// <see cref="PlatformCredential"/> — it does NO Firebase work and never becomes the app
    /// identity itself. The Firebase layer exchanges the server auth code (via
    /// <c>PlayGamesAuthProvider</c>) for a Firebase user whose UID is the canonical identity.
    ///
    /// Android-only: Play Games types are referenced solely under <c>#if UNITY_ANDROID</c>, and
    /// the factory only constructs this on Android, so iOS / WebGL / Editor(non-Android) are
    /// unaffected. Callback-based Play Games APIs are bridged to a Task and completed exactly
    /// once. It does not touch guest/local identity.
    /// </summary>
    public sealed class AndroidPlayGamesAuthenticationProvider : IAuthenticationProvider
    {
        public string ProviderId => AuthProviders.GooglePlayGames;

        public Task<ProviderCredentialResult> AcquireCredentialAsync()
        {
#if UNITY_ANDROID
            var tcs = new TaskCompletionSource<ProviderCredentialResult>();

            // Reuse the existing initializer (idempotent) rather than scattering Activate().
            PlayGamesInitializer.EnsureInitialized();

            var platform = PlayGamesPlatform.Instance;
            if (platform == null)
            {
                Debug.LogWarning("[Auth] Play Games platform is unavailable.");
                return Task.FromResult(ProviderCredentialResult.Unavailable());
            }

            if (platform.IsAuthenticated())
            {
                // Already signed into Play Games (e.g. automatic sign-in at startup).
                RequestServerCode(platform, tcs);
            }
            else
            {
                // The user explicitly pressed "Continue with Account" → interactive sign-in.
                platform.ManuallyAuthenticate(status =>
                {
                    switch (status)
                    {
                        case SignInStatus.Success:
                            RequestServerCode(platform, tcs);
                            break;
                        case SignInStatus.Canceled:
                            TrySet(tcs, ProviderCredentialResult.Cancelled());
                            break;
                        default: // InternalError / anything else
                            Debug.LogWarning($"[Auth] Play Games sign-in failed: {status}");
                            TrySet(tcs, ProviderCredentialResult.Failed("Play Games sign-in failed."));
                            break;
                    }
                });
            }

            return tcs.Task;
#else
            // Never constructed off Android (the factory guards it); safe fallback anyway.
            return Task.FromResult(ProviderCredentialResult.Unavailable());
#endif
        }

#if UNITY_ANDROID
        private static void RequestServerCode(PlayGamesPlatform platform,
            TaskCompletionSource<ProviderCredentialResult> tcs)
        {
            try
            {
                // forceRefreshToken:false — the single exchange happens server-side via Firebase.
                platform.RequestServerSideAccess(false, code =>
                {
                    if (string.IsNullOrEmpty(code))
                    {
                        Debug.LogWarning("[Auth] Play Games returned an empty server auth code.");
                        TrySet(tcs, ProviderCredentialResult.Failed("Could not get a Play Games access code."));
                        return;
                    }
                    TrySet(tcs, ProviderCredentialResult.Acquired(
                        new PlatformCredential(AuthProviders.GooglePlayGames, serverAuthCode: code)));
                });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Auth] Play Games RequestServerSideAccess failed: {e.Message}");
                TrySet(tcs, ProviderCredentialResult.Failed("Play Games sign-in failed."));
            }
        }

        // Completes the request exactly once (guards against duplicate callbacks).
        private static void TrySet(TaskCompletionSource<ProviderCredentialResult> tcs, ProviderCredentialResult result)
            => tcs.TrySetResult(result);
#endif
    }
}
