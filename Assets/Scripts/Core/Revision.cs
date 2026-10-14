using System.Collections.Generic;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Derives a revision run from a completed session, without mutating it.
    /// A revision is forced Untimed, preserves the original AnswerMode and Level,
    /// and asks exactly the kanji that were answered incorrectly (no padding).
    /// </summary>
    public static class Revision
    {
        public static GameConfig ConfigFor(GameSession original)
        {
            var config = original.Config.Clone(); // preserves Answer + Level
            config.Timer = TimerMode.Untimed;     // revision is always untimed
            return config;
        }

        /// <summary>
        /// The exact kanji to revise: the ones answered incorrectly in the preceding
        /// session, in the order the mistakes occurred, each at most once. Normal runs
        /// already use 15 UNIQUE kanji, so a kanji can be wrong at most once and no
        /// duplicates can arise upstream — the de-dup here is purely defensive against a
        /// future upstream change, and never pads or reorders beyond removing repeats.
        /// </summary>
        public static List<string> KanjiFor(GameSession original)
        {
            var seen = new HashSet<string>();
            var result = new List<string>();
            if (original == null) return result;
            foreach (var k in original.IncorrectKanji())
                if (!string.IsNullOrEmpty(k) && seen.Add(k)) result.Add(k);
            return result;
        }
    }
}
