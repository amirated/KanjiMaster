using System;
using System.Globalization;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Serialization-friendly, locale-independent calendar dates for activity/streak
    /// data. Stored as "yyyy-MM-dd" using the invariant culture so a device's locale
    /// never changes how a stored date is read or written.
    /// </summary>
    public static class ActivityDates
    {
        public const string Format_ = "yyyy-MM-dd";

        /// <summary>Parse a stored date (date part only), or null if absent/unreadable.
        /// Tolerant of other parseable forms that might exist in legacy saves.</summary>
        public static DateTime? Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParseExact(s, Format_, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d))
                return d.Date;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d.Date;
            return null;
        }

        /// <summary>Format a date as "yyyy-MM-dd" (invariant, date part only).</summary>
        public static string Format(DateTime d) =>
            d.Date.ToString(Format_, CultureInfo.InvariantCulture);
    }
}
