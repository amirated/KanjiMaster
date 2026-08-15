namespace KanjiMaster.Progression
{
    /// <summary>
    /// Minimal migration infrastructure for the persisted <see cref="PlayerProgress"/>.
    /// Today there is a single version (1), so no real migration runs — but the
    /// version field and the migration loop exist so future shape changes have an
    /// obvious, safe place to hook in.
    ///
    /// Rules:
    ///   • schemaVersion &lt;= 0  → legacy/unversioned save → treated as v1 (no shape change).
    ///   • schemaVersion &gt; Current → a save from a newer app → cannot downgrade → fail.
    ///   • otherwise step forward one version at a time via the switch below.
    /// </summary>
    public static class ProgressMigration
    {
        /// <summary>Current on-disk schema version. Bump this when PlayerProgress changes shape.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>
        /// Migrate <paramref name="progress"/> in place up to <see cref="CurrentSchemaVersion"/>.
        /// Returns true on success; false if the save cannot be migrated (unknown/newer version).
        /// </summary>
        public static bool TryMigrate(PlayerProgress progress)
        {
            if (progress == null) return false;

            // Legacy saves predate the version field and deserialize to 0.
            if (progress.schemaVersion < 1) progress.schemaVersion = 1;

            // A save newer than this build understands must not be silently downgraded.
            if (progress.schemaVersion > CurrentSchemaVersion) return false;

            while (progress.schemaVersion < CurrentSchemaVersion)
            {
                switch (progress.schemaVersion)
                {
                    // Future migrations go here, e.g.:
                    // case 1: MigrateV1ToV2(progress); progress.schemaVersion = 2; break;
                    default:
                        return false; // no migration path defined for this version
                }
            }

            return progress.schemaVersion == CurrentSchemaVersion;
        }
    }
}
