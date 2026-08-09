# Kanji Rush — Design

Living design doc for V0.1. Source of truth for gameplay and system decisions.

## Vision

A **competitive** game where players get better at Japanese by competing — closer
to Chess.com / Wordle / competitive Duolingo than to a flashcard app. Learning is
a side effect of wanting to climb the ranking. Core session ≈ 30 seconds; the loop
is tuned so the player immediately wants "one more round."

## 1–3. Gameplay: brainstorm, evaluation, decision

Six candidate 30-second loops were considered:

- **A — Kanji Rush** (survival blitz): mixed prompts, tap the right answer fast, 3 lives, combo multiplier. *Puzzle Rush for kanji.*
- **B — Kanjle**: one daily Wordle-style deduction puzzle. Viral but thin/one-a-day, hard to monetize, tricky fair feedback.
- **C — Kanji Match**: speed pairs against a clock. Fine but generic.
- **D — Higher/Lower**: ultra-fast binary taps. Addictive but weak on real learning.
- **E — Compound Builder**: assemble jukugo from tiles. Slower; thin at N5.
- **F — Async Duel**: a competitive *wrapper*, not a loop — can wrap A/C/D later.

| Loop | Fun | Learning | Replayability | Impl. ease | Monetization |
|---|---|---|---|---|---|
| **A — Kanji Rush** | 5 | 4 | 5 | 5 | 4 |
| B — Kanjle | 4 | 3 | 3 | 3 | 2 |
| C — Match | 3 | 3 | 4 | 4 | 3 |
| D — Higher/Lower | 4 | 2 | 4 | 5 | 3 |
| E — Compound Builder | 3 | 4 | 3 | 3 | 3 |

**Decision: build A — Kanji Rush**, with a **shared daily seed** driving a
**weekly leaderboard**. The shared seed is the key insight: everyone's ranked run
draws from the same sequence, so scores are directly comparable and competition
feels real (Chess.com-like) with **no real-time netcode**. Duels (F) are a trivial
fast-follow: same seed, two players, compare scores.

### Core loop spec

- 3 lives. Prompt shows a kanji (or reading, or radical); four answer tiles below.
- Prompt types cycle: kanji→meaning, kanji→reading, reading→kanji, radical→kanji.
- Correct → juice (pop, sound, combo +1, score ticks), next prompt instantly.
- Wrong or 4s timeout → lose a life; correct answer flashes (micro-learning moment).
- Combo multiplier climbs on streaks, resets on miss — the "one more round" driver.
- Run ends on 3rd miss. Result screen: score, **personal-best delta**, and
  **"N pts from rank X"** to pull the retry.
- **Two modes, one engine:** *Practice* (unlimited, unranked, adaptive to weak
  kanji) and *Ranked* (today's shared seed, one attempt, feeds weekly leaderboard).

**Learning guardrail:** multiple-choice risks rewarding recognition over recall,
so reading→kanji prompts (recall-direction) are mixed in and weighted up as a
player levels. Cheap to do; keeps the "you're actually learning" claim credible.

## 4. Player journey

**Principle: play before you sign up.** Forcing auth before the fun is the #1 way
to lose installs.

Cold start: Splash → **Tutorial runs immediately** (~20–30s, ~5 easy kanji, teaches
tap-the-meaning + combo + lives, ends on a real scored run) → *then* "Save your
progress & compete" → Firebase Auth (Google + anonymous guest, upgradeable) →
today's ranked challenge → result screen shows leaderboard placement instantly.

Returning loop: Home hub (streak, "Play today's challenge", rank delta) → Ranked or
Practice run → Result (score, best delta, rank delta, one-more button) → optional share.

Screens (each with one clear purpose): Splash/Auth, Home hub, Gameplay, Result,
Leaderboard, Profile, Settings, Paywall, Tutorial. Nothing else in V0.1.

## 5. Progression

Three deliberately separate tracks:

- **XP → Player Level** (effort/status). Every action earns XP; fast early levels,
  then taper. Streaks and daily completion give bonus XP. Cosmetic/status only.
- **Kanji Mastery** (learning). Per-kanji state New → Learning → Familiar → Mastered,
  driven by an *invisible* lightweight SRS (correct-streak + speed). Not a flashcard
  UI — it just biases which kanji appear in Practice and powers "weakest kanji" stats.
- **Rating** (skill). ELO-lite from ranked runs; seeds fair leaderboard buckets and,
  later, duels. Kept distinct from XP so grinding ≠ skill.

Content: V0.1 ships only N5 (~50 kanji), but the unlock scaffold exists —
mastering enough N5 reveals N4 (aspiration + a monetization lever).

## 6. Subscription (RevenueCat)

One tier to start — **Kanji Rush Pro**, monthly + annual, 7-day trial. Target
~$4.99/mo, ~$29.99/yr (validate vs. comps before committing).

- **Free:** tutorial, unlimited Practice, 1 ranked attempt/day, weekly leaderboard,
  profile, basic stats.
- **Pro:** extra ranked attempts, full analytics (weakest kanji, speed/accuracy
  trends), early N4 access, cosmetic themes + sound packs, 1 streak-freeze/week.

**Critical guardrail — rank on best single run.** Extra attempts buy practice reps,
never a purchased score, so Pro is not pay-to-win on the ladder. Protects the
competitive promise and Play Store approval.

Paywall placement: fire it at the highest-intent moment — a free user finishes a
ranked run, loves it, taps "play again," and is out of attempts. Soft entry from
Profile too. Never wall the tutorial or first run. Single entitlement `pro` gates all.

## 7. Leaderboard & ranking

- **Weekly League** (the competitive core). Shared daily seed → identical questions
  → comparable scores. Best daily ranked score adds to a weekly total; resets Monday
  00:00 UTC. **Bucketed leagues** (Bronze/Silver/Gold…), ~30 similar-rated players
  each; top N promote, bottom N relegate. Buckets keep newcomers in a winnable race —
  a global-only board demoralizes them.
- **Rating (ELO-lite)** server-side; seeds buckets and future duels.

**Anti-cheat (good-enough):** client computes score, but a Cloud Function validates
on submit — cap implausible scores, check run length vs. elapsed time against the
seed, reject impossible runs. Firestore rules: a user writes only their own score
doc, immutable after write. Full server authority is out of scope for V0.1.

## 8. Technical architecture

**Client — Unity 6, 2D URP, uGUI + TextMeshPro (C#).** Entire N5 dataset ships
**bundled as JSON** (`Resources/kanji_n5.json`) → instant, offline core loop.
Clients derive each day's prompt sequence deterministically from the seed.

**Backend — Firebase:**
- **Auth:** anonymous + Google Sign-In, anonymous→Google upgrade preserves progress.
- **Firestore:** `users` (profile, XP, level, rating, mastery map, `pro` flag),
  `dailySeeds` (date → canonical seed), `scores` (userId, date, seed, score,
  validated), `leagues` (weekly buckets + standings).
- **Cloud Functions:** daily seed generator (scheduled); callable score-submit +
  validator (updates standings + rating); weekly reset + promotion/relegation
  (scheduled Mon 00:00 UTC).
- **RevenueCat:** Unity SDK checks entitlement locally for UI; webhook → Cloud
  Function → sets `users.pro` so the backend can gate too.
- **Analytics/Crash:** Firebase Analytics (funnel: install → tutorial complete →
  first ranked → D1 retain → paywall view → trial start) + Crashlytics.

**Data pipeline (build-time):** `tools/build_dataset.py` parses the curated N5
source → validated runtime JSON with hand-tuned confusables for plausible
distractors. Schema aligned to KANJIDIC2 for later enrichment. See `docs/LICENSES.md`.

**KanjiVG stroke order:** polish, not core loop — deferred.

## V0.1 scope (from the brief)

Auth · Tutorial · one core mode (Kanji Rush) · ~50 N5 kanji · Daily Challenge ·
Weekly leaderboard · XP + level · RevenueCat subscription · Profile · basic
animations + sound · Analytics · Settings · Privacy Policy + Terms. **Everything
else deferred.**

## Build roadmap

1. **Data foundation + scaffold** — *done.* Verified 50-kanji dataset, pipeline, C# data layer.
2. **Core gameplay loop** — question generator (distractors from confusables), timer, lives, combo, scoring; playable Practice scene.
3. **Meta layer** — home hub, result screen, XP/level, local profile.
4. **Backend** — Firebase Auth, Firestore, daily seed + score submit/validate Cloud Functions, weekly leagues.
5. **Monetization + compliance** — RevenueCat paywall, analytics funnel, settings, Privacy Policy + Terms.
6. **Polish + ship** — animations, sound, store assets, closed test, Play submission.

## Open questions / to validate

- Confirm Pro price points against current comparable apps before store setup.
- Exact combo/scoring curve — tune in playtest (feel > theory).
- Japanese-glyph TextMeshPro font asset (licensing + file size) — pick before UI work.
- Leaderboard reset timezone: UTC (fairness/simplicity) vs. local (feel).
