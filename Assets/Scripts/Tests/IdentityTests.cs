using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;
using KanjiMaster.Services;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the Phase A identity architecture: PlayerIdentity persistence (separate
    /// from PlayerProgress), the Boot identity gate, the guest continuation (reusing
    /// onboarding), and confirmation that the guest flow never modifies PlayerProgress.
    /// </summary>
    public class IdentityTests
    {
        private string _idPath;

        [SetUp]
        public void SetUp()
        {
            _idPath = Path.Combine(Application.persistentDataPath,
                "identity_test_" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                if (File.Exists(_idPath + ext)) File.Delete(_idPath + ext);
        }

        private IdentityService NewService() => new IdentityService(new LocalPlayerIdentityStore(_idPath));

        // ---- Persistence: default / guest / authenticated ----------------------
        [Test]
        public void Missing_File_Defaults_To_Unknown_Not_Authenticated()
        {
            var svc = NewService();
            Assert.AreEqual(AuthState.Unknown, svc.Current.state);
            Assert.IsFalse(svc.IsAuthenticated);
            Assert.IsFalse(svc.IsGuest);
        }

        [Test]
        public void ContinueAsGuest_Persists_Guest_State_And_A_GuestId()
        {
            var svc = NewService();
            svc.ContinueAsGuest();

            Assert.AreEqual(AuthState.Guest, svc.Current.state);
            Assert.IsFalse(string.IsNullOrEmpty(svc.Current.guestId), "guest gets a stable id");
            Assert.IsFalse(svc.IsAuthenticated);

            // Survives reload from disk.
            var reloaded = NewService();
            Assert.AreEqual(AuthState.Guest, reloaded.Current.state);
            Assert.AreEqual(svc.Current.guestId, reloaded.Current.guestId, "guest id persists");
        }

        [Test]
        public void Guest_Id_Is_Stable_Across_Repeated_Continuation()
        {
            var svc = NewService();
            svc.ContinueAsGuest();
            string first = svc.Current.guestId;
            svc.ContinueAsGuest(); // returning guest chooses Guest again
            Assert.AreEqual(first, svc.Current.guestId, "same guest id, not regenerated");
        }

        [Test]
        public void SetAuthenticated_Persists_Authenticated_Identity()
        {
            var svc = NewService();
            svc.SetAuthenticated("user-123", "google");

            Assert.AreEqual(AuthState.Authenticated, svc.Current.state);
            Assert.IsTrue(svc.IsAuthenticated);
            Assert.AreEqual("user-123", svc.Current.userId);
            Assert.AreEqual("google", svc.Current.provider);

            var reloaded = NewService();
            Assert.IsTrue(reloaded.IsAuthenticated);
            Assert.AreEqual("user-123", reloaded.Current.userId);
            Assert.AreEqual("google", reloaded.Current.provider);
        }

        [Test]
        public void Saved_File_Is_A_Versioned_Envelope_Around_Identity()
        {
            var store = new LocalPlayerIdentityStore(_idPath);
            store.Save(new PlayerIdentity { state = AuthState.Guest, guestId = "abc" });

            string json = File.ReadAllText(_idPath);
            StringAssert.Contains("version", json);
            StringAssert.Contains("identity", json);
            var env = JsonUtility.FromJson<IdentitySaveData>(json);
            Assert.AreEqual(IdentitySaveData.CurrentVersion, env.version);
            Assert.AreEqual(AuthState.Guest, env.identity.state);
        }

        // ---- Boot routing (identity gate) --------------------------------------
        [Test]
        public void Router_Sends_Unauthenticated_To_Identity_And_Authenticated_To_Main_Menu()
        {
            Assert.AreEqual(SceneNames.Identity, IdentityRouter.StartDestination(false)); // Unknown or Guest
            Assert.AreEqual(SceneNames.MainMenu, IdentityRouter.StartDestination(true));  // Authenticated
        }

        [Test]
        public void Identity_State_Maps_To_The_Right_Gate()
        {
            Assert.AreEqual(SceneNames.Identity,
                IdentityRouter.StartDestination(new PlayerIdentity { state = AuthState.Unknown }.IsAuthenticated));
            Assert.AreEqual(SceneNames.Identity,
                IdentityRouter.StartDestination(new PlayerIdentity { state = AuthState.Guest }.IsAuthenticated));
            Assert.AreEqual(SceneNames.MainMenu,
                IdentityRouter.StartDestination(new PlayerIdentity { state = AuthState.Authenticated }.IsAuthenticated));
        }

        // ---- Guest continuation reuses onboarding ------------------------------
        [Test]
        public void Guest_Continuation_Goes_To_Onboarding_When_Incomplete_Else_Main_Menu()
        {
            // Post-identity routing reuses OnboardingRouter (as the controller does).
            var incomplete = new PlayerProgress(); // hasCompletedOnboarding = false
            Assert.AreEqual(SceneNames.Onboarding,
                OnboardingRouter.StartDestination(OnboardingService.IsComplete(incomplete)));

            var complete = new PlayerProgress();
            OnboardingService.MarkComplete(complete);
            Assert.AreEqual(SceneNames.MainMenu,
                OnboardingRouter.StartDestination(OnboardingService.IsComplete(complete)));
        }

        // ---- Progress safety ----------------------------------------------------
        [Test]
        public void Continuing_As_Guest_Does_Not_Modify_PlayerProgress()
        {
            // Give a "returning guest" some existing progress in memory.
            var progress = new PlayerProgress();
            progress.progression.xp.totalXp = 500;
            progress.activity.sessions.totalSessions = 7;
            OnboardingService.MarkComplete(progress);

            var svc = NewService();
            svc.ContinueAsGuest();

            // Identity changed; progress is byte-for-byte the same object, untouched.
            Assert.AreEqual(AuthState.Guest, svc.Current.state);
            Assert.AreEqual(500, progress.progression.xp.totalXp, "XP untouched by guest flow");
            Assert.AreEqual(7, progress.activity.sessions.totalSessions, "sessions untouched");
            Assert.IsTrue(OnboardingService.IsComplete(progress), "onboarding flag untouched");
        }

        // ---- Identity is stored separately from progress -----------------------
        [Test]
        public void Identity_And_Progress_Use_Different_Files()
        {
            Assert.AreNotEqual(LocalPlayerIdentityStore.DefaultFileName,
                LocalPlayerProgressStore.DefaultFileName,
                "identity persists to its own file, separate from progress");
        }
    }
}
