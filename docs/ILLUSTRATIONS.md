# Kanji Illustrations — generation pipeline

Custom illustrations help players visually associate a kanji with its meaning.
They appear in the future **Learn** section and during **Revision**.

**Illustrations are generated offline during development and shipped as static
assets. The game never generates images at runtime.** This document defines the
tooling, licensing rules, the generation manifest, asset requirements, and the
proof-of-concept plan. It is documentation only — no images are produced here.

## Where things live

```
Assets/Art/Illustrations/          shipped image assets (PNG), one per kanji
tools/illustrations/
  manifest.schema.json             JSON Schema for a manifest + entries
  manifest.example.json            worked example (POC-style entries)
  manifest.json                    the real production manifest (created during generation)
  validate_manifest.py             validator (run before accepting assets)
```

The manifest and validator are **development artifacts** under `tools/` (not shipped
in the build). The image files under `Assets/Art/Illustrations/` are shipped.

## Generation tool

- **Tool:** Comfy Desktop (offline AI image generation).
- **Commercial use:** permitted per the applicable Comfy Desktop EULA.
- **Version:** _TBD_ — record the exact version in `manifest.tool` / `manifest.toolVersion`.

## Model licensing rules (hard requirements)

Every AI model used for **production** assets must:

- explicitly permit **commercial use**;
- have a **documented license** (name/ID);
- have a **recorded version**;
- be recorded in the generation manifest.

**No model with unclear commercial licensing may be used for production assets.**
The validator fails if a model's license or commercial-use flag is missing/unclear.

## Custom nodes

Every third-party ComfyUI custom node used in production must have a documented
**license**, confirmed **commercial-use compatibility**, and a recorded **version**
(all captured per-entry in `customNodes[]`). **Prefer workflows that minimize
third-party custom nodes** — fewer nodes means less licensing surface and better
reproducibility.

## Generation manifest

Every production illustration has one manifest entry. Required fields (see
`manifest.schema.json` for the exact shape):

| Field | Meaning |
|---|---|
| `kanjiId` | **Unicode code point** — the stable global ID (matches the mastery key, e.g. 水 = 27700). |
| `character` | The kanji glyph (human-readable; must match `kanjiId`). |
| `concept` | English concept/meaning the image depicts. |
| `workflow` | Generation workflow name/file used in Comfy. |
| `model` / `modelVersion` / `modelLicense` / `modelCommercialUse` | Model identity + license + explicit commercial-use flag. |
| `customNodes[]` | Each `{ name, version, license, commercialUse }`. |
| `prompt` | The generation prompt. |
| `seed` | Seed used (for reproducibility). |
| `generationDate` | ISO date (YYYY-MM-DD). |
| `outputFormat` | e.g. `png`. |
| `outputDimensions` | `{ width, height }` in pixels. |
| `outputFile` | Path (relative to repo root) of the produced asset. |

`kanjiId` is the Unicode code point (not the dataset's per-file `id`, which collides
across levels, and not the glyph string). This keeps one identity across mastery,
the manifest, and asset lookup.

## Asset requirements

Production illustrations should:

- use a **consistent visual style** (record the style descriptor in `manifest.style`);
- use a **consistent canvas size** (record in `manifest.canvas`);
- read well on **mobile screens** and remain clear at **small sizes**;
- avoid unnecessary fine detail;
- avoid copyrighted characters or recognizable third-party IP;
- be suitable for **commercial distribution**.

The validator warns when an entry's format/dimensions deviate from the manifest's
declared standard.

## Proof of concept (before the full library)

Generate ~**10 kanji** first and evaluate:

1. **Visual consistency** — do the 10 look like one set?
2. **Semantic clarity** — is the meaning readable from the image alone?
3. **Style** — is the chosen style on-brand and legible small?
4. **Reproducibility** — same prompt + seed + workflow → same/near-same image?
5. **Generation speed** — time per image (feasibility for ~2,500).
6. **Output file size** — per-image and projected library size.
7. **Model licensing** — every model/node commercial-clear and recorded.
8. **Workflow complexity** — how many custom nodes; can it be simplified?
9. **Unity suitability** — import settings, sprite/texture, memory at small sizes.

**POC exit criteria:** all 10 pass the validator (fields + licensing), the style is
approved, and reproducibility + Unity import are confirmed. Only then scale up.

## Deferred (not part of this task)

Actual image generation (done in Comfy on the developer's machine), the full
~2,500-image library, any runtime loading/Unity import automation, and the Learn
section itself. This task establishes the docs, manifest schema, example, validator,
and asset folder only.
