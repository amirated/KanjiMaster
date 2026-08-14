using System.Collections.Generic;
using System.Linq;
using KanjiMaster.Core;
using Data = KanjiRush.Data;

namespace KanjiMaster.Kanji
{
    /// <summary>
    /// Question SELECTION (which kanji appear), separate from question GENERATION
    /// (what options are shown). Wraps the level's dataset and hands back kanji:
    /// a unique random set for a normal run, or a specific set for revision.
    /// </summary>
    public class QuestionPool
    {
        private readonly Data.KanjiDatabase _db;

        public Data.KanjiDatabase Database => _db;
        public int Count => _db.Count;

        public QuestionPool(Data.KanjiDatabase db) => _db = db;

        public static QuestionPool Load(KanjiLevel level) =>
            new QuestionPool(Data.KanjiDatabase.Load(ResourceName(level)));

        public static string ResourceName(KanjiLevel level) =>
            level == KanjiLevel.N4 ? "kanji_n4" : "kanji_n5";

        /// <summary>A shuffled set of <paramref name="count"/> unique kanji (capped by pool size).</summary>
        public List<Data.Kanji> SelectUnique(int count, System.Random rng)
        {
            var all = _db.All.ToList();
            Shuffle(all, rng);
            return all.Take(System.Math.Min(count, all.Count)).ToList();
        }

        /// <summary>The kanji for the given characters, in order (skips any not found).</summary>
        public List<Data.Kanji> SelectByChars(IEnumerable<string> chars)
        {
            var result = new List<Data.Kanji>();
            foreach (var c in chars)
            {
                var k = _db.ByChar(c);
                if (k != null) result.Add(k);
            }
            return result;
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
