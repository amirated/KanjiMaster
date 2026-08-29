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
            if (!await FirebaseInitializer.EnsureReadyAsync())
                return FirebaseSignInResult.Fail("Sign-in is unavailable right now.");

            var firebaseCredential = BuildFirebaseCredential(credential);
            if (firebaseCredential == null)
                return FirebaseSignInResult.Fail("This sign-in method isn't available yet.");

            try
            {
                // Firebase Unity 13.x: use the AuthResult-returning API (the FirebaseUser-
                // returning overload is deprecated). AuthResult.User is the signed-in user.
                var result = await Firebase.Auth.FirebaseAuth.DefaultInstance
                    .SignInAndRetrieveDataWithCredentialAsync(firebaseCredential);
                var user = result?.User;
                return user != null && !string.IsNullOrEmpty(user.UserId)
                    ? FirebaseSignInResult.Ok(user.UserId)   // Firebase UID = canonical player id
                    : FirebaseSignInResult.Fail("Sign-in did not complete.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Auth] Firebase sign-in failed: {e.Message}"); // logged, never shown raw
                return FirebaseSignInResult.Fail("Sign-in failed. Please try again.");
            }
#else
            await Task.CompletedTask;
            return FirebaseSignInResult.Fail("Account sign-in is not available in this build.");
#endif
        }

#if FIREBASE_AUTH
        /// <summary>Map an app-level <see cref="PlatformCredential"/> to a Firebase credential.
        /// Each provider fills its own case when implemented (B3 Play Games, B4 Game Center,
        /// web Google/Apple) — verify the exact Firebase API at that time. Returns null for
        /// providers not yet wired.</summary>
        private static Firebase.Auth.Credential BuildFirebaseCredential(PlatformCredential c)
        {
            // Implemented per provider in later phases.
            return null;
        }
#endif
    }
}
