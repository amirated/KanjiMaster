using System;
using KanjiMaster.Core;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// The single authoritative place that records ACTIVITY (completed normal sessions
    /// + daily streak). It is deliberately decoupled from mastery scoring, level
    /// progression, XP, question selection and UI: gameplay simply NOTIFIES this
    /// service when a normal session reaches Results, and the UI READS the counters
    /// from here rather than computing them.
    ///
    /// Streak math lives in <see cref="StreakCalculator"/>; "now" comes from an
    /// injectable <see cref="IClock"/>; persistence is delegated to
    /// <see cref="PlayerProgressService"/> — mirroring <see cref="MasteryService"/>.
    /// </summary>
    public static class ActivityService
    {
        /// <summary>Time source for streak dates. Overridable in tests.</summary>
        public static IClock Clock = new SystemClock();

        /// <summary>Progress service used for persistence (settable for tests).</summary>
        public static PlayerProgressService Service
        {
            get => PlayerProgressService.Default;
            set => PlayerProgressService.Default = value;
        }

        /// <summary>Cached progress (lazily loaded via the service). Settable for tests.</summary>
        public static PlayerProgress Progress
        {
            get => Service.Current;
            set => Service.Current = value;
        }

        /// <summary>Persistence hook — overridable in tests to avoid disk I/O.</summary>
        public static Action<PlayerProgress> Persist = _ => Service.Save();

        // In-memory guard so a single completed run is committed at most once, even if
        // the completion callback fires twice or Results is revisited with the same
        // session. Not persisted: a new app run / new run always has a new session.
        private static GameSession _lastCommitted;

        /// <summary>
        /// THE authoritative commit for a completed NORMAL session. Increments the
        /// session count and advances the daily streak, then persists once. Returns
        /// true if it counted, false if it was ignored (revision, null, or a duplicate
        /// completion of a session already committed).
        ///
        /// Revision is intentionally NOT counted here: it is a separate learning
        /// activity and must not touch the normal session count or streak.
        /// </summary>
        public static bool RecordNormalSessionCompleted(GameSession session)
        {
            if (session == null) return false;
            if (session.IsRevision) return false;                 // revision ≠ normal session
            if (ReferenceEquals(session, _lastCommitted)) return false; // duplicate completion
            _lastCommitted = session;

            var p = Progress;
            p.activity.sessions.totalSessions++;

            var streak = p.activity.streak;
            var (current, longest, lastActive) = StreakCalculator.Advance(
                streak.currentStreak, streak.longestStreak,
                ActivityDates.Parse(streak.lastPlayedDate), Clock.Now.Date);
            streak.currentStreak = current;
            streak.longestStreak = longest;
            streak.lastPlayedDate = ActivityDates.Format(lastActive);

            Persist(p);
            return true;
        }

        /// <summary>Heal an activity block loaded from an older/partial save: a
        /// completed session always means a streak of at least 1, and longestStreak can
        /// never be below currentStreak. Non-destructive; safe to call repeatedly.</summary>
        public static void EnsureConsistent(PlayerProgress p)
        {
            var sessions = p?.activity?.sessions;
            if (sessions != null && sessions.totalSessions < 0) sessions.totalSessions = 0;

            var s = p?.activity?.streak;
            if (s == null) return;
            if (s.currentStreak < 0) s.currentStreak = 0;
            if (s.longestStreak < s.currentStreak) s.longestStreak = s.currentStreak;
        }

        /// <summary>Clear the in-memory duplicate-commit guard (new app run / tests).</summary>
        public static void ResetSessionGuard() => _lastCommitted = null;

        /// <summary>True if a normal session has already been completed on today's local
        /// calendar date. Read-only (does not record anything). Used by the XP system to
        /// award the daily streak bonus only on the first qualifying activity of a day.</summary>
        public static bool HasActivityToday()
        {
            var last = ActivityDates.Parse(Progress.activity.streak.lastPlayedDate);
            return last.HasValue && last.Value.Date == Clock.Now.Date;
        }

        // ---- Read accessors for UI ---------------------------------------------
        public static int TotalSessions => Progress.activity.sessions.totalSessions;
        public static int CurrentStreak => Progress.activity.streak.currentStreak;
        public static int LongestStreak => Progress.activity.streak.longestStreak;
        public static string LastActiveDate => Progress.activity.streak.lastPlayedDate;
    }
}
