using UnityEngine;

namespace KanjiMaster.Progression
{
    /// <summary>Outcome of interpreting a raw save string.</summary>
    public enum SaveLoadStatus
    {
        Ok,                       // loaded (and migrated/normalized) successfully
        Empty,                    // no content
        Corrupt,                  // malformed / invalid version / unmigratable
        UnsupportedFutureVersion, // written by a newer app — must NOT be downgraded
    }

    public readonly struct SaveLoadResult
    {
        public readonly PlayerProgress Progress;
        public readonly SaveLoadStatus Status;
        public SaveLoadResult(PlayerProgress progress, SaveLoadStatus status)
        {
            Progress = progress;
            Status = status;
        }
    }

    /// <summary>
    /// The SAVE-ENVELOPE versioning boundary. Serializes <see cref="PlayerProgress"/>
    /// inside a versioned <see cref="SaveData"/> wrapper, and loads any known form:
    ///
    ///   • a current envelope            → migrate (sequentially) + normalize
    ///   • a future envelope             → UnsupportedFutureVersion (never downgraded)
    ///   • a pre-envelope legacy save    → delegate to <see cref="ProgressMigration"/>
    ///     (the current root-PlayerProgress format, or the old flat v0/v1 format)
    ///   • malformed / empty             → Corrupt / Empty
    ///
    /// It transforms persisted DATA only — no XP/mastery/level/streak/gameplay logic —
    /// and is deterministic: the same input always yields the same output.
    /// </summary>
    public static class SaveDataMigration
    {
        /// <summary>Produce the versioned save string (one coherent envelope object).</summary>
        public static string Serialize(PlayerProgress progress)
        {
            // Keep the inner shape stamp so pre-envelope readers and inner migration
            // continue to work; the envelope adds the outer save version.
            progress.schemaVersion = ProgressMigration.CurrentSchemaVersion;
            var env = new SaveData { version = SaveData.CurrentVersion, playerProgress = progress };
            return JsonUtility.ToJson(env, true);
        }

        /// <summary>Interpret a raw save string into a current PlayerProgress + status.</summary>
        public static SaveLoadResult Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new SaveLoadResult(null, SaveLoadStatus.Empty);

            // Detect the envelope by its unique key in the RAW json. We cannot rely on
            // JsonUtility here: it ALWAYS instantiates nested [Serializable] class fields,
            // so `SaveData.playerProgress` is never null even for a legacy root save —
            // checking it would misread every legacy save as a version-0 envelope.
            if (!LooksLikeEnvelope(json))
                return LoadLegacy(json); // pre-envelope root PlayerProgress, or flat v0/v1

            SaveData env;
            try { env = JsonUtility.FromJson<SaveData>(json); }
            catch { return new SaveLoadResult(null, SaveLoadStatus.Corrupt); }

            if (env == null || env.playerProgress == null)
                return new SaveLoadResult(null, SaveLoadStatus.Corrupt);

            return LoadEnvelope(env);
        }

        /// <summary>True if the raw save is the versioned envelope. Keys off the unique
        /// top-level "playerProgress" field (no PlayerProgress field is named that), so it
        /// reliably distinguishes an envelope from a legacy root PlayerProgress.</summary>
        private static bool LooksLikeEnvelope(string json) => json.Contains("\"playerProgress\"");

        private static SaveLoadResult LoadEnvelope(SaveData env)
        {
            if (env.version < 1)
                return new SaveLoadResult(null, SaveLoadStatus.Corrupt); // invalid version

            if (env.version > SaveData.CurrentVersion)
                return new SaveLoadResult(null, SaveLoadStatus.UnsupportedFutureVersion);

            var p = env.playerProgress;

            // Guard the INNER shape too: a future inner schema must not be downgraded.
            if (p.schemaVersion > ProgressMigration.CurrentSchemaVersion)
                return new SaveLoadResult(null, SaveLoadStatus.UnsupportedFutureVersion);

            try
            {
                p = MigrateEnvelope(env.version, p); // sequential envelope migrations (none for v1)
                ProgressMigration.Normalize(p);      // seed additive inner fields + rebuild indexes
                return new SaveLoadResult(p, SaveLoadStatus.Ok);
            }
            catch { return new SaveLoadResult(null, SaveLoadStatus.Corrupt); }
        }

        private static SaveLoadResult LoadLegacy(string json)
        {
            try
            {
                // Pre-envelope root PlayerProgress (schemaVersion inside) OR flat v0/v1.
                var p = ProgressMigration.FromJson(json);
                return p != null
                    ? new SaveLoadResult(p, SaveLoadStatus.Ok)
                    : new SaveLoadResult(null, SaveLoadStatus.Corrupt);
            }
            catch { return new SaveLoadResult(null, SaveLoadStatus.Corrupt); }
        }

        /// <summary>
        /// Sequential envelope migrations. V1 is current, so this is a pass-through.
        /// A future version adds ONE step at a time here, e.g.:
        ///   if (fromVersion == 1) { p = V1ToV2(p); fromVersion = 2; }
        ///   if (fromVersion == 2) { p = V2ToV3(p); fromVersion = 3; }
        /// Each step only transforms persisted data and must be deterministic.
        /// </summary>
        private static PlayerProgress MigrateEnvelope(int fromVersion, PlayerProgress p)
        {
            // No transform needed while CurrentVersion == 1.
            return p;
        }
    }
}
