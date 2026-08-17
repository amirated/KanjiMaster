using System;
using KanjiMaster.Core;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Pure Global-XP math — no persistence, no Unity, no side effects — so every rule
    /// is deterministically unit-testable. Reads all numbers from <see cref="XpConfig"/>.
    ///
    /// Rounding policy: answer XP is base × multiplier rounded to the NEAREST integer,
    /// ties away from zero (MidpointRounding.AwayFromZero). No fractional XP is ever
    /// accumulated. All results are &gt;= 0.
    /// </summary>
    public static class XpCalculator
    {
        /// <summary>Base answer XP from answer mode + timer (before the mastery multiplier).</summary>
        public static int BaseAnswerXp(AnswerMode answer, TimerMode timer)
        {
            int b = answer switch
            {
                AnswerMode.Kana => XpConfig.BaseKana,
                AnswerMode.Romaji => XpConfig.BaseRomaji,
                _ => XpConfig.BaseEnglish,
            };
            return timer == TimerMode.Timed ? b * 2 : b;
        }

        /// <summary>Multiplier for a given mastery score (use the PRE-answer score).</summary>
        public static float MasteryMultiplier(int mastery)
        {
            foreach (var (maxInclusive, multiplier) in XpConfig.MasteryMultiplierBands)
                if (mastery <= maxInclusive) return multiplier;
            return XpConfig.MasteryMultiplierBands[XpConfig.MasteryMultiplierBands.Length - 1].multiplier;
        }

        /// <summary>Final answer XP for a CORRECT answer, using the PRE-answer mastery.</summary>
        public static int AnswerXp(AnswerMode answer, TimerMode timer, int preAnswerMastery)
        {
            double raw = BaseAnswerXp(answer, timer) * (double)MasteryMultiplier(preAnswerMastery);
            int xp = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            return xp < 0 ? 0 : xp;
        }

        /// <summary>Flat bonus for completing one normal session.</summary>
        public static int SessionCompletionXp() => XpConfig.SessionCompletionXp;

        /// <summary>Daily streak bonus: min(streakDays × perDay, dailyCap); 0 if no streak.</summary>
        public static int StreakBonusXp(int streakDays) =>
            streakDays <= 0 ? 0 : Math.Min(streakDays * XpConfig.StreakXpPerDay, XpConfig.StreakXpDailyCap);
    }
}
