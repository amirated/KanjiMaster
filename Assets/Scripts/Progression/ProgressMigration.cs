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
    /// Versions: 0 = legacy/unversioned (flat), 1 = flat + schemaVersion, 2 = nested.
    /// Future migrations add a step in the v2→v3 style; the entry point stays here.
    /// </summary>
    public static class ProgressMigration
    {
        /// <summary>Current on-disk schema version. Bump when PlayerProgress changes shape.</summary>
        public const int CurrentSchemaVersion = 2;

        [Serializable] private class SchemaProbe { public int schemaVersion; }

        public static PlayerProgress FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            int version;
            try { version = JsonUtility.FromJson<SchemaProbe>(json)?.schemaVersion ?? 0; }
            catch { return null; }

            try
            {
                if (version <= 1)
                {
                    // Legacy flat format (0 and 1 share the same shape).
                    return MapV1ToV2(JsonUtility.FromJson<LegacyPlayerProgressV1>(json));
                }
                if (version == CurrentSchemaVersion)
                {
                    var p = JsonUtility.FromJson<PlayerProgress>(json);
                    if (p == null) return null;
                    p.schemaVersion = CurrentSchemaVersion;
                    p.RebuildIndexes();
                    return p;
                }
                return null; // newer than this build understands — do not downgrade
            }
            catch { return null; }
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
            // No legacy level data existed → learning.levelProgress stays Novice.
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
