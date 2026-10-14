using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Pure presentation for the gameplay screen. Exposes a small API to update the
    /// visible state and raises <see cref="AnswerSelected"/> when an option is
    /// tapped. Contains no scoring, timer, or question-generation logic — the
    /// gameplay layer drives it entirely through these methods.
    /// </summary>
    public class GameHUDView : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;

        [Header("Timer (hidden in Untimed mode)")]
        [SerializeField] private GameObject timerRoot;
        [SerializeField] private Image timerFill; // Image Type = Filled, Horizontal

        [Header("Question")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Answers (assign 4)")]
        [SerializeField] private Button[] answerButtons;
        [SerializeField] private TMP_Text[] answerLabels;

        /// <summary>Raised with the index (0..3) of the tapped answer.</summary>
        public event Action<int> AnswerSelected;

        public int AnswerCount => answerButtons != null ? answerButtons.Length : 0;

        private void Awake()
        {
            if (answerButtons == null) return;
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int index = i;
                if (answerButtons[i] != null)
                    answerButtons[i].onClick.AddListener(() => AnswerSelected?.Invoke(index));
            }
        }

        public void SetScore(int score)
        {
            if (scoreText) scoreText.text = $"Score {score}";
        }

        public void SetCombo(int combo)
        {
            if (comboText) comboText.text = combo > 1 ? $"x{combo}" : string.Empty;
        }

        public void SetQuestion(string kanji)
        {
            if (questionText) questionText.text = kanji;
        }

        public void SetOptions(IReadOnlyList<string> options)
        {
            if (answerButtons == null) return;
            for (int i = 0; i < answerButtons.Length; i++)
            {
                bool has = options != null && i < options.Count;
                if (answerButtons[i]) answerButtons[i].gameObject.SetActive(has);
                if (has && answerLabels != null && i < answerLabels.Length && answerLabels[i])
                    answerLabels[i].text = options[i];
            }
        }

        public void SetAnswersInteractable(bool interactable)
        {
            if (answerButtons == null) return;
            foreach (var b in answerButtons)
                if (b) b.interactable = interactable;
        }

        public void SetFeedback(string message, Color color)
        {
            if (!feedbackText) return;
            feedbackText.text = message;
            feedbackText.color = color;
        }

        public void ClearFeedback()
        {
            if (feedbackText) feedbackText.text = string.Empty;
        }

        /// <summary>Show/hide the whole timer area (hidden in Untimed mode).</summary>
        public void SetTimerVisible(bool visible)
        {
            if (timerRoot) timerRoot.SetActive(visible);
        }

        /// <summary>Set the timer fill, 0..1.</summary>
        public void SetTimerNormalized(float t)
        {
            if (timerFill) timerFill.fillAmount = Mathf.Clamp01(t);
        }
    }
}
