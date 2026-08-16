# Level progression (V1)

How the game gates content behind player-facing **levels**, and how a level is
unlocked, tracked and completed. This is deliberately a **simple, documented V1** —
there was no pre-existing progression rule (scoring lives separately; see the mastery
scoring code), so the rule here is chosen to be easy to retune, not clever.

Scope: **Rising Star** (unlocked by default) and **Young Master** (initially locked).
The design is extensible to Adept / Expert / Master / Legend with no redesign, but
those levels have no content and are intentionally not defined yet.

## Levels are data, not code

Every level is a `LevelDefinition` in `LevelCatalog.All` — the single source of truth:

| Player-facing name | Internal id (`PlayerLevel`) | Dataset | Unlock prerequisite |
|--------------------|-----------------------------|---------|---------------------|
| Rising Star        | `RisingStar`                | N5      | none (default)      |
| Young Master       | `YoungMaster`               | N4      | complete Rising Star |

Adding a level later = appending one `LevelDefinition` (id, display name, dataset,
prerequisite). The progression service, question selection and the menu all read the
catalog, so nothing branches on a specific level. `PlayerLevel` is the internal id and
is serialized by its **stable integer value**, so renaming a member never breaks saves.

Kanji are associated with a level through the level's `Dataset` (the existing per-record
JLPT metadata / bundled dataset), keyed by the same stable Unicode code-point id used by
mastery (`KanjiId.Of`). No new per-kanji level field was introduced.

## The V1 rule

Two config knobs on `LevelProgressionService` (change here to retune — no logic edits):

- `MasteryThreshold` (default **2**) — a kanji *counts* when its mastery score is at or
  above this. A correct answer makes mastery an even positive; the smallest is 2, so 2
  means "answered correctly at least once."
- `CompletionFraction` (default **1.0**) — the fraction of a level's kanji that must
  count for the level to be **completed**. 1.0 = every kanji.

From these:

- **Progress** (`Progress01`) = `countAtThreshold / totalKanji`, computed **live** from
  mastery for display. It is never stored, so it can never go stale.
- **Completed** = progress meets `CompletionFraction`.
- **Unlocked** = the level has no prerequisite, or its prerequisite is completed.

`unlocked` and `completed` are **latched**: once earned they persist and are never
revoked, even if mastery later dips below the threshold (live progress may drop, but the
gate stays open). Rising Star, having no prerequisite, is always unlocked.

## Where it lives / how it flows

```
MainMenu (Play) ─ pick Level → sets GameConfig.Level = definition.Dataset
                              → QuestionPool.Load(level)  ← existing selection, unchanged
Game finishes  ─ MasteryService records answers (unchanged)
               → LevelProgressionService.EvaluateAndPersist()  ← latch completion/unlock
MainMenu opens ─ EvaluateAndPersist() again → toggles reflect the new unlocked state
```

- Persisted under `PlayerProgress.Learning.LevelProgress`:
  `currentLevel` (last selected) + a `LevelState { level, unlocked, completed }` list.
- `LevelProgressionService` owns all rules and only **reads** mastery. It does **not**
  touch mastery scoring, XP, sessions, streaks or the Revision flow.
- Question generation is unchanged: the level maps to a dataset (`KanjiLevel`), which
  feeds the existing `QuestionPool.Load(level)` selection.

## Backward compatibility & save versioning

Schema bumped **2 → 3** (`ProgressMigration.CurrentSchemaVersion`). The change is purely
additive (`LevelProgress` gained a `levels` list), so:

- Old **v2** saves load into the current model directly; the missing `levels` list is
  empty and `LevelProgressionService.Ensure` seeds defaults (Rising Star unlocked).
- Legacy **v0/v1** flat saves migrate as before, then get the same seeding.
- Mastery, sessions, streak and XP are **preserved**; nothing is wiped. A brand-new or
  unreadable save defaults to Rising Star unlocked, everything else locked.

## Editor wiring (MainMenu)

`MainMenuController` gained serialized slots. In the MainMenu scene:

1. Add two level toggles (**Rising Star**, **Young Master**) in one `ToggleGroup`
   (Allow Switch Off = off) and assign them to `risingStarToggle` / `youngMasterToggle`.
2. Add a `TMP_Text` for the lock message and assign it to `lockMessageText`. It shows
   "Complete Rising Star to unlock Young Master." while Young Master is locked and hides
   once unlocked. The Young Master toggle is made non-interactable while locked.

No other scene changes; the Play flow is Level → Answer mode → Timer → Start.

## Explicitly out of scope for V1

Mastery-weighted selection, a Progress screen, leaderboards, RevenueCat/auth, levels
beyond Young Master, and any UI redesign. The rule knobs and the catalog are the two
extension points.

## Tests

`Assets/Scripts/Tests/LevelProgressionTests.cs` covers default unlock/lock, the
requirement message, the progress metric and threshold boundary, completion, unlocking,
latching (unlock + completion survive mastery regression), idempotent evaluate/ensure,
the catalog dataset mapping and display names, v2→v3 backward compatibility (data
preserved), and persistence of the selected level.
