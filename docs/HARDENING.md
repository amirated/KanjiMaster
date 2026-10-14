# Persistence hardening & edge-case behaviour

Intended behaviour for state/persistence edge cases, and the small guards added. The
success criterion: a player can play, revise, earn XP, close/reopen the app, navigate
scenes, and be interrupted without corrupting or unexpectedly changing PlayerProgress.

## Persistence model (why interruption is safe)

Every progress mutation persists immediately and atomically:

- Each answer → per-kanji mastery (`MasteryService.RecordAttempt`) and, if correct,
  answer XP (`XpService.AwardAnswerXp`) are written right away.
- Session completion → sessions, streak, +50 and streak XP are committed once at
  `GameController.FinishRun`.
- Writes go through `LocalPlayerProgressStore` which serializes the whole versioned
  envelope and swaps it in atomically (temp → `File.Replace` keeping `.bak`).

There is no buffered in-memory progress: `PlayerProgressService.Default.Current` equals
disk after each mutation. So backgrounding/suspension/termination cannot lose or corrupt
committed progress; at worst the current in-flight question is lost. No explicit
save-on-pause is needed (deliberate — avoids a new always-alive singleton). Resumable
games are out of scope for V1.

## Completion counts exactly once

`ActivityService.RecordNormalSessionCompleted` de-dupes by session reference (a
completed run is committed at most once) and ignores revision runs. `FinishRun` also has
a `_finished` guard so its commit + navigation run once even if reached twice. The +50
session bonus and daily streak bonus are gated on that single commit, so returning to or
reloading Results never re-awards anything (Results does no awarding — see XP below).

## Incomplete / abandoned games

A run that never reaches question 15 never calls `FinishRun`, so it awards no session,
no streak, and no completion XP. Answered-question mastery and answer XP already earned
remain (they are real attempts). Transient run state (`_index`, `_score`, combo, timer,
question list) lives on the `GameController` instance and is recreated per Game scene
load, so a new game always starts clean and never inherits a previous run's questions,
mistakes, score, or index. `SessionContext.QueueNormal` clears any revision state
(`NextKanji`, `NextIsRevision`).

## Revision

Revision contains only the immediately-preceding game's mistakes (deduped, exact set;
see `docs/REVISION.md`), bypasses normal selection, is always untimed, and does not
create a normal session, award +50, or touch the streak. Revision Results show only
"Well Done!" (no numeric results, no Revise button — no recursion). Zero-mistake games
show no Revise button (`ResultsPresenter`). `QueueRevision` refuses an empty request.

## Rapid / duplicate actions

- **Answers:** a question stops accepting input the instant it resolves (`_accepting`
  flag + disabled answer buttons), so the same answer can't be submitted twice and the
  timeout can't double-resolve.
- **Scene buttons:** `SceneLoader` ignores further transitions until the current one
  completes, so rapid taps on Play / Play Again / Main Menu / Revise trigger exactly one
  scene load (no double-queued runs, no double entry into Revision).

## Loaded-value validation

On load (`ProgressMigration.Normalize`, applied to every save), invalid invariants are
repaired without destroying valid data: `TotalXP` and `totalSessions` are clamped to
≥ 0; `currentStreak` ≥ 0 and `longestStreak` ≥ `currentStreak`. Missing nested
structures/collections cannot be null (JsonUtility instantiates serializable fields;
additive fields are seeded by the migration). No aggressive validation beyond established
invariants.

## XP consistency

XP changes only through `XpService` (answer / session / streak awards). Loading, saving,
returning to Main Menu, opening Results, and opening Revision award nothing. The Main
Menu `XpDisplay` is strictly read-only — it formats `XpService.TotalXp` and never writes.

## XP number formatting

`NumberFormat.Compact(long)` renders large values compactly for display only — it never
changes stored XP:

| Stored | Shown |
|--------|-------|
| 0 / 250 / 999 | 0 / 250 / 999 |
| 1000 / 1200 / 12500 | 1k / 1.2k / 12.5k |
| 999999 | 1M |
| 1000000 / 1250000 | 1M / 1.3M |
| 100000000 / 1000000000 | 100M / 1B |

Rules: exact integer below 1,000; otherwise k / M / B with 1 decimal while the scaled
value is < 100 (else 0 decimals), rounded half-away-from-zero, trailing ".0" dropped,
and a rounding carry promotes the unit (999,999 → "1M"). Negative inputs (not expected
for XP) format with a leading "-" and never crash. `TotalXP` is unchanged, e.g. stored
`1250000` still persists as `1250000` and only displays as `1.3M`.

## Known limitations (future work)

- No resumable in-progress game (partial run is discarded on termination) — acceptable
  for V1.
- Future/corrupt saves are preserved as a single `.corrupt` copy; a subsequent normal
  save overwrites the primary file (the copy remains). A richer backup/restore flow is a
  separate task (see `docs/PERSISTENCE.md`).
- Normal question selection is currently a uniform shuffle (no mastery weighting yet);
  unchanged by this task.
