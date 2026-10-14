using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the V1 Activity system: completed normal sessions + daily streak.
    /// Uses an injectable fake clock so calendar-date rules are deterministic, and an
    /// in-memory progress instance so nothing touches disk (except the explicit
    /// save/load tests). Revision is verified to stay separate from normal activity.
    /// </summary>
    public class ActivityTests
    {
        private sealed class FakeClock : IClock { public DateTime Now { get; set; } }

        private FakeClock _clock;
        private IClock _savedClock;
        private Action<PlayerProgress> _savedPersist;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _savedClock = ActivityService.Clock;
            _savedPersist = ActivityService.Persist;

            _clock = new FakeClock { Now = new DateTime(2026, 8, 16) };
            ActivityService.Clock = _clock;
            ActivityService.Progress = new PlayerProgress(); // in-memory isolation
            ActivityService.Persist = _ => { };              // no disk writes
            ActivityService.ResetSessionGuard();

            _path = Path.Combine(Application.persistentDataPath,
                "acttest_" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            ActivityService.Clock = _savedClock;
            ActivityService.Persist = _savedPersist;
            ActivityService.ResetSessionGuard();
            foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                if (File.Exists(_path + ext)) File.Delete(_path + ext);
        }

        private void SetDate(int y, int m, int d) => _clock.Now = new DateTime(y, m, d);
        private static GameSession Normal() => new GameSession { IsRevision = false };
        private static GameSession Revision() => new GameSession { IsRevision = true };

        // ---- 1/2/3: new player --------------------------------------------------
        [Test] public void New_Player_Has_Zero_Sessions() => Assert.AreEqual(0, ActivityService.TotalSessions);
        [Test] public void New_Player_Has_Zero_Current_Streak() => Assert.AreEqual(0, ActivityService.CurrentStreak);
        [Test] public void New_Player_Has_Zero_Longest_Streak() => Assert.AreEqual(0, ActivityService.LongestStreak);

        // ---- 4: first session ---------------------------------------------------
        [Test]
        public void First_Session_Sets_Sessions_And_Streaks_To_One()
        {
            SetDate(2026, 8, 16);
            Assert.IsTrue(ActivityService.RecordNormalSessionCompleted(Normal()));
            Assert.AreEqual(1, ActivityService.TotalSessions);
            Assert.AreEqual(1, ActivityService.CurrentStreak);
            Assert.AreEqual(1, ActivityService.LongestStreak);
            Assert.AreEqual("2026-08-16", ActivityService.LastActiveDate);
        }

        // ---- 5: multiple sessions same day -------------------------------------
        [Test]
        public void Same_Day_Sessions_Increment_Count_But_Not_Streak()
        {
            SetDate(2026, 8, 16);
            ActivityService.RecordNormalSessionCompleted(Normal());
            ActivityService.RecordNormalSessionCompleted(Normal());
            ActivityService.RecordNormalSessionCompleted(Normal());
            Assert.AreEqual(3, ActivityService.TotalSessions);
            Assert.AreEqual(1, ActivityService.CurrentStreak, "streak unchanged within one calendar day");
        }

        // ---- 6: consecutive days ------------------------------------------------
        [Test]
        public void Consecutive_Days_Increment_Streak()
        {
            SetDate(2026, 8, 16); ActivityService.RecordNormalSessionCompleted(Normal()); // 1
            SetDate(2026, 8, 17); ActivityService.RecordNormalSessionCompleted(Normal()); // 2
            SetDate(2026, 8, 18); ActivityService.RecordNormalSessionCompleted(Normal()); // 3
            Assert.AreEqual(3, ActivityService.CurrentStreak);
            Assert.AreEqual(3, ActivityService.LongestStreak);
            Assert.AreEqual(3, ActivityService.TotalSessions);
        }

        // ---- 7/8: missed day + longest preserved -------------------------------
        [Test]
        public void Missed_Day_Resets_Current_Streak_But_Keeps_Longest()
        {
            SetDate(2026, 8, 16); ActivityService.RecordNormalSessionCompleted(Normal()); // cur 1
            SetDate(2026, 8, 17); ActivityService.RecordNormalSessionCompleted(Normal()); // cur 2, longest 2
            SetDate(2026, 8, 19); ActivityService.RecordNormalSessionCompleted(Normal()); // skipped 18 → cur 1
            Assert.AreEqual(1, ActivityService.CurrentStreak, "missed day resets current");
            Assert.AreEqual(2, ActivityService.LongestStreak, "longest never decreases");
        }

        [Test]
        public void Multiple_Missed_Days_Also_Reset_To_One()
        {
            SetDate(2026, 8, 16); ActivityService.RecordNormalSessionCompleted(Normal());
            SetDate(2026, 8, 25); ActivityService.RecordNormalSessionCompleted(Normal());
            Assert.AreEqual(1, ActivityService.CurrentStreak);
        }

        // ---- 9/10: incomplete / abandoned --------------------------------------
        [Test]
        public void Incomplete_Session_Does_Not_Affect_Activity()
        {
            SetDate(2026, 8, 16);
            // A run that never reaches Results simply never calls the commit method.
            var _ = Normal(); // started but abandoned — no RecordNormalSessionCompleted
            Assert.AreEqual(0, ActivityService.TotalSessions);
            Assert.AreEqual(0, ActivityService.CurrentStreak);
        }

        [Test]
        public void Abandoning_After_A_Completed_One_Leaves_Prior_Activity_Intact()
        {
            SetDate(2026, 8, 16);
            ActivityService.RecordNormalSessionCompleted(Normal()); // committed
            var abandoned = Normal();                               // started later, abandoned
            _ = abandoned;
            Assert.AreEqual(1, ActivityService.TotalSessions);
            Assert.AreEqual(1, ActivityService.CurrentStreak);
        }

        // ---- 11/12: revision separate ------------------------------------------
        [Test]
        public void Revision_Does_Not_Increment_Normal_Session_Count()
        {
            SetDate(2026, 8, 16);
            Assert.IsFalse(ActivityService.RecordNormalSessionCompleted(Revision()));
            Assert.AreEqual(0, ActivityService.TotalSessions);
        }

        [Test]
        public void Revision_Does_Not_Update_Streak()
        {
            SetDate(2026, 8, 16);
            ActivityService.RecordNormalSessionCompleted(Revision());
            Assert.AreEqual(0, ActivityService.CurrentStreak);
            Assert.AreEqual(0, ActivityService.LongestStreak);
            Assert.AreEqual("", ActivityService.LastActiveDate ?? "");
        }

        // ---- 13: save / load ----------------------------------------------------
        [Test]
        public void Activity_Survives_Save_And_Load()
        {
            var store = new LocalPlayerProgressStore(_path);
            var p = new PlayerProgress();
            p.activity.sessions.totalSessions = 5;
            p.activity.streak.currentStreak = 3;
            p.activity.streak.longestStreak = 4;
            p.activity.streak.lastPlayedDate = "2026-08-16";
            store.Save(p);

            var loaded = store.Load();
            Assert.AreEqual(5, loaded.activity.sessions.totalSessions);
            Assert.AreEqual(3, loaded.activity.streak.currentStreak);
            Assert.AreEqual(4, loaded.activity.streak.longestStreak);
            Assert.AreEqual("2026-08-16", loaded.activity.streak.lastPlayedDate);
        }

        // ---- 14: old save without Activity/longest fields ----------------------
        [Test]
        public void Older_Save_Without_LongestStreak_Loads_Safely_And_Seeds_It()
        {
            // A nested save (pre-Activity streak fields): currentStreak present,
            // longestStreak absent, plus mastery that must be preserved.
            File.WriteAllText(_path,
                "{\"schemaVersion\":3,\"learning\":{\"kanjiMastery\":{\"entries\":" +
                "[{\"kanjiId\":27700,\"score\":8}]}}," +
                "\"activity\":{\"sessions\":{\"totalSessions\":6},\"streak\":" +
                "{\"currentStreak\":3,\"lastPlayedDate\":\"2026-08-10\"}}}");

            var loaded = new LocalPlayerProgressStore(_path).Load();

            Assert.AreEqual(ProgressMigration.CurrentSchemaVersion, loaded.schemaVersion, "bumped safely");
            Assert.AreEqual(3, loaded.activity.streak.currentStreak, "current preserved");
            Assert.AreEqual(3, loaded.activity.streak.longestStreak, "longest seeded from current");
            Assert.AreEqual(6, loaded.activity.sessions.totalSessions, "sessions preserved");
            Assert.AreEqual(8, loaded.learning.kanjiMastery.GetScore(27700), "mastery preserved");
        }

        // ---- 15/16: no double count --------------------------------------------
        [Test]
        public void Revisiting_Results_With_Same_Session_Does_Not_Double_Count()
        {
            SetDate(2026, 8, 16);
            var session = Normal();
            ActivityService.RecordNormalSessionCompleted(session); // Results shown
            ActivityService.RecordNormalSessionCompleted(session); // Results revisited/reloaded
            Assert.AreEqual(1, ActivityService.TotalSessions);
            Assert.AreEqual(1, ActivityService.CurrentStreak);
        }

        [Test]
        public void Duplicate_Completion_Events_Cannot_Double_Count()
        {
            SetDate(2026, 8, 16);
            var session = Normal();
            Assert.IsTrue(ActivityService.RecordNormalSessionCompleted(session), "first commit counts");
            Assert.IsFalse(ActivityService.RecordNormalSessionCompleted(session), "second is ignored");
            Assert.AreEqual(1, ActivityService.TotalSessions);
        }
    }
}
