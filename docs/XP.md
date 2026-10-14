# Global XP (V1)

Global XP measures the player's **overall learning activity and progression**. It is a
distinct, third concept — kept separate from the other two:

| Concept          | Measures                                   | Lives in |
|------------------|--------------------------------------------|----------|
| Kanji Mastery    | how well a single kanji is known           | `Learning.KanjiMastery` |
| Level Progression| which player-facing levels are unlocked    | `Learning.LevelProgress` |
| **Global XP**    | overall learning activity/progression      | `Progression.XP` |

XP never replaces mastery, and mastery total is never used as XP. XP does not unlock
levels (levels remain mastery-gated — see `docs/PROGRESSION.md`).

## Where XP is stored

```
PlayerProgress.Progression.XP
├── totalXp            int  — never negative
└── xpSystemVersion    int  — which XP ruleset produced this data (V1 = 1)
```

Persisted through the existing `PlayerProgress` JSON store — no separate PlayerPrefs key
and no second persistence system. On-disk schema is bumped 4 → 5 (additive); loading an
older save stamps `xpSystemVersion` without touching `totalXp`.

## XP sources (V1)

1. **Correct answers** — base XP × mastery multiplier.
2. **Normal session completion** — flat +50, once per completed normal session.
3. **Daily streak bonus** — once per calendar day.

Wrong answers award **0 XP** (mastery still updates as before).

### 1. Correct-answer XP

Base XP by answer mode and timer (timed = 2× untimed):

| Answer mode   | Untimed | Timed |
|---------------|---------|-------|
| Kanji→English | 10      | 20    |
| Kanji→Romaji  | 20      | 40    |
| Kanji→Kana    | 40      | 80    |

**Mastery multiplier** (rewards learning weaker/newer kanji more), applied to the base:

| Mastery score | Multiplier |
|---------------|------------|
| 0–2           | 1.50×      |
| 3–6           | 1.25×      |
| 7–12          | 1.00×      |
| 13–20         | 0.90×      |
| 21–40         | 0.75×      |
| 41+           | 0.50×      |

**Pre-answer mastery.** The multiplier uses the kanji's mastery score **before** the
answer's mastery change is applied — so you are rewarded for answering at the current
difficulty. Order of operations per correct answer: read pre-answer mastery → base →
multiplier(pre) → final XP → award XP → apply mastery change → persist. (Wrong answer:
0 XP → apply mastery change → persist.)

**Rounding.** `finalXp = round(base × multiplier)` to the nearest integer, ties away
from zero (`MidpointRounding.AwayFromZero`). No fractional XP is accumulated. Example:
English untimed at mastery 2 → 10 × 1.50 = **15**; at mastery 3 → 10 × 1.25 = 12.5 → **13**.

### 2. Session-completion bonus

**+50 XP** when a normal 15-question session actually completes. It is tied to the
session-completion event (the same authoritative commit as sessions/streak), not to the
Results screen rendering, and cannot be awarded twice for one completion.

### 3. Daily streak bonus

`streakBonus = min(streakDays × 10, 100)` — e.g. 1-day → +10, 5-day → +50, 10-day and
beyond → +100 (capped). Awarded **at most once per calendar day**, on the first
qualifying normal session of the day, using the existing Activity/Streak as the source
of truth (`ActivityService.HasActivityToday()` gates it). Multiple sessions the same day
do not repeat it.

## Revision and XP

Revision is a focused practice run, **not** a normal session (see `docs/REVISION.md`):

- Correct revision answers **do** award answer XP, using the untimed base (English 10 /
  Romaji 20 / Kana 40) × mastery multiplier — Revision is always untimed.
- Wrong revision answers award 0 XP.
- Revision awards **no** +50 session bonus and **no** daily streak bonus.
- Revision continues to bypass mastery-weighted question selection (unchanged).

## Architecture

```
XpConfig       — all numbers (base table, +50, streak 10/cap 100, multiplier bands, version)
XpCalculator   — pure functions: BaseAnswerXp, MasteryMultiplier, AnswerXp, SessionCompletionXp, StreakBonusXp
XpService      — awards into PlayerProgress + persists: AwardAnswerXp / AwardSessionCompletionXp /
                 AwardDailyStreakXp, and the AwardForCompletedSession seam
XpMigration    — version/compatibility layer (stamps version; future upgrades branch here)
```

Gameplay never contains formulas — it calls `XpService.AwardAnswerXp(...)` after
evaluating an answer, and `XpService.AwardForCompletedSession(counted, firstToday,
streak)` at the single end-of-run commit in `GameController.FinishRun`. XP is never
awarded from UI rendering, question generation, or scene transitions. The calculator is
fully unit-testable without a running scene.

## Versioning & duplicate protection

`xpSystemVersion` (V1 = `XpConfig.CurrentVersion`) is stored with `totalXp`. It is
independent of the on-disk `schemaVersion`. `XpMigration.Ensure` is the one place a
future ruleset (v2+) adds an upgrade branch; V1 has no predecessor so it only stamps a
missing version and never rewrites earned XP.

Duplicate awards are prevented by tying each award to its real event: answer XP to the
answer-evaluation event; the +50 to the `ActivityService` session-completion commit
(which de-dupes duplicate/re-entrant completions → `sessionCounted = false`); the streak
bonus to the first activity of a new calendar day. XP is only ever added, so `totalXp`
can never go negative.

## Future extensibility

New sources (daily challenge, achievement, level-completion, first-time-kanji, event XP)
can be added as new `XpConfig` entries + `XpCalculator` functions + `XpService.Award…`
methods, and future rule changes go through `xpSystemVersion` / `XpMigration` — without
rewriting the system. None of those are implemented here.
