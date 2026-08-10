using System.Collections.Generic;
using KanjiMaster.Core;
using KanjiMaster.UI;
using UnityEngine;

namespace KanjiMaster.Gameplay
{
    /// <summary>
    /// Orchestrates the gameplay screen. In this foundation it only applies the
    /// selected <see cref="GameConfig"/> to the HUD (e.g. hides the timer in
    /// Untimed mode, shows a placeholder question in the chosen language) and logs
    /// answer taps. The real loop — question generation from KanjiDatabase,
    /// scoring, combo, per-question timer, run length, writing a GameResult and
    /// navigating to Results — is intentionally NOT implemented yet.
    ///
    /// All such logic will live here (or in helpers it owns), never in GameHUDView,
    /// so the UI can be redesigned without touching gameplay.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [SerializeField] private GameHUDView view;

        private void Start()
        {
            if (view == null)
            {
                Debug.LogError("GameController: GameHUDView reference not assigned.");
                return;
            }

            var config = GameSession.Config;

            // Timer UI is only present in Timed mode.
            view.SetTimerVisible(config.TimerMode == TimerMode.Timed);
            view.SetTimerNormalized(1f);

            view.SetScore(0);
            view.SetCombo(0);
            view.ClearFeedback();

            ShowPlaceholderQuestion(config.AnswerLanguage);

            view.AnswerSelected += OnAnswerSelected;
        }

        private void OnDestroy()
        {
            if (view != null) view.AnswerSelected -= OnAnswerSelected;
        }

        // --- Placeholder only. Replaced by real question generation later. ---
        private void ShowPlaceholderQuestion(AnswerLanguage language)
        {
            view.SetQuestion("見"); // placeholder kanji
            IReadOnlyList<string> options = language == AnswerLanguage.Japanese
                ? new[] { "みる", "たべる", "みず", "き" }
                : new[] { "see", "eat", "water", "tree" };
            view.SetOptions(options);
            view.SetAnswersInteractable(true);
        }

        private void OnAnswerSelected(int index)
        {
            // TODO: real scoring / progression. For now just acknowledge the tap.
            Debug.Log($"Answer {index} selected (gameplay not implemented yet).");
        }
    }
}
