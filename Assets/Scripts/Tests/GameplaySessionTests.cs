using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KanjiMaster.Core;
using KanjiMaster.Kanji;
using Data = KanjiRush.Data;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Data-driven tests for the gameplay/session layer: question uniqueness, the
    /// three answer modes, question correctness, session results, and revision.
    /// No mastery/XP behaviour is tested (none exists yet).
    /// </summary>
    public class GameplaySessionTests
    {
        private QuestionPool _pool;   // N5
        private QuestionGenerator _gen;

        [SetUp]
        public void SetUp()
        {
            _pool = QuestionPool.Load(KanjiLevel.N5);
            _gen = new QuestionGenerator(_pool.Database, new System.Random(12345));
        }

        // ---- Question uniqueness ------------------------------------------------
        [Test]
        public void Normal_Run_Selects_15_Unique_Kanji()
        {
            var selected = _pool.SelectUnique(15, new System.Random(1));
            Assert.AreEqual(15, selected.Count);
            Assert.AreEqual(15, selected.Select(k => k.Character).Distinct().Count());
        }

        [Test]
        public void Generated_Sequence_Has_No_Repeated_Kanji()
        {
            var selected = _pool.SelectUnique(15, new System.Random(2));
            var questions = selected.Select(k => _gen.Build(k, AnswerMode.English)).ToList();
            Assert.AreEqual(15, questions.Select(q => q.KanjiCharacter).Distinct().Count());
        }

        // ---- Answer modes -------------------------------------------------------
        [Test]
        public void Answer_Modes_Use_The_Correct_Canonical_Field()
        {
            var mizu = _pool.Database.ByChar("水");
            Assert.AreEqual(mizu.PrimaryMeaning, _gen.Build(mizu, AnswerMode.English).CorrectAnswer);
            Assert.AreEqual(mizu.Romaji, _gen.Build(mizu, AnswerMode.Romaji).CorrectAnswer);
            Assert.AreEqual(mizu.PrimaryReading, _gen.Build(mizu, AnswerMode.Kana).CorrectAnswer);
            // The three modes yield genuinely different correct answers.
            Assert.AreNotEqual(_gen.Build(mizu, AnswerMode.English).CorrectAnswer,
                               _gen.Build(mizu, AnswerMode.Kana).CorrectAnswer);
            Assert.AreNotEqual(_gen.Build(mizu, AnswerMode.Romaji).CorrectAnswer,
                               _gen.Build(mizu, AnswerMode.Kana).CorrectAnswer);
        }

        // ---- Question correctness (every kanji, every mode, both levels) --------
        [Test]
        public void Every_Question_Has_Exactly_One_Correct_Option_In_The_Selected_Mode()
        {
            foreach (var level in new[] { KanjiLevel.N5, KanjiLevel.N4 })
            {
                var pool = QuestionPool.Load(level);
                var gen = new QuestionGenerator(pool.Database, new System.Random(7));
                foreach (var k in pool.Database.All)
                foreach (var mode in new[] { AnswerMode.English, AnswerMode.Romaji, AnswerMode.Kana })
                {
                    var q = gen.Build(k, mode);
                    Assert.AreEqual(4, q.Options.Length, $"{k.Character}/{mode}: not 4 options");
                    Assert.AreEqual(4, q.Options.Distinct().Count(), $"{k.Character}/{mode}: options not distinct");
                    string expected = QuestionGenerator.ValueFor(k, mode);
                    Assert.AreEqual(expected, q.CorrectAnswer, $"{k.Character}/{mode}: wrong correct answer");
                    Assert.AreEqual(1, q.Options.Count(o => o == expected), $"{k.Character}/{mode}: correct value not unique");
                    Assert.AreEqual(expected, q.Options[q.CorrectIndex], $"{k.Character}/{mode}: correctIndex mismatch");
                }
            }
        }

        // ---- Session results ----------------------------------------------------
        [Test]
        public void Session_Aggregates_Correct_Incorrect_Total_Accuracy_Mistakes()
        {
            var s = MakeSession(AnswerMode.Kana, TimerMode.Timed, KanjiLevel.N5,
                ("水", true), ("火", false), ("木", true), ("金", false), ("土", true));
            Assert.AreEqual(5, s.TotalQuestions);
            Assert.AreEqual(3, s.CorrectCount);
            Assert.AreEqual(2, s.IncorrectCount);
            Assert.AreEqual(0.6f, s.Accuracy, 0.0001f);
            CollectionAssert.AreEqual(new[] { "火", "金" }, s.IncorrectKanji());
        }

        // ---- Revision -----------------------------------------------------------
        [Test]
        public void Revision_Is_Untimed_Preserves_Mode_And_Contains_Exactly_The_Mistakes()
        {
            var original = MakeSession(AnswerMode.Romaji, TimerMode.Timed, KanjiLevel.N4,
                ("会", true), ("私", false), ("空", false), ("立", true), ("花", false));

            var revConfig = Revision.ConfigFor(original);
            Assert.AreEqual(TimerMode.Untimed, revConfig.Timer, "revision must be untimed");
            Assert.AreEqual(AnswerMode.Romaji, revConfig.Answer, "revision must preserve AnswerMode");
            Assert.AreEqual(KanjiLevel.N4, revConfig.Level, "revision must preserve Level");

            var revKanji = Revision.KanjiFor(original);
            CollectionAssert.AreEqual(new[] { "私", "空", "花" }, revKanji);
            Assert.AreEqual(original.IncorrectCount, revKanji.Count);

            // A revision pool built from those chars has exactly the mistake count.
            var pool = QuestionPool.Load(KanjiLevel.N4);
            Assert.AreEqual(3, pool.SelectByChars(revKanji).Count);
        }

        [Test]
        public void Revision_Does_Not_Mutate_The_Original_Session()
        {
            var original = MakeSession(AnswerMode.Kana, TimerMode.Timed, KanjiLevel.N5,
                ("水", false), ("火", true), ("木", false));
            int resultsBefore = original.Results.Count;
            var timerBefore = original.Config.Timer;

            SessionContext.QueueRevision(original);

            Assert.AreEqual(resultsBefore, original.Results.Count, "original results changed");
            Assert.AreEqual(timerBefore, original.Config.Timer, "original timer changed");
            Assert.AreEqual(TimerMode.Untimed, SessionContext.NextConfig.Timer);
            Assert.IsTrue(SessionContext.NextIsRevision);
            Assert.AreEqual(2, SessionContext.NextKanji.Count);
        }

        // ---- Feedback timing ----------------------------------------------------
        [Test]
        public void Feedback_Durations_Are_Config_Driven_And_Wrong_Is_Longer()
        {
            var ft = new FeedbackTiming();
            Assert.AreEqual(ft.correctDuration, ft.For(true));
            Assert.AreEqual(ft.wrongDuration, ft.For(false));
            Assert.Greater(ft.wrongDuration, ft.correctDuration);
            float ratio = ft.wrongDuration / ft.correctDuration;
            Assert.That(ratio, Is.InRange(1.5f, 2.0f), "wrong feedback should be ~1.5–2× correct");
        }

        // ---- helper -------------------------------------------------------------
        private static GameSession MakeSession(AnswerMode mode, TimerMode timer, KanjiLevel level,
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
    }
}
