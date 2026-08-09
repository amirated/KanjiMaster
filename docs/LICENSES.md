# Data licensing & attribution

**Read this before importing any external kanji data.** It directly affects Google Play publication.

## Current status (V0.1)

The shipped dataset (`Assets/KanjiRush/Resources/kanji_n5.json`) is a **curated
compilation** authored for this project. Basic kanji facts (a character's
meanings, on'yomi/kun'yomi readings, stroke count) are not themselves
copyrightable, so V0.1 carries no third-party data-license obligation. The JSON
schema is deliberately aligned with KANJIDIC2 field semantics so we can enrich
from it later without reshaping the runtime format.

## If/when we enrich from external datasets

| Dataset | License | Copyright holder |
|---|---|---|
| KANJIDIC2 | Creative Commons **BY-SA 4.0** | James William Breen & the Electronic Dictionary Research and Development Group (EDRDG) |
| KanjiVG (stroke order / SVG) | Creative Commons **BY-SA 3.0** | Ulrich Apel |

Both are **Attribution + ShareAlike** licenses. That means if we incorporate
their data (or derivatives, e.g. importing readings from KANJIDIC2 or stroke
diagrams from KanjiVG), we must:

1. **Attribute** — credit the source and its license in-app (an in-game
   "Credits / Licenses" screen) and in the repo.
2. **ShareAlike** — distribute the derived data under the same (or a compatible)
   CC BY-SA license.
3. **Link the license** and indicate any changes we made.

ShareAlike attaches to the **data**, not to our game code — our Unity/C# code and
app remain ours. But the derived data files must stay CC BY-SA, and the app must
carry the attribution.

## Action items before shipping external data

- [ ] Add an in-app **Credits / Licenses** screen listing any datasets used.
- [ ] Keep derived data files under a `data/` path with a `LICENSE` noting CC BY-SA + source.
- [ ] Re-read the current EDRDG license page before release (terms can be revised).

## Sources

- EDRDG licence: https://www.edrdg.org/edrdg/licence.html
- KANJIDIC project: http://www.edrdg.org/wiki/index.php/KANJIDIC_Project
- KanjiVG: https://kanjivg.tagaini.net/
