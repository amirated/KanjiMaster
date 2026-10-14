# Persistence & save versioning

How player data is stored, versioned, and migrated. The goal is a small, reliable
versioning boundary so the save format can evolve without losing existing progress.

## Domain vs persistence

- **Domain model:** `PlayerProgress` (Profile / Learning{KanjiMastery, LevelProgress} /
  Activity{Sessions, Streak} / Progression{XP}). This is what gameplay uses and is not
  reshaped for persistence.
- **Persistence representation:** `SaveData` — a small versioned envelope that wraps the
  domain model on disk:

```json
{
  "version": 1,
  "playerProgress": { ...PlayerProgress... }
}
```

`SaveData.version` is the **save-format version** (this task introduces version **1**).
It is separate from the inner `PlayerProgress.schemaVersion`, which continues to migrate
the domain model's *shape* (additive fields) and to load pre-envelope saves.

## Storage mechanism

A single JSON **file** at `Application.persistentDataPath/player_progress.json`, written
atomically (temp file → `File.Replace` keeping a `.bak`). `LocalPlayerProgressStore` is
the only class that knows the path/format; everything else goes through
`PlayerProgressService`. (Note: this is a file, not PlayerPrefs — the existing mechanism
is preserved.) A future `BackendSaveRepository` could implement the same
`IPlayerProgressStore` without touching gameplay.

## Save flow

`LocalPlayerProgressStore.Save` → `SaveDataMigration.Serialize(progress)` stamps the
inner `schemaVersion`, wraps the progress in `SaveData { version = CurrentVersion }`, and
serializes the whole envelope as one coherent object → atomic write. The version field
is always written together with the data.

## Load flow

`LocalPlayerProgressStore.Load` → `SaveDataMigration.Load(json)` returns a
`(PlayerProgress, SaveLoadStatus)`:

```
raw json
  ├─ empty                          → Empty
  ├─ envelope (has playerProgress)
  │    ├─ version < 1               → Corrupt
  │    ├─ version > current         → UnsupportedFutureVersion
  │    ├─ inner schemaVersion > current → UnsupportedFutureVersion
  │    └─ otherwise                 → migrate (sequential) + normalize → Ok
  └─ legacy (no envelope)           → ProgressMigration.FromJson → Ok / Corrupt
```

`Normalize` seeds any additive inner fields introduced since the save was written (level
states, longest-streak heal, XP ruleset version) and rebuilds runtime indexes — so both
envelope and legacy loads end up identical and current. Migration transforms **data
only** (no XP/mastery/level/streak/gameplay logic) and is deterministic.

## Legacy / unversioned saves

The pre-envelope format is a **root `PlayerProgress`** (with `schemaVersion` inside), and
before that a flat v0/v1 format. Neither has a `"playerProgress"` envelope key, so they
are detected as legacy and loaded through the existing `ProgressMigration` — no data is
lost. Saving afterward re-writes them as the versioned envelope. (There is no fake
"→ v1" migration; the existing data is simply loaded and then re-serialized in the new
wrapper.)

## Migration architecture (future)

Envelope migrations are **sequential** (v1 → v2 → v3 …), one step at a time in
`SaveDataMigration.MigrateEnvelope`. V1 is current, so it is a pass-through today. A
future field (achievements, onboarding, settings, new metrics) adds one step there;
purely additive inner-model fields can also continue to be seeded via the `Normalize`
hook. Do not migrate old versions directly to the latest — chain the steps.

## Future & corrupted saves

- **Future save version** (envelope `version` or inner `schemaVersion` newer than this
  build) → `UnsupportedFutureVersion`. The app does **not** downgrade or interpret it;
  it falls back to a fresh default in memory and preserves the file (a `.corrupt` copy is
  kept). Load never writes, so the on-disk future save is not overwritten by loading.
- **Malformed JSON / invalid version / unmigratable** → `Corrupt`; same safe fallback,
  file preserved. Malformed data never crashes the app and never silently resets the
  save on load.
- **Limitation / tech debt:** preservation is a single `.corrupt` copy (the existing
  recovery mechanism). After a future/corrupt load the app runs on a default in memory;
  if the player then plays and a normal save occurs, the *primary* file is overwritten
  (the `.corrupt` copy remains). A richer backup/restore/version-quarantine flow is a
  separate task.

## Default player

No save file → `CreateDefault()` builds a fresh `PlayerProgress` (Rising Star unlocked,
zero XP/sessions/streak) via the existing initialization. This is **not** a migration and
is distinguishable from a loaded save (which carries data).

## Backward compatibility

Existing players keep their Kanji mastery, level progression, sessions, streak, XP, and
profile across the introduction of the envelope: legacy saves load through the same
migration/seed path and are re-saved in the versioned wrapper unchanged. XP in particular
(recently added) survives — it is part of the wrapped `PlayerProgress` and is never
recomputed by migration.

## Key types

- `SaveData` — the `{ version, playerProgress }` envelope (`CurrentVersion = 1`).
- `SaveDataMigration` — `Serialize` + versioned `Load` (envelope/legacy detection, future
  vs corrupt), plus the sequential envelope-migration seam.
- `ProgressMigration` — inner-schema migration for pre-envelope/flat saves + `Normalize`.
- `LocalPlayerProgressStore` — file I/O, atomic write, `.corrupt`/`.bak` preservation.
