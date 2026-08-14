namespace KanjiMaster.Progression
{
    /// <summary>
    /// The single, generic mastery-score transition. Not six algorithms — one
    /// function parameterised by (correctPoints, wrongPenalty).
    ///
    /// Invariants (always preserved):
    ///   • score is never negative;
    ///   • a CORRECT answer yields an EVEN score (parity = last attempt correct);
    ///   • a WRONG answer on a positive score yields a POSITIVE ODD score;
    ///   • 0 → wrong → 0   and   1 → wrong → 1 (floors).
    ///
    /// Correct: even score gains the full correctPoints; odd score only +1 (to become
    /// even). Wrong: subtract the penalty, then round DOWN to the nearest positive odd
    /// (floor 1). This uses the penalty but never lets it break the parity/positivity
    /// invariant — the invariant wins over mechanically applying the penalty.
    ///
    /// NOTE: correctPoints must be even for the "correct → even" invariant to hold;
    /// all shipping profiles use even correctPoints.
    /// </summary>
    public static class MasteryScoring
    {
        public static int Apply(int currentScore, int correctPoints, int wrongPenalty, bool answerCorrect)
        {
            if (answerCorrect)
                return IsEven(currentScore) ? currentScore + correctPoints : currentScore + 1;

            if (currentScore <= 0) return 0;               // 0 → wrong → 0, never negative
            return RoundDownToPositiveOdd(currentScore - wrongPenalty);
        }

        /// <summary>Nearest positive odd number ≤ x, with a floor of 1.</summary>
        public static int RoundDownToPositiveOdd(int x)
        {
            if (x < 1) return 1;
            return IsEven(x) ? x - 1 : x;
        }

        private static bool IsEven(int n) => (n & 1) == 0;
    }
}
