using System;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// The source of "now" for activity/streak logic. Injectable so date-based rules
    /// are deterministic in tests (a fake clock) instead of reading DateTime.Now
    /// directly. V1 uses the device's LOCAL calendar date (no server/UTC authority).
    /// </summary>
    public interface IClock
    {
        DateTime Now { get; }
    }

    /// <summary>Real clock — the device's local time.</summary>
    public sealed class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
