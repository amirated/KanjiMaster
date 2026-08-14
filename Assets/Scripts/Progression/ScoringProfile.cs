using System;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// The two numbers that drive a mastery transition. Data, not code — resolved
    /// from (AnswerMode, TimerMode) by <see cref="ScoringProfileResolver"/>.
    /// </summary>
    [Serializable]
    public struct ScoringProfile
    {
        public int CorrectPoints;
        public int WrongPenalty;

        public ScoringProfile(int correctPoints, int wrongPenalty)
        {
            CorrectPoints = correctPoints;
            WrongPenalty = wrongPenalty;
        }

        public override string ToString() => $"+{CorrectPoints} / -{WrongPenalty}";
    }
}
