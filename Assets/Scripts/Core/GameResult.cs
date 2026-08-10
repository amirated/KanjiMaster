using System;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Outcome of a completed game session, produced by the gameplay layer and
    /// consumed by the Results screen. Pure data — no logic.
    /// </summary>
    [Serializable]
    public class GameResult
    {
        public int Score;
        public int Correct;
        public int TotalQuestions;
        public int BestCombo;

        /// <summary>Fraction 0..1; 0 when no questions were answered.</summary>
        public float Accuracy => TotalQuestions > 0 ? (float)Correct / TotalQuestions : 0f;
    }
}
