using KanjiMaster.Core;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Maps (AnswerMode, TimerMode) to the mastery <see cref="ScoringProfile"/>.
    /// Harder-to-produce answers and time pressure are worth more:
    ///
    ///            Untimed        Timed
    ///   English  +2 / -1        +4 / -3
    ///   Romaji   +4 / -3        +8 / -5
    ///   Kana     +8 / -5        +16 / -9
    ///
    /// Timed always scores higher than Untimed for the same AnswerMode. Because a
    /// revision run is Untimed (with the original AnswerMode), resolving on the
    /// actual run's modes automatically gives the correct revision profile.
    /// </summary>
    public static class ScoringProfileResolver
    {
        public static ScoringProfile Resolve(AnswerMode answer, TimerMode timer)
        {
            bool timed = timer == TimerMode.Timed;
            switch (answer)
            {
                case AnswerMode.Romaji: return timed ? new ScoringProfile(8, 5) : new ScoringProfile(4, 3);
                case AnswerMode.Kana: return timed ? new ScoringProfile(16, 9) : new ScoringProfile(8, 5);
                case AnswerMode.English:
                default: return timed ? new ScoringProfile(4, 3) : new ScoringProfile(2, 1);
            }
        }
    }
}
