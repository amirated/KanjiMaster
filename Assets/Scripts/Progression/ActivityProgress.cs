using System;

namespace KanjiMaster.Progression
{
    /// <summary>Aggregate session activity. Only the counter that exists today is
    /// stored (total sessions); more aggregates (total questions, play time, …) can
    /// be added here later without touching other systems.</summary>
    [Serializable]
    public class SessionStats
    {
        public int totalSessions;
    }

    /// <summary>
    /// Daily-streak state — DATA ONLY. The streak ALGORITHM (calendar-day comparison,
    /// increment/break rules) lives in <see cref="StreakCalculator"/>; the commit point
    /// is <see cref="ActivityService"/>. <c>lastPlayedDate</c> is the last calendar date
    /// (yyyy-MM-dd, invariant) on which a normal session was completed — i.e. the
    /// "last active date". Field names are kept stable for backward compatibility with
    /// existing saves; <c>longestStreak</c> is additive (missing → 0, seeded on load).
    /// </summary>
    [Serializable]
    public class StreakData
    {
        public int currentStreak;
        public int longestStreak;
        public string lastPlayedDate; // yyyy-MM-dd (invariant) — last active/completed date
    }

    /// <summary>Player activity: sessions + streak.</summary>
    [Serializable]
    public class ActivityProgress
    {
        public SessionStats sessions = new SessionStats();
        public StreakData streak = new StreakData();
    }
}
