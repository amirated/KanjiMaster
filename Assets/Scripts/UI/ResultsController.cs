using KanjiMaster.Core;
using KanjiMaster.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Results presentation. Reads the completed <see cref="GameSession"/> from
    /// <see cref="SessionContext"/> — mistakes come from session data, never from UI
    /// text — and drives the screen from <see cref="ResultsPresenter"/>.
    ///
    /// Normal run  → numerical results (+ Revise only when there are mistakes).
    /// Revision run → ONLY "Well Done!" (no numbers, no Revise) + Play Again / Main Menu.
    /// No gameplay logic here.
    /// </summary>
    public class ResultsController : MonoBehaviour
    {
        [Header("Result labels")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text correctText;
        [SerializeField] private TMP_Text incorrectText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text bestComboText;
        [SerializeField] private TMP_Text missedText;   // optional: lists missed kanji

        [Header("Revision state (optional — see docs/REVISION.md)")]
        [SerializeField] private GameObject normalResultsGroup; // container for the numeric labels
        [SerializeField] private GameObject wellDoneGroup;      // container shown on a revision run
        [SerializeField] private TMP_Text wellDoneText;         // dedicated "Well Done!" label

        [Header("Actions")]
        [SerializeField] private Button reviseButton;   // shown only for a normal run with mistakes
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        private void Start()
        {
            var session = SessionContext.LastSession ?? new GameSession();
            var view = ResultsPresenter.For(session);

            if (view.ShowNumericResults) ShowNormal(session);
            else ShowWellDone();

            if (reviseButton)
            {
                reviseButton.gameObject.SetActive(view.ShowReviseButton);
                if (view.ShowReviseButton) reviseButton.onClick.AddListener(() => OnRevise(session));
            }
            if (playAgainButton) playAgainButton.onClick.AddListener(OnPlayAgain);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(SceneLoader.GoToMainMenu);
        }

        /// <summary>Normal run: numerical results.</summary>
        private void ShowNormal(GameSession s)
        {
            if (normalResultsGroup) normalResultsGroup.SetActive(true);
            if (wellDoneGroup) wellDoneGroup.SetActive(false);
            Bind(s);
        }

        /// <summary>Revision run: ONLY "Well Done!" — never the numbers.</summary>
        private void ShowWellDone()
        {
            if (normalResultsGroup) normalResultsGroup.SetActive(false);
            if (wellDoneGroup) wellDoneGroup.SetActive(true);

            Set(titleText, ResultsPresenter.WellDoneMessage);
            Set(wellDoneText, ResultsPresenter.WellDoneMessage);

            // Defensive: blank the numeric labels so no score/percentage can show even
            // if the scene has not been split into the optional groups above.
            Set(scoreText, "");
            Set(correctText, "");
            Set(incorrectText, "");
            Set(totalText, "");
            Set(accuracyText, "");
            Set(bestComboText, "");
            Set(missedText, "");
        }

        private void Bind(GameSession s)
        {
            Set(titleText, ResultsPresenter.NormalTitle);
            Set(scoreText, $"Score: {s.Score}");
            Set(correctText, $"Correct: {s.CorrectCount}");
            Set(incorrectText, $"Incorrect: {s.IncorrectCount}");
            Set(totalText, $"Questions: {s.TotalQuestions}");
            Set(accuracyText, $"Accuracy: {s.Accuracy * 100f:0}%");
            Set(bestComboText, $"Best combo: x{s.MaxCombo}");
            Set(missedText, s.HasMistakes ? "Missed: " + string.Join(" ", s.IncorrectKanji()) : string.Empty);
        }

        // Revision preserves the original session (QueueRevision reads, never mutates it),
        // and only navigates when there is actually something to revise.
        private void OnRevise(GameSession original)
        {
            if (SessionContext.QueueRevision(original))
                SceneLoader.GoToGame();
        }

        // Play Again returns to the NORMAL flow (QueueReplayNormal clears any revision
        // state), so a revision can never recursively launch another revision.
        private void OnPlayAgain()
        {
            SessionContext.QueueReplayNormal();
            SceneLoader.GoToGame();
        }

        private static void Set(TMP_Text field, string value)
        {
            if (field) field.text = value;
        }
    }
}
