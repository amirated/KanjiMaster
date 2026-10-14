namespace KanjiMaster.Progression
{
    /// <summary>
    /// The Global XP CONFIGURATION — every tunable number in one place so no XP formula
    /// or magic constant (10/20/40/80/50/1.5/…) is scattered across gameplay scripts.
    /// Change the economy here; the calculator/service read from this.
    ///
    /// Bumping <see cref="CurrentVersion"/> (and adding a branch in XpMigration) is how
    /// a future ruleset can change without corrupting existing player XP.
    /// </summary>
    public static class XpConfig
    {
        /// <summary>Current XP ruleset version. Stored alongside TotalXP.</summary>
        public const int CurrentVersion = 1;

        // ---- Answer XP (base, UNTIMED). Timed doubles the base. ------------------
        public const int BaseEnglish = 10;
        public const int BaseRomaji = 20;
        public const int BaseKana = 40;

        // ---- Session + streak bonuses -------------------------------------------
        /// <summary>Flat bonus for completing one normal 15-question session.</summary>
        public const int SessionCompletionXp = 50;

        /// <summary>Daily streak bonus = min(streakDays × perDay, dailyCap).</summary>
        public const int StreakXpPerDay = 10;
        public const int StreakXpDailyCap = 100;

        /// <summary>
        /// Mastery multiplier bands (inclusive upper bound → multiplier), applied to the
        /// PRE-answer mastery score so learning weaker/newer kanji is worth more. The
        /// final band uses int.MaxValue to catch "41+".
        /// </summary>
        public static readonly (int maxMasteryInclusive, float multiplier)[] MasteryMultiplierBands =
        {
            (2,  1.50f), // 0–2
            (6,  1.25f), // 3–6
            (12, 1.00f), // 7–12
            (20, 0.90f), // 13–20
            (40, 0.75f), // 21–40
            (int.MaxValue, 0.50f), // 41+
        };
    }
}
