# Assets/Art/Illustrations

Shipped kanji illustration assets (one image per kanji), produced **offline** in
Comfy Desktop during development. The game does not generate images at runtime.

- **Naming:** `<kanjiId>_<character>.png` (e.g. `27700_水.png`), where `kanjiId` is
  the kanji's Unicode code point — the same stable ID used for mastery.
- **Source of truth:** every file here must have a matching entry in
  `tools/illustrations/manifest.json` (schema + example in `tools/illustrations/`).
- **Before accepting assets:** run `python3 tools/illustrations/validate_manifest.py`
  to enforce required fields, commercial-license recording, code-point identity, and
  consistency. See `docs/ILLUSTRATIONS.md` for the full process and POC plan.

No images are committed yet — start with the ~10-kanji proof of concept.
