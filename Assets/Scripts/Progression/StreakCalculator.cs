using System;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Pure daily-streak math — no persistence, no clock, no UI, no side effects — so
    /// every rule and calendar edge case is deterministically unit-testable. Given the
    /// prior streak state and today's calendar date, it returns the new state.
    ///
    /// Rules (calendar-date based, not elapsed 24-hour periods):
    ///   • First-ever completion (no last date) → current = 1.
    ///   • Same calendar day               → current unchanged.
    ///   • Immediately following day        → current + 1.
    ///   • One or more days missed          → current resets to 1.
    ///   • Clock moved backwards (today &lt; last) → current unchanged, date not moved back.
    /// longestStreak only ever grows: it never decreases when the current streak breaks.
    /// </summary>
    public static class StreakCalculator
    {
        /// <summary>Advance the streak for one completed normal session on
        /// <paramref name="today"/>. <paramref name="lastActive"/> is the last active
        /// calendar date, or null if the player has never completed a session.</summary>
        public static (int current, int longest, DateTime lastActive) Advance(
            int current, int longest, DateTime? lastActive, DateTime today)
        {
            today = today.Date;

            int newCurrent;
            if (lastActive == null)
            {
                newCurrent = 1; // first-ever completed session
            }
            else
            {
                int dayDelta = (today - lastActive.Value.Date).Days;
                if (dayDelta == 0) newCurrent = current;          // same calendar day
                else if (dayDelta == 1) newCurrent = current + 1; // consecutive day
                else if (dayDelta >= 2) newCurrent = 1;           // missed one or more days
                else newCurrent = current;                        // clock went backwards: ignore
            }

            if (newCurrent < 1) newCurrent = 1; // a completed session always means >= 1

            // Never move the recorded date backwards (guards a backwards device clock).
            DateTime newLast = (lastActive == null || today >= lastActive.Value.Date)
                ? today
                : lastActive.Value.Date;

            int newLongest = Math.Max(longest, newCurrent);
            return (newCurrent, newLongest, newLast);
        }
    }
}
