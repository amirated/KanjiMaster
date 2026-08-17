using System;
using System.Globalization;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Reusable compact formatting for positive progression values (XP now; future
    /// Progress screen / profile / leaderboard / achievements). Presentation only — it
    /// never changes the underlying stored value.
    ///
    ///   &lt; 1,000        → exact integer           (0, 250, 999)
    ///   ≥ 1,000        → k / M / B with ≤ 1 decimal (1k, 1.2k, 12.5k, 125k, 1.3M, 1B)
    ///
    /// Rules (deterministic): choose the largest unit (k=1e3, M=1e6, B=1e9); show 1
    /// decimal while the scaled value is &lt; 100, otherwise 0 decimals; round half-away
    /// -from-zero to that precision (so 1,250,000 → "1.3M"); drop a trailing ".0"; and
    /// if rounding carries to 1000 promote to the next unit (999,999 → "1M").
    /// Negatives (not expected for XP) are formatted with a leading "-" and never crash.
    /// </summary>
    public static class NumberFormat
    {
        private static readonly string[] Suffixes = { "k", "M", "B" };
        private static readonly double[] Divisors = { 1e3, 1e6, 1e9 };

        public static string Compact(long value)
        {
            if (value < 0)
                return "-" + Compact(value == long.MinValue ? long.MaxValue : -value);

            if (value < 1000)
                return value.ToString(CultureInfo.InvariantCulture);

            int i = value >= 1_000_000_000L ? 2 : value >= 1_000_000L ? 1 : 0;
            while (true)
            {
                double scaled = value / Divisors[i];
                int decimals = scaled < 100 ? 1 : 0;
                double rounded = Math.Round(scaled, decimals, MidpointRounding.AwayFromZero);

                if (rounded >= 1000 && i < Suffixes.Length - 1) { i++; continue; } // carried up a unit

                string fmt = decimals == 1 ? "0.#" : "0";
                return rounded.ToString(fmt, CultureInfo.InvariantCulture) + Suffixes[i];
            }
        }
    }
}
