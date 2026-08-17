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

| Player-facing name | Internal id (`PlayerLevel`) | Dataset | Order | JLPT* | Unlock prerequisite |
|--------------------|-----------------------------|---------|-------|-------|---------------------|
| Rising Star        | `RisingStar`                | N5      | 0     | N5    | none (default)      |
| Young Master       | `YoungMaster`               | N4      | 1     | N4    | complete Rising Star |

\* JLPT is **internal metadata only** (`LevelDefinition.JlptEquivalent`) — the UI uses the
player-facing names, never N5/N4. The remaining tiers map Adept→N3, Expert→N2, Master→N1,
Legend→beyond N1 and exist in the `PlayerLevel` enum, but are **not** in the catalog until
they have content.

Adding a level later = appending one `LevelDefinition` (id, display name, dataset,
prerequisite, order, JLPT). The progression service, question selection and the menu all
read the catalog, so nothing branches on a specific level. `PlayerLevel` is the internal
id and is serialized by its **stable integer value**, so renaming a member never breaks
saves. Adding Adept/Expert/Master/Legend is a content/config task: add a `KanjiLevel`
dataset value + `kanji_n*.json`, then one catalog row — no changes to selection, mastery,
progression, or save logic.

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
MainMenu (Play) ─ pick Level (must be playable) → GameConfig.Level = definition.Dataset
Game builds run ─ level pool → mastery weighting → 15 unique (see pipeline below)
Game finishes  ─ MasteryService records answers (unchanged)
               → LevelProgressionService.EvaluateAndPersist()  ← latch completion/unlock
MainMenu opens ─ EvaluateAndPersist() again → toggles reflect the new unlocked state
```

- Persisted under `PlayerProgress.Learning.LevelProgress`:
  `currentLevel` (last selected) + a `LevelState { level, unlocked, completed }` list.
- `LevelProgressionService` owns all rules and only **reads** mastery. It does **not**
  touch mastery scoring, XP, sessions, streaks or the Revision flow, and it does **not**
  do question selection.

## Question-selection pipeline

```
Current level → QuestionPool.Load(level)   (that level's dataset ONLY — eligibility first)
             → MasteryWeightedSelector.SelectUnique(pool, 15, masteryOf, rng)
                 · weight by mastery band (low mastery → high weight; never zero)
                 · pick WITHOUT replacement → 15 unique, no repeats
             → QuestionGenerator builds options
```

Level eligibility comes **first** (the pool is a single level's dataset, so weighting can
never pull in another level's kanji), mastery weighting **second**. Weights (config in
`MasteryWeightedSelector.Bands`): mastery 0–2 → 32, 3–6 → 16, 7–12 → 8, 13–20 → 4,
21–40 → 2, 41+ → 1. So weak/new kanji are strongly favoured and well-known ones are rare,
but every weight ≥ 1 so nothing is impossible to draw. Question selection never unlocks
levels — that is `LevelProgressionService`'s job, after the run.

Revision is unchanged: it uses `QuestionPool.SelectByChars(mistakes)` (the exact,
deterministic mistake set), bypassing weighting entirely — see `docs/REVISION.md`.

## Pool edge cases

- **Fewer than 15 eligible kanji** → the run contains all of them (min(15, poolSize)),
  still unique. It never pads with duplicates. (N5=84, N4=162 today, so full 15-question
  runs are guaranteed; smaller future levels degrade gracefully during development.)
- **All kanji high mastery** → weights fall to 1 (uniform among them); a full, unique
  session is still produced. No empty result, retry loop, or repeat.
- **Empty / missing dataset** → selection returns empty and the level is **not playable**.
  `LevelProgressionService.HasContent(level)` (safe — never throws) and
  `IsPlayable(p, level) = IsUnlocked && HasContent` gate this; MainMenu falls back to the
  default entry level, so a content-less level (e.g. an unfinished Adept) can never start.
- **Data integrity** is validated by the existing pipeline (`tools/test_dataset.py`):
  unique ids, valid readings/meanings, correct level assignment, and no cross-level
  overlap.

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
