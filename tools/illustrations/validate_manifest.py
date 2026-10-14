#!/usr/bin/env python3
"""
Validate a Kanji illustration generation manifest.

Enforces the rules in docs/ILLUSTRATIONS.md:
  • every entry has all required fields (of the correct type);
  • kanjiId is the Unicode code point of `character` (stable global ID);
  • kanjiId is unique (no duplicate illustrations for a kanji);
  • the COMMERCIAL-LICENSE GATE: each model and each custom node must record a
    clear commercial license and commercialUse == true;
  • warnings (non-blocking) for: format/size deviating from the manifest standard,
    a missing output file, or a character not present in the shipped datasets.

Fails (exit 1) on any error. Usage:
    python3 tools/illustrations/validate_manifest.py [manifest.json]
Defaults to tools/illustrations/manifest.json, falling back to manifest.example.json.
"""

import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))

REQUIRED_ENTRY = [
    ("kanjiId", int), ("character", str), ("concept", str), ("workflow", str),
    ("model", str), ("modelVersion", str), ("modelLicense", str),
    ("modelCommercialUse", bool), ("customNodes", list), ("prompt", str),
    ("seed", int), ("generationDate", str), ("outputFormat", str),
    ("outputDimensions", dict), ("outputFile", str),
]
UNCLEAR = ("unknown", "unclear", "tbd", "n/a", "none", "")


def license_is_clear(value):
    return isinstance(value, str) and value.strip().lower() not in UNCLEAR \
        and not any(u in value.strip().lower() for u in ("tbd", "unknown", "unclear"))


def codepoint(character):
    if not character:
        return None
    return ord(character[0])


def load_dataset_chars():
    chars = set()
    for name in ("kanji_n5.json", "kanji_n4.json"):
        path = os.path.join(REPO, "Assets", "Resources", name)
        if os.path.exists(path):
            data = json.load(open(path, encoding="utf-8"))
            chars |= {k["char"] for k in data.get("kanji", [])}
    return chars


def main():
    default = os.path.join(HERE, "manifest.json")
    if not os.path.exists(default):
        default = os.path.join(HERE, "manifest.example.json")
    path = sys.argv[1] if len(sys.argv) > 1 else default

    errors, warnings = [], []
    try:
        manifest = json.load(open(path, encoding="utf-8"))
    except Exception as e:
        print(f"FAIL  could not read manifest {path}: {e}")
        sys.exit(1)

    for key in ("schemaVersion", "tool", "toolVersion", "style", "canvas", "entries"):
        if key not in manifest:
            errors.append(f"manifest missing top-level '{key}'")
    std_format = manifest.get("outputFormat")
    std_canvas = manifest.get("canvas")
    entries = manifest.get("entries", [])
    dataset_chars = load_dataset_chars()

    seen_ids = {}
    for i, e in enumerate(entries):
        tag = f"entry[{i}] {e.get('character', '?')}"

        for field, typ in REQUIRED_ENTRY:
            if field not in e:
                errors.append(f"{tag}: missing '{field}'")
            elif not isinstance(e[field], typ):
                errors.append(f"{tag}: '{field}' should be {typ.__name__}, got {type(e[field]).__name__}")

        # identity: kanjiId == code point of character; unique
        ch = e.get("character")
        cp = codepoint(ch)
        if cp is not None and e.get("kanjiId") != cp:
            errors.append(f"{tag}: kanjiId {e.get('kanjiId')} != code point {cp} of '{ch}'")
        if "kanjiId" in e:
            if e["kanjiId"] in seen_ids:
                errors.append(f"{tag}: duplicate kanjiId {e['kanjiId']} (also {seen_ids[e['kanjiId']]})")
            seen_ids[e["kanjiId"]] = tag

        # COMMERCIAL-LICENSE GATE (blocking)
        if e.get("modelCommercialUse") is not True:
            errors.append(f"{tag}: modelCommercialUse must be true (model must permit commercial use)")
        if not license_is_clear(e.get("modelLicense")):
            errors.append(f"{tag}: modelLicense not recorded/clear ('{e.get('modelLicense')}')")
        for j, n in enumerate(e.get("customNodes", []) or []):
            if not isinstance(n, dict):
                errors.append(f"{tag}: customNodes[{j}] must be an object"); continue
            if not n.get("version"):
                errors.append(f"{tag}: customNodes[{j}] '{n.get('name','?')}' missing version")
            if n.get("commercialUse") is not True:
                errors.append(f"{tag}: customNodes[{j}] '{n.get('name','?')}' commercialUse must be true")
            if not license_is_clear(n.get("license")):
                errors.append(f"{tag}: customNodes[{j}] '{n.get('name','?')}' license not recorded/clear")

        # consistency (warnings)
        if std_format and e.get("outputFormat") != std_format:
            warnings.append(f"{tag}: outputFormat '{e.get('outputFormat')}' != manifest '{std_format}'")
        if std_canvas and e.get("outputDimensions") != std_canvas:
            warnings.append(f"{tag}: outputDimensions {e.get('outputDimensions')} != canvas {std_canvas}")

        # dataset cross-check + asset presence (warnings)
        if dataset_chars and ch and ch not in dataset_chars:
            warnings.append(f"{tag}: '{ch}' not found in shipped N5/N4 datasets")
        out = e.get("outputFile")
        if out and not os.path.exists(os.path.join(REPO, out)):
            warnings.append(f"{tag}: outputFile not found on disk ({out})")

    print(f"Illustration manifest: {os.path.relpath(path, REPO)}  ({len(entries)} entr{'y' if len(entries)==1 else 'ies'})")
    print("-" * 64)
    for w in warnings:
        print(f"  WARN  {w}")
    if errors:
        for er in errors:
            print(f"  FAIL  {er}")
        print(f"\n{len(errors)} error(s), {len(warnings)} warning(s). NOT production-ready.")
        sys.exit(1)
    print(f"  OK    all entries valid, licensing recorded ({len(warnings)} warning(s))")


if __name__ == "__main__":
    main()
