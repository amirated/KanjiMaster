using System.Collections;
using KanjiMaster.Core;
using KanjiMaster.Kanji;
using KanjiMaster.Services;
using KanjiMaster.UI;
using UnityEngine;

namespace KanjiMaster.Gameplay
{
    /// <summary>
    /// Drives one game session: generates questions, scores answers, tracks combo,
    /// runs the per-question timer (Timed mode only), then writes a
    /// <see cref="GameResult"/> to <see cref="GameSession"/> and navigates to Results.
    ///
    /// All gameplay rules live here; <see cref="GameHUDView"/> only presents state.
    /// The UI can be redesigned without touching this file.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameHUDView view;

        [Header("Tuning")]
        [SerializeField] private int questionsPerRun = 15;
        [SerializeField] private float questionTimeSeconds = 5f;
        [SerializeField] private float feedbackSeconds = 0.6f;

        [Header("Scoring")]
        [SerializeField] private int basePoints = 100;
        [SerializeField] private int maxSpeedBonus = 100;
        [SerializeField] private float comboStep = 0.25f;
        [SerializeField] private float maxComboMultiplier = 4f;

        private static readonly Color GoodColor = new Color(0.30f, 0.78f, 0.45f);
        private static readonly Color BadColor = new Color(0.85f, 0.32f, 0.34f);

        private GameConfig _config;
        private QuestionGenerator _generator;
        private Question _current;

        private int _index;
        private int _score;
        private int _combo;
        private int _bestCombo;
        private int _correct;
        private float _remaining;
        private bool _running;
        private bool _accepting;

        private void Start()
        {
            if (view == null)
            {
                Debug.LogError("GameController: GameHUDView reference not assigned.");
                return;
            }

            _config = GameSession.Config;

            try
            {
                _generator = new QuestionGenerator();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GameController: failed to load kanji data — {e.Message}");
                view.SetQuestion("!");
                view.SetFeedback("Could not load kanji data.", BadColor);
                return;
            }

            view.AnswerSelected += OnAnswerSelected;
            view.SetTimerVisible(_config.TimerMode == TimerMode.Timed);

            StartRun();
        }

        private void OnDestroy()
        {
            if (view != null) view.AnswerSelected -= OnAnswerSelected;
        }

        private void Update()
        {
            if (!_running || !_accepting) return;
            if (_config.TimerMode != TimerMode.Timed) return;

            _remaining -= Time.deltaTime;
            view.SetTimerNormalized(_remaining / questionTimeSeconds);
            if (_remaining <= 0f) Resolve(-1); // timeout = wrong
        }

        private void StartRun()
        {
            _index = 0;
            _score = 0;
            _combo = 0;
            _bestCombo = 0;
            _correct = 0;
            _running = true;

            view.SetScore(0);
            view.SetCombo(0);
            NextQuestion();
        }

        private void NextQuestion()
        {
            if (_index >= questionsPerRun)
            {
                FinishRun();
                return;
            }

            _current = _generator.Next(_config.AnswerLanguage);
            view.SetQuestion(_current.Prompt);
            view.SetOptions(_current.Options);
            view.ClearFeedback();
            view.SetAnswersInteractable(true);

            _remaining = questionTimeSeconds;
            if (_config.TimerMode == TimerMode.Timed) view.SetTimerNormalized(1f);

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

            bool timedOut = selectedIndex < 0;
            bool correct = !timedOut && selectedIndex == _current.CorrectIndex;

            if (correct)
            {
                _correct++;
                _combo++;
                if (_combo > _bestCombo) _bestCombo = _combo;
                _score += PointsForCorrect();
                view.SetFeedback("Correct!", GoodColor);
            }
            else
            {
                _combo = 0;
                string prefix = timedOut ? "Time!" : "Wrong";
                view.SetFeedback($"{prefix}  →  {_current.CorrectAnswer}", BadColor);
            }

            view.SetScore(_score);
            view.SetCombo(_combo);
            StartCoroutine(AdvanceAfterFeedback());
        }

        private int PointsForCorrect()
        {
            int speedBonus = _config.TimerMode == TimerMode.Timed
                ? Mathf.RoundToInt(maxSpeedBonus * Mathf.Clamp01(_remaining / questionTimeSeconds))
                : 0;
            float multiplier = Mathf.Min(1f + comboStep * (_combo - 1), maxComboMultiplier);
            return Mathf.RoundToInt((basePoints + speedBonus) * multiplier);
        }

        private IEnumerator AdvanceAfterFeedback()
        {
            yield return new WaitForSeconds(feedbackSeconds);
            _index++;
            NextQuestion();
        }

        private void FinishRun()
        {
            _running = false;
            _accepting = false;

            GameSession.LastResult = new GameResult
            {
                Score = _score,
                Correct = _correct,
                TotalQuestions = questionsPerRun,
                BestCombo = _bestCombo,
            };

            SceneLoader.GoToResults();
        }
    }
}
