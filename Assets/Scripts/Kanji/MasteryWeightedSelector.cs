using System;
using System.Collections.Generic;
using Data = KanjiRush.Data;

namespace KanjiMaster.Kanji
{
    /// <summary>
    /// Mastery-weighted, level-safe, UNIQUE question selection for a normal run.
    ///
    /// It only ever draws from the pool it is handed (the caller passes the CURRENT
    /// LEVEL's kanji), so level eligibility comes first and mastery weighting second —
    /// weighting can never pull in a kanji from another level. Selection is without
    /// replacement, so a run never repeats a kanji. Weighting favours weaker/newer kanji
    /// (low mastery → high weight) and rarely picks well-known ones (high mastery → low
    /// weight), but every weight is ≥ 1, so all-high-mastery pools still yield a valid
    /// session and there is no empty result, retry loop, or duplicate.
    ///
    /// Small pools are handled gracefully: it returns min(count, poolSize) unique kanji —
    /// never padding with duplicates. An empty pool returns an empty list (the caller
    /// decides a level with no content cannot start).
    /// </summary>
    public static class MasteryWeightedSelector
    {
        /// <summary>(inclusive upper mastery bound → selection weight). Higher mastery
        /// means lower weight; the final band catches "41+". Config-driven — retune here.</summary>
        public static readonly (int maxMasteryInclusive, int weight)[] Bands =
        {
            (2,  32), // 0–2   new / just seen → strongly favoured
            (6,  16), // 3–6
            (12,  8), // 7–12  normal
            (20,  4), // 13–20
            (40,  2), // 21–40 well known → seldom
            (int.MaxValue, 1), // 41+ → rarely (never zero, so still selectable)
        };

        public static int Weight(int mastery)
        {
            foreach (var (max, w) in Bands)
                if (mastery <= max) return w;
            return 1;
        }

        /// <summary>Pick up to <paramref name="count"/> unique kanji from
        /// <paramref name="pool"/>, weighted by each kanji's mastery via
        /// <paramref name="masteryOf"/> (null → uniform). Never returns duplicates or
        /// kanji outside the pool.</summary>
        public static List<Data.Kanji> SelectUnique(IReadOnlyList<Data.Kanji> pool, int count,
            Func<Data.Kanji, int> masteryOf, System.Random rng)
        {
            var result = new List<Data.Kanji>();
            if (pool == null || pool.Count == 0 || count <= 0) return result;

            rng ??= new System.Random();
            var candidates = new List<Data.Kanji>(pool);
            var weights = new List<int>(candidates.Count);
            foreach (var k in candidates)
                weights.Add(masteryOf != null ? Math.Max(1, Weight(masteryOf(k))) : 1);

            int take = Math.Min(count, candidates.Count);
            for (int n = 0; n < take; n++)
            {
                int total = 0;
                foreach (var w in weights) total += w;

                int r = rng.Next(total); // 0 .. total-1
                int idx = 0;
                while (idx < weights.Count - 1 && r >= weights[idx]) { r -= weights[idx]; idx++; }

                result.Add(candidates[idx]);
                candidates.RemoveAt(idx); // without replacement → uniqueness + termination
                weights.RemoveAt(idx);
            }
            return result;
        }
    }
}
