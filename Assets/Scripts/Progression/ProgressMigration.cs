using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Reads a raw save (any known schema version) and returns a current-version
    /// <see cref="PlayerProgress"/>, or null if it cannot be read/migrated.
    ///
    /// Because JsonUtility deserializes into the TARGET type (dropping fields the
    /// target lacks), a shape change can't be migrated by loading into the new class.
    /// So we peek the version, and for old (flat) saves we read a legacy DTO first,
    /// then map it into the nested model — preserving all data.
    ///
    /// Versions: 0 = legacy/unversioned (flat), 1 = flat + schemaVersion,
    ///           2 = nested model, 3 = nested + per-level LevelProgress,
    ///           4 = + Activity streak (longestStreak; active streak tracking),
    ///           5 = + XP system version (Progression.XP.xpSystemVersion).
    /// Each of these upgrades is purely additive, so an older nested save loads into the
    /// current class directly and only needs its new fields seeded (level states, streak
    /// heal, XP version). Note: the XP RULESET has its own version (XpConfig.CurrentVersion)
    /// tracked inside the save, independent of this on-disk schema version.
    /// </summary>
    public static class ProgressMigration
    {
        /// <summary>Current on-disk schema version. Bump when PlayerProgress changes shape.</summary>
        public const int CurrentSchemaVersion = 5;

        [Serializable] private class SchemaProbe { public int schemaVersion; }

        public static PlayerProgress FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            int version;
            try { version = JsonUtility.FromJson<SchemaProbe>(json)?.schemaVersion ?? 0; }
            catch { return null; }

            try
            {
                PlayerProgress p;
                if (version <= 1)
                {
                    // Legacy flat format (0 and 1 share the same shape).
                    p = MapV1ToV2(JsonUtility.FromJson<LegacyPlayerProgressV1>(json));
                }
                else if (version <= CurrentSchemaVersion)
                {
                    // v2 and v3 share the nested C# shape; missing fields (e.g. the v2
                    // save has no `levels`) deserialize to sensible empties.
                    p = JsonUtility.FromJson<PlayerProgress>(json);
                }
                else
                {
                    return null; // newer than this build understands — do not downgrade
                }

                if (p == null) return null;
                Normalize(p);
                return p;
            }
            catch { return null; }
        }

        /// <summary>
        /// Bring a loaded PlayerProgress to the current INNER schema: stamp the version,
        /// seed any additive fields introduced since the save was written (level states,
        /// longest-streak heal, XP ruleset version), and rebuild runtime indexes. Data
        /// only — no XP/mastery/level/streak awarding. Shared by legacy loads and the
        /// save-envelope loader so both paths seed new fields identically.
        /// </summary>
        public static void Normalize(PlayerProgress p)
        {
            if (p == null) return;
            p.schemaVersion = CurrentSchemaVersion;   // bring the version forward
            LevelProgressionService.Ensure(p);        // seed level states (new/legacy)
            ActivityService.EnsureConsistent(p);      // seed longestStreak (new/legacy)
            XpMigration.Ensure(p);                    // stamp XP ruleset version (no total change)
            p.RebuildIndexes();
        }

        /// <summary>Map the old flat model into the new nested model, preserving all
        /// existing data. Mastery keys move from character → code-point id (lossless).</summary>
        private static PlayerProgress MapV1ToV2(LegacyPlayerProgressV1 old)
        {
            if (old == null) return null;

            var p = new PlayerProgress { schemaVersion = CurrentSchemaVersion };

            if (old.Mastery != null)
            {
                foreach (var e in old.Mastery)
                    if (e != null && !string.IsNullOrEmpty(e.kanji))
                        p.learning.kanjiMastery.SetScore(KanjiId.Of(e.kanji), e.score);
            }

            p.activity.sessions.totalSessions = old.SessionsPlayed;
            p.activity.streak.currentStreak = old.DailyStreak;
            p.activity.streak.lastPlayedDate = old.LastPlayedDate;
            p.progression.xp.totalXp = old.Xp;
            // No legacy level data existed → level states are seeded by the caller
            // (FromJson → LevelProgressionService.Ensure): Rising Star unlocked.
            // No legacy profile data existed → profile stays empty.

            p.RebuildIndexes();
            return p;
        }
    }

    // --- Legacy flat schema (v0/v1) — read-only, used only by migration ----------
    [Serializable]
    internal class LegacyPlayerProgressV1
    {
        public int schemaVersion;
        public int SessionsPlayed;
        public int DailyStreak;
        public string LastPlayedDate;
        public int Xp;
        public List<LegacyKanjiEntry> Mastery;
    }

    [Serializable]
    internal class LegacyKanjiEntry
    {
        public string kanji;
        public int score;
    }
}
