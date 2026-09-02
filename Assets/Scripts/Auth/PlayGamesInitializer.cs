using UnityEngine;

namespace KanjiMaster.Auth
{
    /// <summary>
    /// Initializes and activates Google Play Games Services on Android, so the platform is
    /// ready for a later account sign-in. It runs automatically at app startup (no UI / Boot /
    /// scene coupling) and ONLY on a real Android device — it is a no-op in the Editor and on
    /// other platforms, so it can never break Editor / iOS / WebGL compilation or crash the game.
    ///
    /// Scope: it ONLY activates Play Games. It does NOT sign the user in, does NOT initialize or
    /// touch Firebase (Firebase stays lazily initialized by FirebaseInitializer), and does NOT
    /// affect guest / local identity. The canonical identity remains the Firebase UID, produced
    /// later by exchanging a Play Games server auth code — that flow is a separate task.
    ///
    /// Play Games plugin v2.x note: there is no PlayGamesClientConfiguration / InitializeInstance
    /// (those were v0.x/v1.x); activation is simply <c>PlayGamesPlatform.Activate()</c>.
    /// </summary>
    public static class PlayGamesInitializer
    {
        /// <summary>True once Play Games has been activated on-device.</summary>
        public static bool IsInitialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureInitialized()
        {
            if (IsInitialized) return;

#if UNITY_ANDROID
            // Play Games Services only exist on a real Android device. Skip in the Editor
            // (even with the Android build target) and everywhere else.
            if (Application.platform != RuntimePlatform.Android) return;

            try
            {
                GooglePlayGames.PlayGamesPlatform.DebugLogEnabled = Debug.isDebugBuild;
                GooglePlayGames.PlayGamesPlatform.Activate(); // v2.x: initialize + set active platform
                IsInitialized = true;
                Debug.Log("[Auth] Google Play Games Services activated — ready for account sign-in.");
            }
            catch (System.Exception e)
            {
                // Non-fatal (e.g. Play Games app missing, or the user is not signed into Play
                // Games). Actual sign-in is handled separately; initialization must never crash.
                Debug.LogWarning($"[Auth] Play Games initialization failed (non-fatal): {e.Message}");
            }
#endif
        }
    }
}
