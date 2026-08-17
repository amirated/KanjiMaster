using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;
using KanjiMaster.Services;
using KanjiMaster.UI;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the first-time onboarding: completion state, Boot routing, existing-
    /// player grandfathering (no lost progress), pure paging navigation, and persistence.
    /// UI glue (rendering slots) needs a scene, so the testable logic lives in
    /// OnboardingService / OnboardingRouter / OnboardingFlow.
    /// </summary>
    public class OnboardingTests
    {
        private Action<PlayerProgress> _savedPersist;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _savedPersist = MasteryService.Persist;
            _path = Path.Combine(Application.persistentDataPath, "onbtest_" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            MasteryService.Persist = _savedPersist;
            foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                if (File.Exists(_path + ext)) File.Delete(_path + ext);
        }

        // ---- New player (1) -----------------------------------------------------
        [Test]
        public void New_Progress_Has_Onboarding_Incomplete()
            => Assert.IsFalse(OnboardingService.IsComplete(new PlayerProgress()));

        // ---- Boot routing (2, 6, 7) --------------------------------------------
        [Test]
        public void Boot_Routes_New_Player_To_Onboarding()
            => Assert.AreEqual(SceneNames.Onboarding, OnboardingRouter.StartDestination(false));

        [Test]
        public void Boot_Routes_Completed_Player_To_Main_Menu()
            => Assert.AreEqual(SceneNames.MainMenu, OnboardingRouter.StartDestination(true));

        // ---- Completion (3, 4) --------------------------------------------------
        [Test]
        public void CompleteAsGuest_Marks_Complete_And_Persists()
        {
            var store = new LocalPlayerProgressStore(_path);
            var saved = PlayerProgressService.Default;
            try
            {
                PlayerProgressService.Default = new PlayerProgressService(store);
                Assert.IsFalse(OnboardingService.IsComplete(PlayerProgressService.Default.Current));

                OnboardingService.CompleteAsGuest();

                Assert.IsTrue(OnboardingService.IsComplete(PlayerProgressService.Default.Current), "marked complete");
                Assert.IsTrue(store.Exists(), "persisted to disk");
                Assert.IsTrue(OnboardingService.IsComplete(store.Load()), "completion survives reload");
            }
            finally { PlayerProgressService.Default = saved; }
        }

        // ---- Returning player persistence (20) ---------------------------------
        [Test]
        public void Completed_Onboarding_Survives_Save_And_Load()
        {
            var store = new LocalPlayerProgressStore(_path);
            var p = new PlayerProgress();
            OnboardingService.MarkComplete(p);
            store.Save(p);
            Assert.IsTrue(OnboardingService.IsComplete(store.Load()));
        }

        // ---- Exit during onboarding (21, 22) -----------------------------------
        [Test]
        public void No_Save_Means_Onboarding_Still_Incomplete()
        {
            // Exiting before completing writes nothing → next launch is a fresh default.
            var loaded = new LocalPlayerProgressStore(_path).Load(); // missing file
            Assert.IsFalse(OnboardingService.IsComplete(loaded));
            Assert.AreEqual(SceneNames.Onboarding,
                OnboardingRouter.StartDestination(OnboardingService.IsComplete(loaded)));
        }

        // ---- Existing-player migration (8–14) ----------------------------------
        [Test]
        public void Existing_Player_With_Progress_Is_Grandfathered_And_Data_Preserved()
        {
            // A pre-onboarding save (no hasCompletedOnboarding field) with real progress.
            File.WriteAllText(_path,
                "{\"schemaVersion\":5," +
                "\"learning\":{\"kanjiMastery\":{\"entries\":[{\"kanjiId\":27700,\"score\":8}]}}," +
                "\"activity\":{\"sessions\":{\"totalSessions\":12},\"streak\":{\"currentStreak\":4,\"longestStreak\":9,\"lastPlayedDate\":\"2026-08-16\"}}," +
                "\"progression\":{\"xp\":{\"totalXp\":1340}}}");

            var loaded = new LocalPlayerProgressStore(_path).Load();

            Assert.IsTrue(OnboardingService.IsComplete(loaded), "existing player is not forced through onboarding");
            // ...and nothing else changed:
            Assert.AreEqual(8, loaded.learning.kanjiMastery.GetScore(27700), "mastery intact");
            Assert.AreEqual(12, loaded.activity.sessions.totalSessions, "sessions intact");
            Assert.AreEqual(4, loaded.activity.streak.currentStreak, "streak intact");
            Assert.AreEqual(9, loaded.activity.streak.longestStreak);
            Assert.AreEqual(1340, loaded.progression.xp.totalXp, "XP intact");
        }

        [Test]
        public void Existing_Save_With_No_Progress_Is_Not_Grandfathered()
        {
            // A save that exists but has zero engagement → treated like a new player.
            File.WriteAllText(_path, "{\"schemaVersion\":5}");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.IsFalse(OnboardingService.IsComplete(loaded), "no meaningful progress → still onboards");
        }

        [Test]
        public void Fresh_Default_Is_Not_Grandfathered()
        {
            // Grandfathering runs on LOAD only, never on fresh-default creation.
            var p = new PlayerProgress();
            OnboardingService.GrandfatherIfExistingPlayer(p); // no progress → unchanged
            Assert.IsFalse(OnboardingService.IsComplete(p));
        }

        // ---- Navigation (15–18) -------------------------------------------------
        [Test]
        public void Flow_Next_Advances_And_Back_Retreats()
        {
            var flow = new OnboardingFlow(5);
            Assert.IsTrue(flow.IsFirst);
            Assert.IsTrue(flow.Next()); Assert.AreEqual(1, flow.Index);
            Assert.IsTrue(flow.Next()); Assert.AreEqual(2, flow.Index);
            Assert.IsTrue(flow.Back()); Assert.AreEqual(1, flow.Index);
        }

        [Test]
        public void Flow_First_Page_Cannot_Go_Back()
        {
            var flow = new OnboardingFlow(5);
            Assert.IsFalse(flow.Back());
            Assert.AreEqual(0, flow.Index);
            Assert.IsTrue(flow.IsFirst);
        }

        [Test]
        public void Flow_Last_Page_Next_Signals_Completion()
        {
            var flow = new OnboardingFlow(3);
            flow.Next(); flow.Next();
            Assert.IsTrue(flow.IsLast);
            Assert.IsFalse(flow.Next(), "no more pages → controller completes onboarding");
            Assert.AreEqual(2, flow.Index, "index does not run past the last page");
        }

        [Test]
        public void Flow_Handles_Single_Page()
        {
            var flow = new OnboardingFlow(1);
            Assert.IsTrue(flow.IsFirst);
            Assert.IsTrue(flow.IsLast);
            Assert.IsFalse(flow.Next());
        }

        // ---- Default content sanity --------------------------------------------
        [Test]
        public void Default_Content_Has_A_Short_Sequence_With_A_Final_Cta()
        {
            var pages = OnboardingPages.Default();
            Assert.That(pages.Count, Is.InRange(3, 5), "concise 3–5 cards");
            Assert.IsFalse(string.IsNullOrEmpty(pages[pages.Count - 1].ctaOverride), "final page has a CTA");
        }
    }
}
