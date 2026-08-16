# Learn section

Lets the player browse the game's kanji and open one to study it. Built to be
**reused by the future Revision flow** — there is one kanji presentation component,
not a Learn-specific copy.

## Architecture

```
LearnController (UI)                    ← builds the screens, owns list⇄detail⇄menu nav
├── LearnCatalog (Learn)                ← consumes existing KanjiRush.Data.KanjiDatabase (N5+N4)
│     └── LearnKanji { Kanji, Level, PlayerLevel }
├── KanjiListItem (prefab)              ← one authored row per kanji (char, meaning, level)
└── KanjiDetailView (UI)  ← REUSABLE    ← Show(kanji, illustrationProvider, playerLevel?)
      ├── large character
      ├── illustration slot  ← IKanjiIllustrationProvider (loosely coupled)
      ├── meaning
      ├── on'yomi / kun'yomi
      └── stroke count
```

- **No new data model / no duplicate JSON.** `LearnCatalog` loads the existing
  `KanjiDatabase` for each available dataset and tags each kanji with its
  **player-facing** level (`Novice`/`Apprentice`; JLPT names stay internal). It's the
  place to add filter/search/sort/mastery/lock later without touching the views.
- **Stable identity.** Illustrations are keyed by the kanji's Unicode code point
  (`KanjiId.Of`) — the same id used by mastery.
- **Authored UI (Inspector-wired).** The Learn scene is built in the Editor like the
  other scenes (Canvas Scaler 1080×1920, layout groups, `ScrollRect`, `SafeAreaFitter`);
  the three scripts hold no layout and expose `[SerializeField]` slots you assign. The
  list is populated by instantiating an authored **`KanjiListItem`** prefab into the
  ScrollRect content. See `docs/LEARN_SETUP.md` for the exact hierarchy and wiring.
  (`UiFactory.cs`, the old runtime UI builder, is no longer used and can be deleted.)

## Reusable KanjiDetailView

`KanjiDetailView` is a `MonoBehaviour` with one entry point:

```csharp
detailView.Show(kanji, illustrationProvider, playerLevel);   // playerLevel optional
```

It renders whatever the `Kanji` data provides and shows `—` for any missing field
(never broken UI). It contains **no navigation** and hardcodes **no** kanji. Any host
(Learn now; Revision, post-answer, etc. later) instantiates it under a RectTransform
and calls `Show(...)`. Presentation differences should be handled by parameters, not a
second implementation. The `playerLevel` being optional is the first such knob (Revision
can hide the level chip by passing none).

## Illustration integration point

```
Kanji → KanjiId.Of(char) → IKanjiIllustrationProvider.Get(id) → Sprite (or null → placeholder)
```

- `IKanjiIllustrationProvider` decouples the view from where images come from.
- `PlaceholderIllustrationProvider` returns null (view shows "Illustration coming soon").
- `ResourcesKanjiIllustrationProvider` (the default) loads
  `Resources/Illustrations/<kanjiId>` when present, else null.
- **No image references live in scenes/prefabs.** When the ~2,500-image pipeline ships
  (see `docs/ILLUSTRATIONS.md`), drop sprites named by code point into a
  `Resources/Illustrations` folder, or swap in an Addressables-backed provider — no
  view changes required.

## How Revision reuses this

The future Revision flow (post-answer / review) instantiates a `KanjiDetailView` and
calls `Show(kanji, provider)` — the exact same component. Do **not** build a second
kanji info panel for Revision; add configuration/parameters to `KanjiDetailView` if it
needs a slightly different presentation.

## Navigation

Scene-per-screen (matches the existing architecture): `Boot → MainMenu → { Game, Learn }`.
`MainMenu` gets a **Learn** button (`SceneLoader.GoToLearn`); Play remains the default
flow. The `Learn | Play | Progress` tab bar can be layered on later; Progress is not
implemented here.

## Assumptions / limitations / follow-up

- The list instantiates one `KanjiListItem` prefab per kanji (~246 today) — fine at this
  scale; `LearnController.PopulateList` is the single swap point for a recycled/virtualized
  list later (~2,500) without changing the data or detail view.
- Kanji glyphs rely on the TMP Japanese font/fallback already configured for the game.
- Illustrations show the placeholder until real assets exist under `Resources/Illustrations`.
- Detail currently shows meaning/on/kun/strokes (all existing fields); more existing
  metadata can be added inside `KanjiDetailView` only.
