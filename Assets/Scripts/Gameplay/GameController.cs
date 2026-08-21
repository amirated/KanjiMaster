using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KanjiMaster.Core;
using KanjiMaster.Kanji;
using KanjiMaster.Progression;
using KanjiMaster.Services;
using KanjiMaster.UI;
using UnityEngine;
using Data = KanjiRush.Data;

namespace KanjiMaster.Gameplay
{
    /// <summary>
    /// Drives one run (normal or revision). Builds the full question sequence ONCE
    /// up front from a unique set of kanji, then scores answers, tracks combo, runs
    /// the per-question timer (Timed only), records a <see cref="QuestionResult"/>
    /// per question, and publishes a completed <see cref="GameSession"/> to
    /// <see cref="SessionContext"/> before navigating to Results.
    ///
    /// Records correctness/timing only — no mastery or XP. All gameplay rules live
    /// here; <see cref="GameHUDView"/> only presents state.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameHUDView view;

        [Header("Run")]
        [SerializeField] private int questionsPerRun = 15;
        [SerializeField] private float questionTimeSeconds = 5f;

        [Header("Feedback (configurable durations)")]
        [SerializeField] private FeedbackTiming feedback = new FeedbackTiming();

        [Header("Scoring")]
        [SerializeField] private int basePoints = 100;
        [SerializeField] private int maxSpeedBonus = 100;
        [SerializeField] private float comboStep = 0.25f;
        [SerializeField] private float maxComboMultiplier = 4f;

        private static readonly Color GoodColor = new Color(0.30f, 0.78f, 0.45f);
        private static readonly Color BadColor = new Color(0.85f, 0.32f, 0.34f);

        private GameConfig _config;
        private GameSession _session;
        private List<Question> _questions;
        private Data.KanjiDatabase _database; // this run's dataset (for revision feedback lookups)

        private int _index;
        private int _combo;
        private int _maxCombo;
        private int _score;
        private float _remaining;
        private float _shownAt;
        private bool _running;
        private bool _accepting;
        private bool _finished;

        private void Start()
        {
            if (view == null)
            {
                Debug.LogError("GameController: GameHUDView reference not assigned.");
                return;
            }

            _config = (SessionContext.NextConfig ?? new GameConfig()).Clone();

            try
            {
                BuildQuestionSequence();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GameController: {e.Message}");
                view.SetQuestion("!");
                view.SetFeedback("Could not start run: " + e.Message, BadColor);
                return;
            }

            _session = new GameSession { Config = _config.Clone(), IsRevision = SessionContext.NextIsRevision };

            view.AnswerSelected += OnAnswerSelected;
            view.SetTimerVisible(_config.Timer == TimerMode.Timed);

            StartRun();
        }

        private void OnDestroy()
        {
            if (view != null) view.AnswerSelected -= OnAnswerSelected;
        }

        private void Update()
        {
            if (!_running || !_accepting) return;
            if (_config.Timer != TimerMode.Timed) return;

            _remaining -= Time.deltaTime;
            view.SetTimerNormalized(_remaining / questionTimeSeconds);
            if (_remaining <= 0f) Resolve(-1); // timeout = wrong
        }

        /// <summary>Select the kanji (unique 15, or the revision set) and generate
        /// the whole sequence once, guaranteeing no repeats within the run.</summary>
        private void BuildQuestionSequence()
        {
            var rng = new System.Random();
            var pool = QuestionPool.Load(_config.Level);
            _database = pool.Database; // kept so revision feedback can look up meaning/romaji/kana
            var generator = new QuestionGenerator(pool.Database, rng);

            // Level eligibility first (pool is this level's dataset), then mastery
            // weighting — weighting can never cross the level boundary. Revision keeps
            // its exact mistake set (unchanged).
            List<Data.Kanji> kanji = SessionContext.NextKanji != null
                ? pool.SelectByChars(SessionContext.NextKanji)   // revision: exactly the mistakes
                : MasteryWeightedSelector.SelectUnique(          // normal: ≤15 unique, weighted
                    pool.Database.All, questionsPerRun,
                    k => MasteryService.GetScore(k.Character), rng);

            if (kanji.Count == 0)
                throw new System.InvalidOperationException("no kanji available for this level");

            _questions = kanji.Select(k => generator.Build(k, _config.Answer)).ToList();
        }

        private void StartRun()
        {
            _index = 0;
            _combo = 0;
            _maxCombo = 0;
            _score = 0;
            _running = true;

            view.SetScore(0);
            view.SetCombo(0);
            NextQuestion();
        }

        private void NextQuestion()
        {
            if (_index >= _questions.Count)
            {
                FinishRun();
                return;
            }

            var q = _questions[_index];
            view.SetQuestion(q.Prompt);
            view.SetOptions(q.Options);
            view.ClearFeedback();
            view.SetAnswersInteractable(true);

            _remaining = questionTimeSeconds;
            if (_config.Timer == TimerMode.Timed) view.SetTimerNormalized(1f);
            _shownAt = Time.time;
            _accepting = true;
        }

        private void OnAnswerSelected(int index)
        {
            if (!_running || !_accepting) return;
            Resolve(index);
        }

        private void Resolve(int selectedIndex)
        {
            _accepting = false;
            view.SetAnswersInteractable(false);

            var q = _questions[_index];
            bool timedOut = selectedIndex < 0;
            bool correct = !timedOut && selectedIndex == q.CorrectIndex;
            string selected = timedOut ? string.Empty : q.Options[selectedIndex];

            string message;
            Color color;
            if (correct)
            {
                _combo++;
                if (_combo > _maxCombo) _maxCombo = _combo;
                _score += PointsForCorrect();
                message = "Correct!";
                color = GoodColor;
            }
            else
            {
                _combo = 0;
                string prefix = timedOut ? "Time!" : "Wrong";
                message = $"{prefix}  →  {q.CorrectAnswer}";
                color = BadColor;
            }

            // Revision is a study loop, so its feedback teaches: append the kanji's
            // meaning, romaji and kana. Normal games keep the terse single-line feedback.
            if (_session.IsRevision)
            {
                string detail = RevisionDetail(q.KanjiCharacter);
                if (!string.IsNullOrEmpty(detail)) message += "\n" + detail;
            }

            view.SetFeedback(message, color);

            _session.Results.Add(new QuestionResult
            {
                KanjiCharacter = q.KanjiCharacter,
                CorrectAnswer = q.CorrectAnswer,
                SelectedAnswer = selected,
                IsCorrect = correct,
                TimedOut = timedOut,
                ResponseTime = Time.time - _shownAt,
            });

            // Global XP for a correct answer, using the PRE-answer mastery score (read
            // BEFORE RecordAttempt changes it), so learning a kanji at its current
            // difficulty is rewarded. Wrong answers award 0 XP; mastery still updates.
            int preAnswerMastery = MasteryService.GetScore(q.KanjiCharacter);
            XpService.AwardAnswerXp(_config.Answer, _config.Timer, preAnswerMastery, correct);

            // Persistent per-kanji mastery (separate from the visible run score and XP).
            // Uses this run's actual modes, so a revision run (Untimed + inherited
            // AnswerMode) automatically resolves the correct revision profile.
            MasteryService.RecordAttempt(q.KanjiCharacter, _config.Answer, _config.Timer, correct);

            view.SetScore(_score);
            view.SetCombo(_combo);

            // Next question waits until the feedback period ends (longer during revision).
            StartCoroutine(AdvanceAfterFeedback(feedback.For(correct, _session.IsRevision)));
        }

        private int PointsForCorrect()
        {
            int speedBonus = _config.Timer == TimerMode.Timed
                ? Mathf.RoundToInt(maxSpeedBonus * Mathf.Clamp01(_remaining / questionTimeSeconds))
                : 0;
            float multiplier = Mathf.Min(1f + comboStep * (_combo - 1), maxComboMultiplier);
            return Mathf.RoundToInt((basePoints + speedBonus) * multiplier);
        }

        private IEnumerator AdvanceAfterFeedback(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _index++;
            NextQuestion();
        }

        /// <summary>Meaning + romaji + kana for a kanji in this run's dataset — shown in
        /// REVISION feedback only. Returns "" if the kanji or its data is unavailable.</summary>
        private string RevisionDetail(string character)
        {
            if (_database != null && _database.TryGet(character, out var k) && k != null)
                return $"{k.PrimaryMeaning}\n{k.Romaji}  ·  {k.PrimaryReading}";
            return string.Empty;
        }

        private void FinishRun()
        {
            if (_finished) return; // commit + navigate exactly once, even if reached twice
            _finished = true;
            _running = false;
            _accepting = false;

            _session.Score = _score;
            _session.MaxCombo = _maxCombo;
            SessionContext.LastSession = _session;

            // Commit a completed NORMAL session (sessions + daily streak). ActivityService
            // ignores revision and de-dupes duplicate completions — this is the single
            // authoritative commit point. Per-answer mastery was already recorded.
            // Capture "first activity today" BEFORE recording (which stamps today's date).
            bool firstActivityToday = !ActivityService.HasActivityToday();
            bool sessionCounted = ActivityService.RecordNormalSessionCompleted(_session);

            // Global XP: +50 once per completed normal session, and the daily streak
            // bonus at most once per calendar day. Revision/duplicate → sessionCounted
            // is false, so neither is awarded.
            XpService.AwardForCompletedSession(sessionCounted, firstActivityToday, ActivityService.CurrentStreak);

            // Latch any level completion/unlock earned by this run (reads mastery only;
            // does not touch scoring, XP, sessions or streaks).
            LevelProgressionService.EvaluateAndPersist();

            SceneLoader.GoToResults();
        }
    }
}
