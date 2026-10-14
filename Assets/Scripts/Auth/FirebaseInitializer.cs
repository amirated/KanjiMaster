using System.Threading.Tasks;
using UnityEngine;

namespace KanjiMaster.Auth
{
    /// <summary>
    /// Lazy Firebase initialization following the SDK's recommended
    /// <c>CheckAndFixDependenciesAsync()</c> pattern. It runs the FIRST time authentication is
    /// requested — NEVER from Boot and NEVER for guests. Compile-gated by the
    /// <c>FIREBASE_AUTH</c> scripting define so the project builds without the Firebase SDK
    /// installed (see docs/IDENTITY.md → "Firebase setup").
    ///
    /// Initialization has TWO steps, both required before a credential can be created:
    ///  1. <c>CheckAndFixDependenciesAsync()</c> — prepares the default <c>FirebaseApp</c>.
    ///  2. Access <c>FirebaseAuth.DefaultInstance</c> — this is what actually registers the
    ///     native Auth module. Credential factories such as
    ///     <c>PlayGamesAuthProvider.GetCredential(...)</c> fail with "Firebase Auth was not
    ///     initialized" if called before an Auth instance exists.
    ///
    /// A single in-flight <see cref="Task{Boolean}"/> is reused so concurrent sign-in attempts
    /// never run dependency resolution twice.
    /// </summary>
    public static class FirebaseInitializer
    {
        /// <summary>Whether the Firebase SDK is compiled into this build (define set).</summary>
        public static bool IsFirebaseCompiledIn =>
#if FIREBASE_AUTH
            true;
#else
            false;
#endif

#if FIREBASE_AUTH
        private static Firebase.Auth.FirebaseAuth _auth;
        private static Task<bool> _initTask;

        /// <summary>The initialized default FirebaseAuth instance, or null until
        /// <see cref="EnsureReadyAsync"/> has completed successfully. Creating this instance is
        /// what registers the native Auth module, so it must exist before any credential is built.</summary>
        public static Firebase.Auth.FirebaseAuth Auth => _auth;
#endif

        /// <summary>Ensure Firebase dependencies are available AND an Auth instance exists before
        /// use. Returns false (no throw) when Firebase is not installed or dependencies are
        /// unavailable — the caller surfaces an app-level failure, not an exception. Safe to call
        /// concurrently: all callers await the same in-flight initialization.</summary>
        public static Task<bool> EnsureReadyAsync()
        {
#if FIREBASE_AUTH
            // Reuse a single in-flight (or already-completed-successful) initialization. On a
            // failed attempt InitializeAsync clears _initTask so a later attempt can retry.
            return _initTask ??= InitializeAsync();
#else
            return Task.FromResult(false); // Firebase not compiled in
#endif
        }

#if FIREBASE_AUTH
        private static async Task<bool> InitializeAsync()
        {
            try
            {
                Debug.Log("[Auth] Firebase dependency check started…");
                var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
                Debug.Log($"[Auth] Firebase dependency check completed: {status}");

                if (status != Firebase.DependencyStatus.Available)
                {
                    Debug.LogWarning($"[Auth] Firebase dependencies not available: {status}");
                    _initTask = null; // allow a later retry
                    return false;
                }

                // Step 2: creating the Auth instance registers the native Auth module. This MUST
                // happen before PlayGamesAuthProvider.GetCredential(...) or the native layer logs
                // "Firebase Auth was not initialized, unable to create a Credential."
                _auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
                Debug.Log($"[Auth] Firebase App + Auth initialized (app={_auth?.App?.Name ?? "?"}).");
                return _auth != null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Auth] Firebase initialization failed: {e}");
                _initTask = null; // allow a later retry after a transient failure
                return false;
            }
        }
#endif
    }
}
