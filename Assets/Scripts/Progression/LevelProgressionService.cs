using System;
using System.Collections.Generic;
using UnityEngine;
using KanjiMaster.Core;
using Data = KanjiRush.Data;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Owns the LEVEL progression rules — unlock, completion and progress — kept
    /// deliberately separate from mastery scoring, XP, sessions and streaks, which it
    /// only READS. It is pure logic over a passed-in <see cref="PlayerProgress"/>; the
    /// one global touch point is <see cref="EvaluateAndPersist"/>, which uses the
    /// shared service so callers don't reach into persistence.
    ///
    /// V1 rule (config-driven — documented in docs/PROGRESSION.md):
    ///   A level is COMPLETED when at least <see cref="CompletionFraction"/> of its
    ///   kanji have a mastery score &gt;= <see cref="MasteryThreshold"/>. Completing a
    ///   level UNLOCKS every level whose prerequisite is that level. Unlocked and
    ///   completed are LATCHED (never revoked if mastery later dips).
    ///
    /// This intentionally does NOT invent a complex formula: no existing rule was
    /// documented (there is no SCORING.md progression rule), so this is a simple,
    /// clearly-documented V1 that is easy to retune via the two knobs below.
    /// </summary>
    public static class LevelProgressionService
    {
        // ---- V1 rule knobs (config; change here to retune) ----------------------

        /// <summary>Mastery score at/above which a kanji counts toward completion.
        /// 2 = "answered correctly at least once" (a correct answer makes mastery an
        /// even positive; the smallest such value is 2).</summary>
        public static int MasteryThreshold = 2;

        /// <summary>Fraction of a level's kanji that must reach the threshold for the
        /// level to count as completed. 1.0 = every kanji. Lower it later without
        /// touching any logic.</summary>
        public static float CompletionFraction = 1f;

        /// <summary>Supplies the kanji ids (Unicode code points) for a dataset.
        /// Defaults to loading the bundled dataset from Resources; overridable in tests
        /// so the rules can be exercised without real content.</summary>
        public static Func<KanjiLevel, IReadOnlyList<int>> KanjiIdsProvider = DefaultKanjiIds;

        // ---- State setup --------------------------------------------------------

        /// <summary>Ensure a <see cref="LevelState"/> exists for every defined level.
        /// Idempotent and non-destructive: it never resets existing flags and never
        /// duplicates. The entry level (no prerequisite) is marked unlocked. Safe on
        /// legacy saves that had no level list.</summary>
        public static void Ensure(PlayerProgress p)
        {
            if (p?.learning?.levelProgress == null) return;
            var lp = p.learning.levelProgress;
            foreach (var def in LevelCatalog.All)
            {
                var st = lp.GetOrAdd(def.Id);
                if (def.UnlockPrereq == null) st.unlocked = true; // entry level always open
            }
        }

        // ---- Queries ------------------------------------------------------------

        public static bool IsUnlocked(PlayerProgress p, PlayerLevel level)
        {
            Ensure(p);
            var st = p.learning.levelProgress.Find(level);
            return st != null && st.unlocked;
        }

        public static bool IsCompleted(PlayerProgress p, PlayerLevel level)
        {
            Ensure(p);
            var st = p.learning.levelProgress.Find(level);
            return st != null && st.completed;
        }

        /// <summary>How many playable kanji a level currently has (0 if the level is not
        /// defined or its dataset is missing/empty). Safe — never throws.</summary>
        public static int ContentCount(PlayerLevel level)
        {
            var def = LevelCatalog.Get(level);
            if (def == null) return 0;
            try { return KanjiIdsProvider?.Invoke(def.Dataset)?.Count ?? 0; }
            catch { return 0; } // missing/empty dataset → treated as no content
        }

        /// <summary>True if the level has gameplay content. A defined-but-empty or
        /// not-yet-authored level (e.g. Adept) reports false and must not be playable.</summary>
        public static bool HasContent(PlayerLevel level) => ContentCount(level) > 0;

        /// <summary>The gate for actually starting a level: it must be unlocked AND have
        /// content. Prevents entering a locked level or one with no kanji.</summary>
        public static bool IsPlayable(PlayerProgress p, PlayerLevel level) =>
            IsUnlocked(p, level) && HasContent(level);

        /// <summary>(kanji at/over threshold, total kanji) for a level — live from mastery.</summary>
        public static (int done, int total) Counts(PlayerProgress p, PlayerLevel level)
        {
            var def = LevelCatalog.Get(level);
            if (def == null || p?.learning?.kanjiMastery == null) return (0, 0);
            var ids = KanjiIdsProvider != null ? KanjiIdsProvider(def.Dataset) : null;
            if (ids == null) return (0, 0);
            int done = 0;
            foreach (var id in ids)
                if (p.learning.kanjiMastery.GetScore(id) >= MasteryThreshold) done++;
            return (done, ids.Count);
        }

        /// <summary>Live completion progress in [0,1] for UI.</summary>
        public static float Progress01(PlayerProgress p, PlayerLevel level)
        {
            var (done, total) = Counts(p, level);
            return total > 0 ? (float)done / total : 0f;
        }

        /// <summary>Message explaining how to unlock a level, or "" if it has no
        /// prerequisite. E.g. "Complete Rising Star to unlock Young Master."</summary>
        public static string RequirementText(PlayerLevel level)
        {
            var def = LevelCatalog.Get(level);
            if (def?.UnlockPrereq == null) return "";
            return $"Complete {LevelCatalog.DisplayName(def.UnlockPrereq.Value)} " +
                   $"to unlock {LevelCatalog.DisplayName(def.Id)}.";
        }

        // ---- Advancement --------------------------------------------------------

        /// <summary>Recompute completion from mastery and unlock any dependent levels.
        /// Flags are latched (only ever set true). Returns true if anything changed.
        /// Does not touch mastery, XP, sessions or streaks.</summary>
        public static bool Evaluate(PlayerProgress p)
        {
            if (p?.learning?.levelProgress == null) return false;
            Ensure(p);
            bool changed = false;

            // 1) latch completion for levels that now meet the rule
            foreach (var def in LevelCatalog.All)
            {
                var st = p.learning.levelProgress.Find(def.Id);
                if (st != null && !st.completed && MeetsCompletion(p, def))
                {
                    st.completed = true;
                    changed = true;
                }
            }

            // 2) latch unlocks for levels whose prerequisite is now completed
            foreach (var def in LevelCatalog.All)
            {
                var st = p.learning.levelProgress.Find(def.Id);
                if (st == null || st.unlocked) continue;
                bool open = def.UnlockPrereq == null || IsCompleted(p, def.UnlockPrereq.Value);
                if (open) { st.unlocked = true; changed = true; }
            }

            return changed;
        }

        /// <summary>Evaluate the shared progress and persist only if something changed.
        /// Called after a session finishes and when the menu opens, so a newly earned
        /// unlock is reflected automatically.</summary>
        public static bool EvaluateAndPersist()
        {
            var service = PlayerProgressService.Default;
            bool changed = Evaluate(service.Current);
            if (changed) service.Save();
            return changed;
        }

        private static bool MeetsCompletion(PlayerProgress p, LevelDefinition def)
        {
            var (done, total) = Counts(p, def.Id);
            if (total <= 0) return false;
            int need = Mathf.CeilToInt(total * CompletionFraction);
            return done >= need;
        }

        private static IReadOnlyList<int> DefaultKanjiIds(KanjiLevel level)
        {
            var db = Data.KanjiDatabase.Load(level == KanjiLevel.N4 ? "kanji_n4" : "kanji_n5");
            var ids = new List<int>(db.Count);
            foreach (var k in db.All) ids.Add(KanjiId.Of(k.Character));
            return ids;
        }
    }
}
