using System.Collections.Generic;
using System.Linq;

namespace KanjiMaster.Core
{
    /// <summary>
    /// The complete record of one game run (normal or revision). Results are derived
    /// from this — never reconstructed from UI state. It records correctness and
    /// timing only; it deliberately holds NO mastery scores or XP.
    /// </summary>
    public class GameSession
    {
        public GameConfig Config = new GameConfig();
        public bool IsRevision;

        public readonly List<QuestionResult> Results = new List<QuestionResult>();

        /// <summary>Running score (existing speed/combo scoring), set by the gameplay layer.</summary>
        public int Score;
        public int MaxCombo;

        public AnswerMode AnswerMode => Config.Answer;
        public TimerMode TimerMode => Config.Timer;
        public KanjiLevel Level => Config.Level;

        public int TotalQuestions => Results.Count;
        public int CorrectCount => Results.Count(r => r.IsCorrect);
        public int IncorrectCount => Results.Count(r => !r.IsCorrect);
        public float Accuracy => TotalQuestions > 0 ? (float)CorrectCount / TotalQuestions : 0f;
        public bool HasMistakes => IncorrectCount > 0;

        public IEnumerable<QuestionResult> IncorrectResults => Results.Where(r => !r.IsCorrect);

        /// <summary>The kanji answered incorrectly, in order (already unique within a run).</summary>
        public List<string> IncorrectKanji() => IncorrectResults.Select(r => r.KanjiCharacter).ToList();
    }
}
