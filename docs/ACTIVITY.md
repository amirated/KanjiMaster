# Activity tracking (V1)

How the game records **completed normal sessions** and the **daily streak**. This is
the Activity portion of `PlayerProgress`. It deliberately provides clean inputs for the
future Global XP system **without** implementing XP here, and it is decoupled from
mastery scoring, level progression, question selection and UI.

## What counts as a completed session

A completed session is **one normal 15-question gameplay run that reaches the Results
state**:

```
Game → question 15 answered → Results → commit
```

The following do **not** count: opening the Game scene, starting then abandoning a run,
returning to Main Menu before completion, closing/killing the app mid-run, any
incomplete game, or merely opening Learn. **Revision does not count as a normal session.**

There is exactly one authoritative commit: `GameController.FinishRun()` calls
`ActivityService.RecordNormalSessionCompleted(session)` once the last question is
answered. Per-answer mastery is recorded separately during play and is unchanged.

## Activity data in PlayerProgress

Stored under `PlayerProgress.Activity` (existing JSON persistence — no new mechanism):

```
Activity
├── Sessions.totalSessions   int    — completed normal sessions
└── Streak
    ├── currentStreak        int    — consecutive-day streak
    ├── longestStreak        int    — best streak ever (never decreases)
    └── lastPlayedDate       string — last active date, "yyyy-MM-dd" (invariant)
```

`lastPlayedDate` keeps its original field name for backward compatibility but is the
"last active date" (the last calendar date a normal session was completed). Total
questions answered is intentionally **not** tracked in V1 (unnecessary for now).

## Streak rules

Streaks are based on completed normal sessions and compared by **calendar date**, not
elapsed 24-hour periods (`StreakCalculator`, a pure function):

- **First-ever completion** → `currentStreak = 1`, `longestStreak = 1`, `lastActive = today`.
- **Same calendar day** (another session today) → `currentStreak` unchanged.
- **Next calendar day** → `currentStreak += 1`.
- **One or more days missed** → `currentStreak = 1`.
- `longestStreak` grows to match `currentStreak` and **never decreases** when a streak breaks.
- **Backwards device clock** (today earlier than the last active date) → `currentStreak`
  unchanged and the stored date is not moved backwards (defensive; V1 trusts local time).

`totalSessions` increments on **every** completed normal session, including extra
sessions on the same day (only the streak is day-gated).

## Date handling

V1 uses the **device's local calendar date** via an injectable `IClock` (`SystemClock`
in the game; a fake clock in tests). Dates are stored as `yyyy-MM-dd` with the invariant
culture, so locale never affects reading/writing. No server-authoritative time and no
UTC primary comparison. A session at Aug 16 23:55 followed by Aug 17 00:05 correctly
counts as two consecutive calendar days.

## How Revision stays separate

`ActivityService.RecordNormalSessionCompleted` ignores any session where `IsRevision` is
true, so a Revision run reaching its end never increments the normal session count or
touches the streak. Revision scoring and the Results/Revision UI are unchanged. Revision
answers still update per-kanji mastery (existing behaviour), just not normal activity.

## Incomplete / abandoned sessions

If a run never reaches Results (exit, app close, abandonment), the commit method is never
called, so `PlayerProgress` is left exactly as it was — no session credit, no streak
change.

## Duplicate protection

The commit is idempotent per run: `ActivityService` holds an in-memory reference to the
last-committed `GameSession`, so a duplicate completion callback or a revisited/reloaded
Results screen with the same session cannot double-count. A new run is a new session
object, so it always counts.

## Persistence & backward compatibility

Save schema bumped **3 → 4** (`ProgressMigration.CurrentSchemaVersion`); the change is
additive (`StreakData` gained `longestStreak`). On load, older saves:

- do not crash and are not discarded;
- get `longestStreak` seeded via `ActivityService.EnsureConsistent` (never below
  `currentStreak`);
- preserve existing kanji mastery, level progression and XP.

A brand-new or unreadable save starts at zero sessions / zero streak.

## Architecture

- `ActivityService` — the single commit point + read accessors (`TotalSessions`,
  `CurrentStreak`, `LongestStreak`, `LastActiveDate`) so the UI **asks** rather than
  computes.
- `StreakCalculator` — pure calendar-date streak math (no I/O, fully unit-tested).
- `IClock` / `SystemClock` — injectable time source for deterministic date tests.
- `ActivityDates` — locale-independent `yyyy-MM-dd` parse/format.

Activity logic touches none of: mastery scoring, level progression, XP, question
selection, or UI layout.

## Tests

`Assets/Scripts/Tests/ActivityTests.cs` covers new-player zeros; first session; multiple
same-day sessions; consecutive days; one and multiple missed days; incomplete and
abandoned runs; Revision not counting toward sessions or streak; save/load round-trip;
old saves without `longestStreak` loading safely and seeding it; and no double-counting
on Results revisit or duplicate completion events.
