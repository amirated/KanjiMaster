using System.Threading.Tasks;
using UnityEngine;

namespace KanjiMaster.Auth
{
    /// <summary>Result of exchanging a platform credential for a Firebase user.</summary>
    public sealed class FirebaseSignInResult
    {
        public bool Success { get; }
        public string FirebaseUid { get; }
        public string Message { get; } // user-safe text on failure

        private FirebaseSignInResult(bool success, string firebaseUid, string message)
        {
            Success = success;
            FirebaseUid = firebaseUid;
            Message = message;
        }

        public static FirebaseSignInResult Ok(string firebaseUid) => new FirebaseSignInResult(true, firebaseUid, null);
        public static FirebaseSignInResult Fail(string message) => new FirebaseSignInResult(false, null, message);
    }

    /// <summary>
    /// The Firebase-facing seam: exchange a platform credential for a Firebase user id. Kept
    /// behind an interface so <see cref="AuthenticationService"/> and its tests never touch
    /// Firebase types (which can't run in EditMode anyway).
    /// </summary>
    public interface IFirebaseAuthenticator
    {
        Task<FirebaseSignInResult> SignInAsync(PlatformCredential credential);
    }

    /// <summary>
    /// Production implementation, compile-gated by <c>FIREBASE_AUTH</c>. It lazily initializes
    /// Firebase, exchanges the platform credential for a Firebase user, and returns the UID.
    /// It NEVER creates an anonymous user — it only signs in with an explicit provider
    /// credential. Without the define (Firebase not installed) it returns a safe failure.
    /// </summary>
    public sealed class FirebaseAuthenticator : IFirebaseAuthenticator
    {
        public async Task<FirebaseSignInResult> SignInAsync(PlatformCredential credential)
        {
#if FIREBASE_AUTH
            // Step 1: ensure Firebase (App + Auth instance) is initialized BEFORE building a
            // credential. This awaits init rather than relying on timing, and is a Firebase-layer
            // failure that is reported separately from Play Games errors (handled in the provider).
            Debug.Log("[Auth] Firebase sign-in requested — ensuring Firebase is initialized…");
            if (!await FirebaseInitializer.EnsureReadyAsync())
                return FirebaseSignInResult.Fail("Sign-in is unavailable right now.");

            var auth = FirebaseInitializer.Auth;
            if (auth == null)
            {
                Debug.LogError("[Auth] Firebase Auth instance is null after initialization.");
                return FirebaseSignInResult.Fail("Sign-in is unavailable right now.");
            }

            // Step 2: build the Firebase credential. Only safe now that the Auth instance exists —
            // PlayGamesAuthProvider.GetCredential requires an initialized Auth module.
            Firebase.Auth.Credential firebaseCredential;
            try
            {
                Debug.Log("[Auth] Creating Firebase credential from platform credential…");
                firebaseCredential = BuildFirebaseCredential(credential);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Auth] Failed to create Firebase credential: {e}");
                return FirebaseSignInResult.Fail("Sign-in failed. Please try again.");
            }
            if (firebaseCredential == null)
                return FirebaseSignInResult.Fail("This sign-in method isn't available yet.");

            // Step 3: exchange the credential for a Firebase user.
            try
            {
                Debug.Log("[Auth] Exchanging platform credential with Firebase…");
                // Firebase Unity 13.x: use the AuthResult-returning API (the FirebaseUser-
                // returning overload is deprecated). AuthResult.User is the signed-in user.
                var result = await auth.SignInAndRetrieveDataWithCredentialAsync(firebaseCredential);
                var user = result?.User;
                if (user != null && !string.IsNullOrEmpty(user.UserId))
                {
                    Debug.Log($"[Auth] Firebase sign-in succeeded (uid length={user.UserId.Length}).");
                    return FirebaseSignInResult.Ok(user.UserId); // Firebase UID = canonical player id
                }
                Debug.LogWarning("[Auth] Firebase sign-in returned no user.");
                return FirebaseSignInResult.Fail("Sign-in did not complete.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Auth] Firebase sign-in failed: {e}"); // full details to logcat, never shown raw
                return FirebaseSignInResult.Fail("Sign-in failed. Please try again.");
            }
#else
            await Task.CompletedTask;
            return FirebaseSignInResult.Fail("Account sign-in is not available in this build.");
#endif
        }

#if FIREBASE_AUTH
        /// <summary>Map an app-level <see cref="PlatformCredential"/> to a Firebase credential.
        /// Android Play Games is wired; iOS Game Center and web Google/Apple are added in later
        /// phases. Returns null for providers not yet wired.</summary>
        private static Firebase.Auth.Credential BuildFirebaseCredential(PlatformCredential c)
        {
            switch (c.Provider)
            {
                case AuthProviders.GooglePlayGames:
                    // Exchange the Play Games server auth code for a Firebase credential.
                    return Firebase.Auth.PlayGamesAuthProvider.GetCredential(c.ServerAuthCode);
                default:
                    return null;
            }
        }
#endif
    }
}
