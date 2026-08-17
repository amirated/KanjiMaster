using NUnit.Framework;
using KanjiMaster.UI;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Boundary tests for the compact number formatter used by the XP display (and any
    /// future progression UI). Presentation only — it never changes stored values.
    /// </summary>
    public class NumberFormatTests
    {
        [TestCase(0L, "0")]
        [TestCase(250L, "250")]
        [TestCase(999L, "999")]
        [TestCase(1000L, "1k")]
        [TestCase(1200L, "1.2k")]
        [TestCase(9999L, "10k")]     // 9.999k rounds half-up to 10k
        [TestCase(10000L, "10k")]
        [TestCase(12500L, "12.5k")]
        [TestCase(999999L, "1M")]    // carries up a unit
        [TestCase(1000000L, "1M")]
        [TestCase(1250000L, "1.3M")] // 1.25M rounds half-away-from-zero to 1.3M
        [TestCase(10000000L, "10M")]
        [TestCase(100000000L, "100M")]
        [TestCase(1000000000L, "1B")]
        public void Compact_Formats_Representative_Boundaries(long value, string expected)
            => Assert.AreEqual(expected, NumberFormat.Compact(value));

        [Test]
        public void Compact_Handles_Negative_Safely_Without_Crashing()
        {
            Assert.AreEqual("-1.5k", NumberFormat.Compact(-1500));
            Assert.AreEqual("-250", NumberFormat.Compact(-250));
        }

        [Test]
        public void Compact_Is_Deterministic()
            => Assert.AreEqual(NumberFormat.Compact(1250000), NumberFormat.Compact(1250000));
    }
}
