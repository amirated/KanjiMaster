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

        private static bool _ready;

        /// <summary>Ensure Firebase dependencies are available before use. Returns false (no
        /// throw) when Firebase is not installed or dependencies are unavailable — the caller
        /// surfaces an app-level failure, not an exception.</summary>
        public static async Task<bool> EnsureReadyAsync()
        {
#if FIREBASE_AUTH
            if (_ready) return true;
            try
            {
                var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
                _ready = status == Firebase.DependencyStatus.Available;
                if (!_ready) Debug.LogWarning($"[Auth] Firebase dependencies not available: {status}");
                return _ready;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Auth] Firebase initialization failed: {e.Message}");
                return false;
            }
#else
            await Task.CompletedTask;
            return false; // Firebase not compiled in
#endif
        }
    }
}
