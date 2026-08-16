# Revision (V1)

Revision is a focused, mistake-only practice run derived from the normal game the
player just finished. It reinforces exactly what they got wrong, without interfering
with normal sessions, streaks, level progression, or question selection.

## Flow

```
Normal 15-question game → Results
   ├─ zero mistakes  → normal numeric Results (no Revise button)
   └─ 1+ mistakes    → normal numeric Results + Revise
                          → Revision session (mistakes only)
                             → Revision Results: "Well Done!"
                                → Play Again  |  Main Menu
```

There is deliberately no `Revision → Revise → Revision` recursion: Revision Results
never shows a Revise button.

## What a Revision contains

The exact set of kanji answered incorrectly in the immediately preceding normal
session, in the order the mistakes occurred, each **at most once**.

- Source: `GameSession.IncorrectKanji()` of that session, de-duplicated by
  `Revision.KanjiFor`. Normal runs already use 15 **unique** kanji, so a kanji can be
  wrong at most once and duplicates cannot arise upstream — the de-dup is purely
  defensive.
- It never adds unrelated kanji and never uses the player's mastery score to choose or
  reorder mistakes.

## Bypasses normal question selection

Normal selection draws kanji from the level pool (`QuestionPool.SelectUnique`).
Revision instead uses `QuestionPool.SelectByChars(mistakes)` — the exact characters,
in order. It never goes through the normal selector, so it is fully deterministic and
mistake-focused. (Note: normal selection is currently a uniform shuffle; there is no
mastery-weighted selector, so there is nothing extra to bypass — but Revision would
bypass it regardless because it selects by explicit characters.)

## Always untimed; inherits the answer mode

`Revision.ConfigFor` clones the preceding config, forces `Timer = Untimed`, and
preserves `Answer` and `Level`:

| Preceding normal game        | Revision            |
|------------------------------|---------------------|
| Kanji→English, Timed/Untimed | Kanji→English, Untimed |
| Kanji→Romaji,  Timed/Untimed | Kanji→Romaji,  Untimed |
| Kanji→Kana,    Timed/Untimed | Kanji→Kana,    Untimed |

Level context is preserved implicitly (the mistakes come from that level's session) and
explicitly (the cloned config keeps `Level`), so Revision can never introduce kanji from
another level.

## Mastery scoring

Revision answers are real attempts and update per-kanji mastery using the **untimed**
profile for the inherited answer mode (English +2/−1, Romaji +4/−3, Kana +8/−5). Timed
scoring is never applied during Revision. The mastery scoring implementation and its
edge cases are unchanged — see the scoring rules in the mastery code
(`ScoringProfileResolver` / `MasteryScoring`).

## Not a normal session

Revision does **not** touch the Activity system. `ActivityService`
`RecordNormalSessionCompleted` ignores any `IsRevision` run, so Revision never:
increments completed normal sessions, and never extends/resets/updates the daily streak
or the last-active date. See `docs/ACTIVITY.md`.

## Level progression

Level progression is mastery-based, not session-based. Revision does not count as a
normal game and grants no session credit; it only affects progression to the same
extent that it legitimately moves per-kanji mastery (existing behaviour, unchanged).
No new progression rule is introduced. See `docs/PROGRESSION.md`.

## Results presentation

`ResultsPresenter.For(session)` decides what Results shows (pure, unit-tested):

- **Normal run** → numeric results (score / correct / incorrect / total / accuracy /
  best combo), plus Revise only when there are mistakes.
- **Revision run** → only **"Well Done!"**, with Play Again and Main Menu. No score,
  no counts, no percentage, no Revise.

`ResultsController` reuses the existing Results scene and switches between the two
states (optional `normalResultsGroup` / `wellDoneGroup` containers, with a defensive
blanking of the numeric labels so no numbers appear on Revision even if the scene has
not been split into those groups).

## Navigation

- **Play Again** (`SessionContext.QueueReplayNormal`) returns to the **normal** flow
  using the last normal config; it clears any revision state, so it never launches
  another Revision.
- **Main Menu** returns normally; the next normal game uses the currently selected
  level/mode from the menu.

## State lifetime & edge cases

- Revision state lives only in `SessionContext` (`NextConfig` / `NextKanji` /
  `NextIsRevision`) for the one scene transition Results → Game. `QueueNormal` clears
  `NextKanji`/`NextIsRevision`, so **Game A's mistakes can never leak into Game B**.
- `QueueRevision` does not modify `LastNormalConfig` or `LastSession`, so abandoning a
  Revision leaves the preceding normal session intact and Play Again still works.
- **Empty request guard:** `QueueRevision` returns false and starts nothing when there
  are no mistakes, so an empty Revision can never begin a fake/invalid run.
- **Abandoned Revision:** if it never reaches Results, no commit runs — activity,
  streak, and session records are untouched.

## Tests

`Assets/Scripts/Tests/RevisionTests.cs` covers Revise exposure, the exact/deduped
mistake list, untimed + answer-mode/level inheritance across all three modes and both
timers, the untimed scoring profile, mastery-independent selection, separation from
sessions/streak/progression, the "Well Done!" presentation (no numbers, no Revise, with
Play Again / Main Menu), non-recursive Play Again, clean next-normal state / no mistake
leak, no mutation of the preceding session, and the empty-request guard.
`GameplaySessionTests` continues to cover the base revision derivation.
