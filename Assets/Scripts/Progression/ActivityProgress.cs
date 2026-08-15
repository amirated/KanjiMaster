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
    /// Streak state — DATA ONLY. The streak algorithm (what counts as a day, when it
    /// increments/breaks, timezones, grace periods) is deferred to a later task.
    /// These are just the fields the current save already carried.
    /// </summary>
    [Serializable]
    public class StreakData
    {
        public int currentStreak;
        public string lastPlayedDate; // yyyy-MM-dd; interpretation is deferred
    }

    /// <summary>Player activity: sessions + streak.</summary>
    [Serializable]
    public class ActivityProgress
    {
        public SessionStats sessions = new SessionStats();
        public StreakData streak = new StreakData();
    }
}
