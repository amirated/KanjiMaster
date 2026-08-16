using System;
using System.Linq;
using NUnit.Framework;
using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the final V1 Revision behaviour: the exact mistake list (deduped),
    /// untimed + inherited answer mode, bypassing normal selection, the "Well Done!"
    /// Results presentation, navigation (no recursive revision, clean next normal
    /// game), the empty-request guard, and separation from sessions/streak/level
    /// progression. Answer-mode/timer inheritance across all modes is also covered.
    /// </summary>
    public class RevisionTests
    {
        private static GameSession Session(AnswerMode mode, TimerMode timer, KanjiLevel level,
            params (string kanji, bool correct)[] items)
        {
            var s = new GameSession { Config = new GameConfig { Answer = mode, Timer = timer, Level = level } };
            foreach (var (kanji, correct) in items)
                s.Results.Add(new QuestionResult
                {
                    KanjiCharacter = kanji,
                    CorrectAnswer = "x",
                    SelectedAnswer = correct ? "x" : "y",
                    IsCorrect = correct,
                });
            return s;
        }

        // ---- 1/2: Revise exposure via presenter --------------------------------
        [Test]
        public void Zero_Mistakes_Does_Not_Expose_Revise()
        {
            var s = Session(AnswerMode.English, TimerMode.Timed, KanjiLevel.N5, ("山", true), ("水", true));
            var view = ResultsPresenter.For(s);
            Assert.IsFalse(view.ShowReviseButton);
            Assert.IsTrue(view.ShowNumericResults, "normal run keeps numeric results");
        }

        [Test]
        public void One_Or_More_Mistakes_Exposes_Revise_With_Numeric_Results()
        {
            var s = Session(AnswerMode.English, TimerMode.Timed, KanjiLevel.N5, ("山", true), ("水", false));
            var view = ResultsPresenter.For(s);
            Assert.IsTrue(view.ShowReviseButton);
            Assert.IsTrue(view.ShowNumericResults);
        }

        // ---- 3/4/5: exact mistake set ------------------------------------------
        [Test]
        public void Revision_Contains_Exactly_The_Incorrect_Kanji_And_Nothing_Else()
        {
            var s = Session(AnswerMode.English, TimerMode.Untimed, KanjiLevel.N5,
                ("山", true), ("水", false), ("木", true), ("火", false), ("日", false));
            var list = Revision.KanjiFor(s);
            CollectionAssert.AreEqual(new[] { "水", "火", "日" }, list, "exact mistakes, in order");
            CollectionAssert.DoesNotContain(list, "山");
            CollectionAssert.DoesNotContain(list, "木");
        }

        // ---- 6: no duplicates ---------------------------------------------------
        [Test]
        public void Revision_Deduplicates_A_Repeated_Mistake()
        {
            // Defensive: even if a kanji appears wrong twice upstream, it appears once.
            var s = Session(AnswerMode.English, TimerMode.Untimed, KanjiLevel.N5,
                ("水", false), ("火", false), ("水", false));
            var list = Revision.KanjiFor(s);
            CollectionAssert.AreEqual(new[] { "水", "火" }, list);
            Assert.AreEqual(list.Count, list.Distinct().Count());
        }

        // ---- 7/8: untimed + answer-mode inheritance (all modes, both timers) ----
        [Test]
        public void Revision_Is_Always_Untimed_And_Preserves_Answer_Mode_And_Level()
        {
            foreach (var mode in new[] { AnswerMode.English, AnswerMode.Romaji, AnswerMode.Kana })
            foreach (var timer in new[] { TimerMode.Timed, TimerMode.Untimed })
            {
                var s = Session(mode, timer, KanjiLevel.N4, ("私", false));
                var cfg = Revision.ConfigFor(s);
                Assert.AreEqual(TimerMode.Untimed, cfg.Timer, $"{mode}/{timer} → untimed");
                Assert.AreEqual(mode, cfg.Answer, "answer mode inherited");
                Assert.AreEqual(KanjiLevel.N4, cfg.Level, "level preserved");
            }
        }

        // ---- 9: untimed mastery scoring profile --------------------------------
        [Test]
        public void Revision_Uses_Untimed_Scoring_Profile_For_Each_Mode()
        {
            var cases = new[]
            {
                (AnswerMode.English, 2, -1),
                (AnswerMode.Romaji, 4, -3),
                (AnswerMode.Kana, 8, -5),
            };
            foreach (var (mode, correct, wrong) in cases)
            {
                var cfg = Revision.ConfigFor(Session(mode, TimerMode.Timed, KanjiLevel.N5, ("水", false)));
                var profile = ScoringProfileResolver.Resolve(cfg.Answer, cfg.Timer); // Untimed
                Assert.AreEqual(correct, profile.CorrectPoints, $"{mode} untimed correct");
                Assert.AreEqual(wrong, profile.WrongPenalty, $"{mode} untimed wrong");
            }
        }

        // ---- 10/11: bypasses normal (mastery-independent) selection ------------
        [Test]
        public void Revision_List_Is_Independent_Of_Mastery_Scores()
        {
            var s = Session(AnswerMode.English, TimerMode.Untimed, KanjiLevel.N5,
                ("水", false), ("火", false));
            // Whatever the player's mastery is, the revision list is the exact mistakes.
            MasteryService.Progress = new PlayerProgress();
            MasteryService.Persist = _ => { };
            MasteryService.Progress.learning.kanjiMastery.SetScore(KanjiId.Of("水"), 16);
            MasteryService.Progress.learning.kanjiMastery.SetScore(KanjiId.Of("火"), 0);

            var list = Revision.KanjiFor(s);
            CollectionAssert.AreEqual(new[] { "水", "火" }, list, "mastery does not reorder or filter");
        }

        // ---- 12/13: not a normal session; no streak effect ---------------------
        [Test]
        public void Revision_Is_Not_A_Normal_Completed_Session()
        {
            ActivityService.Progress = new PlayerProgress();
            ActivityService.Persist = _ => { };
            ActivityService.ResetSessionGuard();

            var revision = new GameSession { IsRevision = true };
            bool counted = ActivityService.RecordNormalSessionCompleted(revision);

            Assert.IsFalse(counted);
            Assert.AreEqual(0, ActivityService.TotalSessions, "no session credit");
            Assert.AreEqual(0, ActivityService.CurrentStreak, "streak untouched");
            Assert.AreEqual(0, ActivityService.LongestStreak);
        }

        // ---- 14: does not count as a normal game for progression ---------------
        [Test]
        public void Revision_Does_Not_Give_Normal_Session_Credit_That_Progression_Uses()
        {
            // Level progression is mastery-based and never reads a "revision counted as a
            // normal game" flag — the guard is that revision yields no normal-session credit.
            ActivityService.Progress = new PlayerProgress();
            ActivityService.Persist = _ => { };
            ActivityService.ResetSessionGuard();

            ActivityService.RecordNormalSessionCompleted(new GameSession { IsRevision = true });
            Assert.AreEqual(0, ActivityService.Progress.activity.sessions.totalSessions);
        }

        // ---- 15/16/17/18/19: Revision Results presentation ---------------------
        [Test]
        public void Revision_Results_Show_Well_Done_And_No_Numeric_Results()
        {
            var revision = new GameSession { IsRevision = true, Score = 999 };
            revision.Results.Add(new QuestionResult { KanjiCharacter = "水", IsCorrect = true });
            var view = ResultsPresenter.For(revision);

            Assert.AreEqual("Well Done!", view.Title);
            Assert.IsFalse(view.ShowNumericResults, "no score/correct/incorrect/%/summary");
            Assert.IsFalse(view.ShowReviseButton, "no Revise on revision results (no recursion)");
            Assert.IsTrue(view.ShowPlayAgain);
            Assert.IsTrue(view.ShowMainMenu);
        }

        // ---- 20: Play Again from revision is not another revision --------------
        [Test]
        public void Play_Again_From_Revision_Starts_A_Normal_Game_Not_Revision()
        {
            var normal = Session(AnswerMode.Romaji, TimerMode.Untimed, KanjiLevel.N4, ("会", false));
            SessionContext.QueueNormal(normal.Config);          // establishes LastNormalConfig
            SessionContext.QueueRevision(normal);               // now in revision
            Assert.IsTrue(SessionContext.NextIsRevision);

            SessionContext.QueueReplayNormal();                 // "Play Again" from revision
            Assert.IsFalse(SessionContext.NextIsRevision, "not a revision");
            Assert.IsNull(SessionContext.NextKanji, "normal random selection, not the mistake list");
            Assert.AreEqual(AnswerMode.Romaji, SessionContext.NextConfig.Answer, "keeps normal config");
            Assert.AreEqual(TimerMode.Untimed, SessionContext.NextConfig.Timer);
        }

        // ---- 21/22: clean state / no mistake leak ------------------------------
        [Test]
        public void Starting_A_New_Normal_Game_Clears_Any_Revision_State()
        {
            var gameA = Session(AnswerMode.English, TimerMode.Timed, KanjiLevel.N5, ("水", false), ("火", false));
            SessionContext.QueueRevision(gameA);
            Assert.IsNotNull(SessionContext.NextKanji);

            SessionContext.QueueNormal(new GameConfig { Answer = AnswerMode.Kana, Timer = TimerMode.Timed, Level = KanjiLevel.N4 });
            Assert.IsNull(SessionContext.NextKanji, "game B does not inherit game A's mistakes");
            Assert.IsFalse(SessionContext.NextIsRevision);
        }

        // ---- 23: abandoning revision does not touch the preceding session ------
        [Test]
        public void Queuing_Revision_Does_Not_Mutate_Preceding_Normal_State()
        {
            var normal = Session(AnswerMode.Kana, TimerMode.Timed, KanjiLevel.N5, ("水", false));
            SessionContext.QueueNormal(normal.Config);
            var lastNormalBefore = SessionContext.LastNormalConfig.Timer;

            SessionContext.QueueRevision(normal); // start revision (may be abandoned)

            Assert.AreEqual(lastNormalBefore, SessionContext.LastNormalConfig.Timer,
                "LastNormalConfig unchanged — Play Again still returns to the normal game");
            Assert.AreEqual(TimerMode.Timed, normal.Config.Timer, "original session not mutated");
        }

        // ---- 24: empty revision request handled safely -------------------------
        [Test]
        public void Empty_Revision_Request_Is_Refused_Not_Started()
        {
            var noMistakes = Session(AnswerMode.English, TimerMode.Timed, KanjiLevel.N5, ("山", true), ("水", true));
            SessionContext.NextIsRevision = false;
            SessionContext.NextKanji = null;

            bool queued = SessionContext.QueueRevision(noMistakes);

            Assert.IsFalse(queued, "no mistakes → not queued");
            Assert.IsFalse(SessionContext.NextIsRevision, "no fake revision session started");
        }
    }
}
