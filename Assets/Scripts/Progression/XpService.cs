using System;
using KanjiMaster.Core;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Facade the gameplay layer uses to AWARD Global XP. Gameplay says "award answer
    /// XP / session XP / streak XP"; the formulas live in <see cref="XpCalculator"/> and
    /// the numbers in <see cref="XpConfig"/>. Persistence is delegated to
    /// <see cref="PlayerProgressService"/> — mirroring MasteryService/ActivityService.
    ///
    /// XP is only ever ADDED (never subtracted), so <c>totalXp</c> can never go negative.
    /// XP is independent of Kanji mastery and level progression: this class reads/writes
    /// only <c>progression.xp</c>.
    /// </summary>
    public static class XpService
    {
        public static PlayerProgressService Service
        {
            get => PlayerProgressService.Default;
            set => PlayerProgressService.Default = value;
        }

        public static PlayerProgress Progress
        {
            get => Service.Current;
            set => Service.Current = value;
        }

        /// <summary>Persistence hook — overridable in tests to avoid disk I/O.</summary>
        public static Action<PlayerProgress> Persist = _ => Service.Save();

        /// <summary>
        /// Award answer XP for one evaluated question. On a CORRECT answer, awards
        /// base(answer,timer) × masteryMultiplier(<paramref name="preAnswerMastery"/>);
        /// on a wrong answer awards 0. Pass the mastery score read BEFORE the mastery
        /// change is applied. Returns the XP awarded.
        /// </summary>
        public static int AwardAnswerXp(AnswerMode answer, TimerMode timer, int preAnswerMastery, bool correct)
            => correct ? Add(XpCalculator.AnswerXp(answer, timer, preAnswerMastery)) : 0;

        /// <summary>Award the flat completed-normal-session bonus. Returns the XP awarded.</summary>
        public static int AwardSessionCompletionXp() => Add(XpCalculator.SessionCompletionXp());

        /// <summary>Award the daily streak bonus for the given streak length (0 if none).</summary>
        public static int AwardDailyStreakXp(int streakDays) => Add(XpCalculator.StreakBonusXp(streakDays));

        /// <summary>
        /// The single integration seam for end-of-run XP. Awards the +50 session bonus
        /// only when the run actually counted as a completed normal session, and the
        /// daily streak bonus only on the first qualifying activity of a new calendar
        /// day. Revision and duplicate completions pass <paramref name="sessionCounted"/>
        /// = false, so they award neither.
        /// </summary>
        public static void AwardForCompletedSession(bool sessionCounted, bool firstActivityToday, int currentStreak)
        {
            if (!sessionCounted) return;              // revision / duplicate completion → nothing
            AwardSessionCompletionXp();               // +50, once per completed session
            if (firstActivityToday)
                AwardDailyStreakXp(currentStreak);    // streak bonus, at most once per calendar day
        }

        private static int Add(int amount)
        {
            if (amount <= 0) return 0;                // only ever add; never negative
            var xp = Progress.progression.xp;
            xp.totalXp += amount;
            Persist(Progress);
            return amount;
        }

        public static int TotalXp => Progress.progression.xp.totalXp;
        public static int Version => Progress.progression.xp.xpSystemVersion;
    }
}
