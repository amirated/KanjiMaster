using KanjiMaster.Core;
using KanjiMaster.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Presentation for the Results screen. Binds the last <see cref="GameResult"/>
    /// to the labels and wires the two navigation buttons. No gameplay logic —
    /// it only displays data produced elsewhere and triggers scene changes.
    /// </summary>
    public class ResultsController : MonoBehaviour
    {
        [Header("Result labels")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text correctText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text bestComboText;

        [Header("Actions")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        private void Start()
        {
            Bind(GameSession.LastResult);

            if (playAgainButton) playAgainButton.onClick.AddListener(SceneLoader.GoToGame);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(SceneLoader.GoToMainMenu);
        }

        private void Bind(GameResult result)
        {
            result ??= new GameResult();
            if (scoreText) scoreText.text = $"Score: {result.Score}";
            if (correctText) correctText.text = $"Correct: {result.Correct}";
            if (totalText) totalText.text = $"Questions: {result.TotalQuestions}";
            if (accuracyText) accuracyText.text = $"Accuracy: {result.Accuracy * 100f:0}%";
            if (bestComboText) bestComboText.text = $"Best combo: x{result.BestCombo}";
        }
    }
}
