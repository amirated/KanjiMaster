# Kanji Rush

A competitive mobile game where you get better at Japanese by competing with others — not a flashcard app with a coat of paint. Think Chess.com's Puzzle Rush meets Wordle's daily streak, built around JLPT kanji.

Built for the **RevenueCat Shipaton 2026**. Android first, iOS later.

## The game (V0.1)

**Kanji Rush** — a ~30-second survival blitz. A prompt appears (a kanji, a reading, or a radical); tap the correct answer as fast as you can. Wrong answer or timeout costs a life; you get three. A combo multiplier rewards streaks. Score = speed × accuracy × combo.

The competitive hook is a **shared daily seed**: everyone's ranked run today draws from the same sequence, so scores are directly comparable and the weekly leaderboard is fair — with zero real-time netcode.

See [`docs/DESIGN.md`](docs/DESIGN.md) for the full design (gameplay evaluation, player journey, progression, subscription, leaderboard, architecture).

## Tech stack

| Layer | Choice |
|---|---|
| Engine | Unity 6 (6000.4.6f1), 2D URP, uGUI + TextMeshPro |
| Backend | Firebase (Firestore, Auth, Cloud Functions) |
| Subscriptions | RevenueCat |
| Analytics / Crash | Firebase Analytics + Crashlytics |
| Source control | GitHub |

## Project layout

```
Assets/KanjiRush/
  Resources/kanji_n5.json     # bundled N5 dataset (generated — do not hand-edit)
  Scripts/
    Data/                     # Kanji model, DTOs, KanjiDatabase loader   [done]
    Core/                     # gameplay loop: timer, lives, combo, scoring [next]
    UI/                       # screens (home, gameplay, result, ...)       [later]
    Services/                 # Firebase + RevenueCat wrappers              [later]
  Tests/                      # EditMode tests                              [later]
tools/
  build_dataset.py            # curated N5 source -> validated runtime JSON
docs/
  DESIGN.md                   # full game + system design (tasks 1-8)
  LICENSES.md                 # data attribution / license obligations
```

## Working with the dataset

The kanji dataset is **generated**, not hand-edited. Edit the curated source in
`tools/build_dataset.py`, then regenerate:

```bash
python3 tools/build_dataset.py
```

The script validates the data and **fails the build** if any kanji can't produce
a fair 4-option question for every prompt type (meaning, on'yomi, kun'yomi,
reading→kanji), if there are duplicate characters, or if a kanji lists itself as
a confusable. Output: `Assets/KanjiRush/Resources/kanji_n5.json`.

## Current status

V0.1 scaffold. Done so far: verified 50-kanji N5 dataset + build pipeline, and
the C# data layer (`Kanji`, `KanjiDatabase`). Next brick: the Kanji Rush core
gameplay loop. See `docs/DESIGN.md` for the roadmap.

## Data & licensing

V0.1 ships a **curated** kanji dataset. If it is later enriched from KANJIDIC2 or
KanjiVG, those are Creative Commons **BY-SA** and carry attribution + share-alike
obligations that affect Play Store publication. Read [`docs/LICENSES.md`](docs/LICENSES.md)
before adding external data.
