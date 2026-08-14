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

        private int _index;
        private int _combo;
        private int _maxCombo;
        private int _score;
        private float _remaining;
        private float _shownAt;
        private bool _running;
        private bool _accepting;

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
            var generator = new QuestionGenerator(pool.Database, rng);

            List<Data.Kanji> kanji = SessionContext.NextKanji != null
                ? pool.SelectByChars(SessionContext.NextKanji)   // revision: exactly the mistakes
                : pool.SelectUnique(questionsPerRun, rng);        // normal: 15 unique

            if (kanji.Count == 0)
                throw new System.InvalidOperationException("no kanji selected for this run");

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

            if (correct)
            {
                _combo++;
                if (_combo > _maxCombo) _maxCombo = _combo;
                _score += PointsForCorrect();
                view.SetFeedback("Correct!", GoodColor);
            }
            else
            {
                _combo = 0;
                string prefix = timedOut ? "Time!" : "Wrong";
                view.SetFeedback($"{prefix}  →  {q.CorrectAnswer}", BadColor);
            }

            _session.Results.Add(new QuestionResult
            {
                KanjiCharacter = q.KanjiCharacter,
                CorrectAnswer = q.CorrectAnswer,
                SelectedAnswer = selected,
                IsCorrect = correct,
                TimedOut = timedOut,
                ResponseTime = Time.time - _shownAt,
            });

            // Persistent per-kanji mastery (separate from the visible run score).
            // Uses this run's actual modes, so a revision run (Untimed + inherited
            // AnswerMode) automatically resolves the correct revision profile.
            MasteryService.RecordAttempt(q.KanjiCharacter, _config.Answer, _config.Timer, correct);

            view.SetScore(_score);
            view.SetCombo(_combo);

            // Next question waits until the (mode-dependent) feedback period ends.
            StartCoroutine(AdvanceAfterFeedback(feedback.For(correct)));
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

        private void FinishRun()
        {
            _running = false;
            _accepting = false;

            _session.Score = _score;
            _session.MaxCombo = _maxCombo;
            SessionContext.LastSession = _session;

            // Count the completed run (normal or revision) for the future XP system.
            MasteryService.RegisterSessionPlayed();

            SceneLoader.GoToResults();
        }
    }
}
