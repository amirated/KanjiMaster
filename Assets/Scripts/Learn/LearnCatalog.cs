using System.Collections.Generic;
using KanjiMaster.Core;
using KanjiMaster.Progression;
using Data = KanjiRush.Data;

namespace KanjiMaster.Learn
{
    /// <summary>One browsable kanji plus its (internal) level and player-facing level.</summary>
    public struct LearnKanji
    {
        public Data.Kanji Kanji;
        public KanjiLevel Level;         // internal JLPT dataset
        public PlayerLevel PlayerLevel;  // player-facing name
    }

    /// <summary>
    /// The Learn section's read-only catalog. Consumes the EXISTING kanji data layer
    /// (KanjiRush.Data.KanjiDatabase) — no duplicate model, no duplicate JSON — and
    /// tags each entry with its player-facing level. Currently loads all available
    /// content (N5 + N4). Structured so future filter/search/sort/mastery/lock
    /// features can be added here without changing the views.
    /// </summary>
    public class LearnCatalog
    {
        private readonly List<LearnKanji> _all;

        public IReadOnlyList<LearnKanji> All => _all;
        public int Count => _all.Count;

        private LearnCatalog(List<LearnKanji> all) => _all = all;

        public static LearnCatalog Load()
        {
            var all = new List<LearnKanji>();
            foreach (var level in new[] { KanjiLevel.N5, KanjiLevel.N4 })
            {
                Data.KanjiDatabase db;
                try { db = Data.KanjiDatabase.Load(ResourceFor(level)); }
                catch { continue; } // a missing dataset shouldn't break the whole list
                var playerLevel = LevelInfo.ToPlayerLevel(level);
                foreach (var k in db.All)
                    all.Add(new LearnKanji { Kanji = k, Level = level, PlayerLevel = playerLevel });
            }
            return new LearnCatalog(all);
        }

        private static string ResourceFor(KanjiLevel level) =>
            level == KanjiLevel.N4 ? "kanji_n4" : "kanji_n5";

        // Future (not implemented now): ByLevel(...), Search(...), Sort(...),
        // WithMastery(...), etc. — add here so views stay unchanged.
    }
}
