# Scripts/Kanji

Reserved for kanji-related gameplay helpers (question generation, selection,
mastery) added during the gameplay phase.

The core Kanji **data layer** (`Kanji`, `KanjiDatabase`, DTOs) currently lives in
`Assets/Scripts/Data/` under the `KanjiRush.Data` namespace (assembly
`KanjiRush.Runtime`). It was intentionally left in place — moving it would churn
the assembly definition and asset GUIDs for no benefit. It loads the bundled
dataset from `Assets/Resources/kanji_n5.json`.
