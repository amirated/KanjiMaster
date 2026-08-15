using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Unit tests for persistent mastery scoring: the generic transition (every
    /// profile), the parity/positivity invariants, profile resolution, and
    /// persistence. No mastery UI / XP / selection is exercised (none exists).
    /// </summary>
    public class MasteryScoringTests
    {
        // ---- Transition tables per profile (correctPoints, wrongPenalty) --------
        [Test]
        public void Profile_2_1_Transitions()
        {
            AssertCorrect(2, (0, 2), (2, 4), (4, 6), (1, 2), (3, 4), (5, 6));
            AssertWrong(1, (0, 0), (2, 1), (4, 3), (6, 5), (1, 1), (3, 1), (5, 3));
        }

        [Test]
        public void Profile_4_3_Transitions()
        {
            AssertCorrect(4, (0, 4), (2, 6), (6, 10), (1, 2), (3, 4), (7, 8));
            // includes the prompt's 5 -> 1, and the author-uncertain 7 -> 3 (invariant-correct)
            AssertWrong(3, (0, 0), (4, 1), (8, 5), (2, 1), (1, 1), (3, 1), (5, 1), (7, 3));
        }

        [Test]
        public void Profile_8_5_Transitions()
        {
            AssertCorrect(8, (0, 8), (2, 10), (6, 14), (1, 2), (3, 4), (7, 8));
            AssertWrong(5, (0, 0), (8, 3), (10, 5), (12, 7), (2, 1), (1, 1), (3, 1), (5, 1), (7, 1), (9, 3));
        }

        [Test]
        public void Profile_16_9_Transitions()
        {
            AssertCorrect(16, (0, 16), (2, 18), (14, 30), (1, 2), (3, 4), (15, 16));
            AssertWrong(9, (0, 0), (16, 7), (18, 9), (20, 11), (2, 1), (1, 1), (3, 1), (9, 1), (11, 1), (13, 3));
        }

        // ---- Critical invariants (fuzzed across all profiles) -------------------
        [Test]
        public void Invariants_Hold_For_All_Profiles_And_Scores()
        {
            (int cp, int wp)[] profiles = { (2, 1), (4, 3), (8, 5), (16, 9) };
            foreach (var (cp, wp) in profiles)
            {
                for (int score = 0; score <= 300; score++)
                {
                    int right = MasteryScoring.Apply(score, cp, wp, true);
                    Assert.GreaterOrEqual(right, 0, "never negative");
                    Assert.AreEqual(0, right % 2, $"correct → even (score {score}, +{cp})");

                    int wrong = MasteryScoring.Apply(score, cp, wp, false);
                    Assert.GreaterOrEqual(wrong, 0, "never negative");
                    if (score > 0 && wrong > 0)
                        Assert.AreEqual(1, wrong % 2, $"wrong (positive) → odd (score {score}, -{wp})");
                }
                Assert.AreEqual(0, MasteryScoring.Apply(0, cp, wp, false), "0 → wrong → 0");
                Assert.AreEqual(1, MasteryScoring.Apply(1, cp, wp, false), "1 → wrong → 1");
            }
        }

        // ---- Profile resolution --------------------------------------------------
        [Test]
        public void Resolver_Maps_Modes_To_Profiles()
        {
            AssertProfile(AnswerMode.English, TimerMode.Untimed, 2, 1);
            AssertProfile(AnswerMode.English, TimerMode.Timed, 4, 3);
            AssertProfile(AnswerMode.Romaji, TimerMode.Untimed, 4, 3);
            AssertProfile(AnswerMode.Romaji, TimerMode.Timed, 8, 5);
            AssertProfile(AnswerMode.Kana, TimerMode.Untimed, 8, 5);
            AssertProfile(AnswerMode.Kana, TimerMode.Timed, 16, 9);
        }

        [Test]
        public void Revision_Uses_Untimed_Profile_Of_Inherited_Mode()
        {
            // Original Timed+Kana → revision is Untimed+Kana → +8/-5 (not the timed +16/-9).
            var original = new GameSession { Config = new GameConfig { Answer = AnswerMode.Kana, Timer = TimerMode.Timed } };
            var revision = Revision.ConfigFor(original);
            var profile = ScoringProfileResolver.Resolve(revision.Answer, revision.Timer);
            Assert.AreEqual(8, profile.CorrectPoints);
            Assert.AreEqual(5, profile.WrongPenalty);
        }

        // ---- PlayerProgress model + independence --------------------------------
        [Test]
        public void New_Kanji_Start_At_Zero_And_Scores_Are_Independent()
        {
            var p = new PlayerProgress();
            Assert.AreEqual(0, p.GetScore("水"));
            p.SetScore("水", 8);
            Assert.AreEqual(8, p.GetScore("水"));
            Assert.AreEqual(0, p.GetScore("山"), "unset kanji still zero");
            p.SetScore("山", 3);
            Assert.AreEqual(8, p.GetScore("水"), "updating 山 must not affect 水");
            Assert.AreEqual(3, p.GetScore("山"));
        }

        [Test]
        public void Progress_Survives_Json_RoundTrip()
        {
            var p = new PlayerProgress { SessionsPlayed = 4 };
            p.SetScore("水", 12);
            p.SetScore("日", 24);

            string json = JsonUtility.ToJson(p);
            var loaded = JsonUtility.FromJson<PlayerProgress>(json);
            loaded.InvalidateIndex();

            Assert.AreEqual(12, loaded.GetScore("水"));
            Assert.AreEqual(24, loaded.GetScore("日"));
            Assert.AreEqual(4, loaded.SessionsPlayed);
            Assert.AreEqual(0, loaded.GetScore("学"), "unseen kanji still zero after reload");
        }

        [Test]
        public void LocalStore_Save_Then_Load_Round_Trips_From_Disk()
        {
            string path = Path.Combine(Application.persistentDataPath,
                "test_progress_" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new LocalPlayerProgressStore(path);
                var p = new PlayerProgress();
                p.SetScore("学", 1);
                p.SetScore("水", 8);
                store.Save(p);

                var loaded = store.Load();   // simulates app restart
                Assert.AreEqual(1, loaded.GetScore("学"));
                Assert.AreEqual(8, loaded.GetScore("水"));
                Assert.AreEqual(0, loaded.GetScore("山"));
            }
            finally
            {
                foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                    if (File.Exists(path + ext)) File.Delete(path + ext);
            }
        }

        // ---- MasteryService (in-memory, no disk) --------------------------------
        [SetUp]
        public void IsolateService()
        {
            MasteryService.Progress = new PlayerProgress();
            MasteryService.Persist = _ => { }; // no disk writes in tests
        }

        [Test]
        public void Service_Applies_Transition_And_Keeps_Kanji_Independent()
        {
            // Kana + Untimed → +8/-5
            Assert.AreEqual(8, MasteryService.RecordAttempt("水", AnswerMode.Kana, TimerMode.Untimed, true));
            Assert.AreEqual(3, MasteryService.RecordAttempt("水", AnswerMode.Kana, TimerMode.Untimed, false)); // 8-5=3
            Assert.AreEqual(0, MasteryService.GetScore("山"), "independent kanji unaffected");

            // Original wrong then revision correct both affect 山 independently.
            Assert.AreEqual(0, MasteryService.RecordAttempt("山", AnswerMode.English, TimerMode.Timed, false)); // 0→0
            Assert.AreEqual(2, MasteryService.RecordAttempt("山", AnswerMode.English, TimerMode.Untimed, true)); // 0→+2 (revision)
        }

        [Test]
        public void Service_Counts_Sessions()
        {
            Assert.AreEqual(0, MasteryService.Progress.SessionsPlayed);
            MasteryService.RegisterSessionPlayed();
            MasteryService.RegisterSessionPlayed();
            Assert.AreEqual(2, MasteryService.Progress.SessionsPlayed);
        }

        // ---- helpers ------------------------------------------------------------
        private static void AssertCorrect(int correctPoints, params (int from, int to)[] cases)
        {
            foreach (var (from, to) in cases)
                Assert.AreEqual(to, MasteryScoring.Apply(from, correctPoints, 0, true),
                    $"+{correctPoints} correct: {from} → expected {to}");
        }

        private static void AssertWrong(int wrongPenalty, params (int from, int to)[] cases)
        {
            foreach (var (from, to) in cases)
                Assert.AreEqual(to, MasteryScoring.Apply(from, 0, wrongPenalty, false),
                    $"-{wrongPenalty} wrong: {from} → expected {to}");
        }

        private static void AssertProfile(AnswerMode a, TimerMode t, int cp, int wp)
        {
            var p = ScoringProfileResolver.Resolve(a, t);
            Assert.AreEqual(cp, p.CorrectPoints, $"{a}/{t} correctPoints");
            Assert.AreEqual(wp, p.WrongPenalty, $"{a}/{t} wrongPenalty");
        }
    }
}
