using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the V1 Global XP system: base answer XP, mastery multiplier bands,
    /// pre-answer mastery usage, wrong-answer 0 XP, session + streak bonuses, Revision
    /// behaviour, persistence, versioning, and separation from mastery/levels/UI.
    /// Pure calculator rules need no scene; awarding uses an in-memory PlayerProgress.
    /// </summary>
    public class XpTests
    {
        private Action<PlayerProgress> _savedXpPersist, _savedMasteryPersist, _savedActivityPersist;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _savedXpPersist = XpService.Persist;
            _savedMasteryPersist = MasteryService.Persist;
            _savedActivityPersist = ActivityService.Persist;

            var fresh = new PlayerProgress();
            XpService.Progress = fresh;         // all three proxy PlayerProgressService.Default.Current
            XpService.Persist = _ => { };
            MasteryService.Persist = _ => { };
            ActivityService.Persist = _ => { };
            ActivityService.ResetSessionGuard();

            _path = Path.Combine(Application.persistentDataPath, "xptest_" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            XpService.Persist = _savedXpPersist;
            MasteryService.Persist = _savedMasteryPersist;
            ActivityService.Persist = _savedActivityPersist;
            foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                if (File.Exists(_path + ext)) File.Delete(_path + ext);
        }

        // ---- 1–6: base answer XP -----------------------------------------------
        [Test] public void Base_English_Untimed_Is_10() => Assert.AreEqual(10, XpCalculator.BaseAnswerXp(AnswerMode.English, TimerMode.Untimed));
        [Test] public void Base_English_Timed_Is_20() => Assert.AreEqual(20, XpCalculator.BaseAnswerXp(AnswerMode.English, TimerMode.Timed));
        [Test] public void Base_Romaji_Untimed_Is_20() => Assert.AreEqual(20, XpCalculator.BaseAnswerXp(AnswerMode.Romaji, TimerMode.Untimed));
        [Test] public void Base_Romaji_Timed_Is_40() => Assert.AreEqual(40, XpCalculator.BaseAnswerXp(AnswerMode.Romaji, TimerMode.Timed));
        [Test] public void Base_Kana_Untimed_Is_40() => Assert.AreEqual(40, XpCalculator.BaseAnswerXp(AnswerMode.Kana, TimerMode.Untimed));
        [Test] public void Base_Kana_Timed_Is_80() => Assert.AreEqual(80, XpCalculator.BaseAnswerXp(AnswerMode.Kana, TimerMode.Timed));

        // ---- 7–18: mastery multiplier bands ------------------------------------
        [TestCase(0, 1.50f)] [TestCase(2, 1.50f)]
        [TestCase(3, 1.25f)] [TestCase(6, 1.25f)]
        [TestCase(7, 1.00f)] [TestCase(12, 1.00f)]
        [TestCase(13, 0.90f)] [TestCase(20, 0.90f)]
        [TestCase(21, 0.75f)] [TestCase(40, 0.75f)]
        [TestCase(41, 0.50f)] [TestCase(9999, 0.50f)]
        public void Mastery_Multiplier_Bands(int mastery, float expected)
            => Assert.AreEqual(expected, XpCalculator.MasteryMultiplier(mastery), 1e-4f);

        // ---- 19: pre-answer mastery --------------------------------------------
        [Test]
        public void Answer_Xp_Uses_Pre_Answer_Mastery_Not_The_Updated_Score()
        {
            // Mastery 2 (×1.50) → 10×1.5 = 15. If it used the post-answer mastery (2→4,
            // ×1.25) it would be 13. Emulate the gameplay order: read pre, award, apply.
            MasteryService.Progress.learning.kanjiMastery.SetScore(KanjiId.Of("水"), 2);
            int pre = MasteryService.GetScore("水");
            int awarded = XpService.AwardAnswerXp(AnswerMode.English, TimerMode.Untimed, pre, true);
            MasteryService.RecordAttempt("水", AnswerMode.English, TimerMode.Untimed, true); // mastery 2→4

            Assert.AreEqual(15, awarded, "used pre-answer ×1.50, not post-answer ×1.25 (=13)");
            Assert.AreEqual(15, XpService.TotalXp);
            Assert.AreEqual(4, MasteryService.GetScore("水"), "mastery still advanced");
        }

        // ---- 20/21: wrong answers ----------------------------------------------
        [Test]
        public void Wrong_Answer_Awards_Zero_Xp()
        {
            int awarded = XpService.AwardAnswerXp(AnswerMode.Kana, TimerMode.Timed, 0, false);
            Assert.AreEqual(0, awarded);
            Assert.AreEqual(0, XpService.TotalXp);
        }

        [Test]
        public void Wrong_Answer_Still_Updates_Mastery()
        {
            MasteryService.Progress.learning.kanjiMastery.SetScore(KanjiId.Of("山"), 8);
            XpService.AwardAnswerXp(AnswerMode.English, TimerMode.Untimed, 8, false); // 0 XP
            int newScore = MasteryService.RecordAttempt("山", AnswerMode.English, TimerMode.Untimed, false);
            Assert.AreEqual(0, XpService.TotalXp);
            Assert.AreNotEqual(8, newScore, "mastery changed on a wrong answer");
        }

        // ---- 22/23/24: session bonus -------------------------------------------
        [Test]
        public void Completed_Normal_Session_Awards_50()
        {
            XpService.AwardForCompletedSession(sessionCounted: true, firstActivityToday: false, currentStreak: 0);
            Assert.AreEqual(50, XpService.TotalXp);
        }

        [Test]
        public void Incomplete_Session_Awards_No_Session_Xp()
        {
            // A run that never completes never calls the award seam.
            Assert.AreEqual(0, XpService.TotalXp);
            XpService.AwardForCompletedSession(sessionCounted: false, firstActivityToday: false, currentStreak: 0);
            Assert.AreEqual(0, XpService.TotalXp);
        }

        [Test]
        public void Session_Bonus_Cannot_Be_Awarded_Twice_For_One_Completion()
        {
            // First completion counts; a duplicate completion reports sessionCounted=false.
            XpService.AwardForCompletedSession(true, false, 0);
            XpService.AwardForCompletedSession(false, false, 0); // duplicate / re-entry
            Assert.AreEqual(50, XpService.TotalXp);
        }

        // ---- 25/26/27/28: Revision ---------------------------------------------
        [Test]
        public void Revision_Correct_Answer_Awards_Answer_Xp()
        {
            // Revision is untimed; a correct English answer at mastery 7 (×1.0) = 10.
            int awarded = XpService.AwardAnswerXp(AnswerMode.English, TimerMode.Untimed, 7, true);
            Assert.AreEqual(10, awarded);
        }

        [Test]
        public void Revision_Wrong_Answer_Awards_Zero_Xp()
            => Assert.AreEqual(0, XpService.AwardAnswerXp(AnswerMode.Romaji, TimerMode.Untimed, 0, false));

        [Test]
        public void Revision_Awards_No_Session_Or_Streak_Xp()
        {
            // Revision → sessionCounted=false, even if it is the first activity today.
            XpService.AwardForCompletedSession(sessionCounted: false, firstActivityToday: true, currentStreak: 5);
            Assert.AreEqual(0, XpService.TotalXp);
        }

        // ---- 29–33: streak bonus -----------------------------------------------
        [TestCase(1, 10)] [TestCase(5, 50)] [TestCase(10, 100)] [TestCase(20, 100)]
        public void Streak_Bonus_Values(int streakDays, int expected)
            => Assert.AreEqual(expected, XpCalculator.StreakBonusXp(streakDays));

        [Test]
        public void Streak_Bonus_Awarded_Only_Once_Per_Day()
        {
            XpService.AwardForCompletedSession(true, firstActivityToday: true, currentStreak: 3);  // +50 +30
            XpService.AwardForCompletedSession(true, firstActivityToday: false, currentStreak: 3); // +50 only
            Assert.AreEqual(50 + 30 + 50, XpService.TotalXp, "streak bonus not repeated same day");
        }

        // ---- 34–37: persistence -------------------------------------------------
        [Test]
        public void TotalXp_And_Version_Persist_Through_PlayerProgress()
        {
            var store = new LocalPlayerProgressStore(_path);
            var p = new PlayerProgress();
            p.progression.xp.totalXp = 275;
            store.Save(p);

            var loaded = store.Load();
            Assert.AreEqual(275, loaded.progression.xp.totalXp, "TotalXP restored");
            Assert.AreEqual(XpConfig.CurrentVersion, loaded.progression.xp.xpSystemVersion, "version restored");
        }

        [Test]
        public void Xp_Is_Never_Negative()
        {
            Assert.AreEqual(0, XpCalculator.StreakBonusXp(-5), "no negative streak bonus");
            XpService.AwardAnswerXp(AnswerMode.English, TimerMode.Timed, 0, false); // wrong → 0
            XpService.AwardDailyStreakXp(0);                                        // 0
            Assert.GreaterOrEqual(XpService.TotalXp, 0);
            Assert.AreEqual(0, XpService.TotalXp);
        }

        // ---- 38/39: versioning --------------------------------------------------
        [Test]
        public void New_Progress_Starts_At_Xp_Version_1()
            => Assert.AreEqual(1, new PlayerProgress().progression.xp.xpSystemVersion);

        [Test]
        public void Loading_And_Saving_Does_Not_Modify_Existing_Xp()
        {
            // A legacy nested save (no xpSystemVersion) with existing XP.
            File.WriteAllText(_path,
                "{\"schemaVersion\":4,\"progression\":{\"xp\":{\"totalXp\":123}}}");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(123, loaded.progression.xp.totalXp, "existing XP untouched");
            Assert.AreEqual(XpConfig.CurrentVersion, loaded.progression.xp.xpSystemVersion, "version stamped, total unchanged");
        }

        // ---- 40–43: separation --------------------------------------------------
        [Test]
        public void Awarding_Xp_Does_Not_Change_Kanji_Mastery()
        {
            MasteryService.Progress.learning.kanjiMastery.SetScore(KanjiId.Of("水"), 5);
            XpService.AwardAnswerXp(AnswerMode.Kana, TimerMode.Timed, 5, true);
            Assert.AreEqual(5, MasteryService.GetScore("水"), "XP award left mastery untouched");
            Assert.Greater(XpService.TotalXp, 0);
        }

        [Test]
        public void Xp_Does_Not_Unlock_Levels()
        {
            for (int i = 0; i < 50; i++) XpService.AwardSessionCompletionXp(); // lots of XP
            Assert.IsFalse(LevelProgressionService.IsUnlocked(XpService.Progress, PlayerLevel.YoungMaster),
                "levels are mastery-gated, not XP-gated");
        }

        [Test]
        public void Question_Generation_Does_Not_Award_Xp()
        {
            var pool = KanjiMaster.Kanji.QuestionPool.Load(KanjiLevel.N5);
            var gen = new KanjiMaster.Kanji.QuestionGenerator(pool.Database, new System.Random(1));
            var kanji = pool.SelectUnique(5, new System.Random(1));
            foreach (var k in kanji) gen.Build(k, AnswerMode.English);
            Assert.AreEqual(0, XpService.TotalXp);
        }

        [Test]
        public void Ui_Rendering_Does_Not_Award_Xp()
        {
            var s = new GameSession { IsRevision = true };
            var _ = ResultsPresenter.For(s); // rendering decision only
            Assert.AreEqual(0, XpService.TotalXp);
        }
    }
}
