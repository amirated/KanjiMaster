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
    /// text. Works for both normal and revision sessions. Shows Revise only when the
    /// (non-revision) session has mistakes. No gameplay logic here.
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

        [Header("Actions")]
        [SerializeField] private Button reviseButton;   // shown only when there are mistakes
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        private void Start()
        {
            var session = SessionContext.LastSession ?? new GameSession();
            Bind(session);

            bool canRevise = session.HasMistakes && !session.IsRevision;
            if (reviseButton)
            {
                reviseButton.gameObject.SetActive(canRevise);
                if (canRevise) reviseButton.onClick.AddListener(() => OnRevise(session));
            }

            if (playAgainButton) playAgainButton.onClick.AddListener(OnPlayAgain);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(SceneLoader.GoToMainMenu);
        }

        private void Bind(GameSession s)
        {
            if (titleText) titleText.text = s.IsRevision ? "Revision Results" : "Results";
            if (scoreText) scoreText.text = $"Score: {s.Score}";
            if (correctText) correctText.text = $"Correct: {s.CorrectCount}";
            if (incorrectText) incorrectText.text = $"Incorrect: {s.IncorrectCount}";
            if (totalText) totalText.text = $"Questions: {s.TotalQuestions}";
            if (accuracyText) accuracyText.text = $"Accuracy: {s.Accuracy * 100f:0}%";
            if (bestComboText) bestComboText.text = $"Best combo: x{s.MaxCombo}";
            if (missedText)
                missedText.text = s.HasMistakes ? "Missed: " + string.Join(" ", s.IncorrectKanji()) : string.Empty;
        }

        // Revision preserves the original session (QueueRevision reads, never mutates it).
        private void OnRevise(GameSession original)
        {
            SessionContext.QueueRevision(original);
            SceneLoader.GoToGame();
        }

        private void OnPlayAgain()
        {
            SessionContext.QueueReplayNormal();
            SceneLoader.GoToGame();
        }
    }
}
