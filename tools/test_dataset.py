#!/usr/bin/env python3
"""
Data-level tests for the built kanji datasets (content reliability only —
no gameplay logic). Run:  python3 tools/test_dataset.py

Verifies, for both kanji_n5.json and kanji_n4.json:
  * schema consistency (required keys present, correct types)
  * required values (meanings, a reading, primary_reading, romaji, strokes)
  * no duplicate characters within a level
  * valid JLPT level per record and in meta
  * valid readings (onyomi katakana, kunyomi hiragana, primary hiragana)
  * valid romaji (ascii, matches a re-transliteration of primary_reading)
  * valid stroke counts (int > 0, matches meta count)
  * no N5/N4 overlap
Exits non-zero on any failure.
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
sys.path.insert(0, HERE)
from build_dataset import to_romaji  # reuse the canonical transliterator

REQUIRED = {
    "id": int, "char": str, "meanings": list, "onyomi": list, "kunyomi": list,
    "primary_reading": str, "romaji": str, "strokes": int, "tier": int,
    "confusions": list, "confusions_in_set": list, "level": str,
}
HIRA = range(0x3041, 0x3097)
KATA = range(0x30A1, 0x30FB)

def is_hiragana(s):
    return all(ord(c) in HIRA or c == "ー" for c in s)

def is_katakana(s):
    return all(ord(c) in KATA for c in s)

def load(level):
    with open(os.path.join(REPO, "Assets", "Resources", f"kanji_{level}.json"), encoding="utf-8") as f:
        return json.load(f)

def check_level(data, level, failures):
    def fail(msg): failures.append(f"[{level.upper()}] {msg}")
    recs = data["kanji"]
    if data["meta"]["jlpt_level"] != level.upper():
        fail(f"meta jlpt_level {data['meta']['jlpt_level']} != {level.upper()}")
    if data["meta"]["count"] != len(recs):
        fail(f"meta count {data['meta']['count']} != {len(recs)} records")

    seen = set()
    for r in recs:
        c = r.get("char", "?")
        for key, typ in REQUIRED.items():
            if key not in r:
                fail(f"{c}: missing key {key}")
            elif not isinstance(r[key], typ):
                fail(f"{c}: {key} wrong type {type(r[key]).__name__}")
        if c in seen:
            fail(f"duplicate char {c}")
        seen.add(c)
        if not r.get("meanings"):
            fail(f"{c}: no meanings")
        if not r.get("onyomi") and not r.get("kunyomi"):
            fail(f"{c}: no readings")
        if not r.get("primary_reading"):
            fail(f"{c}: empty primary_reading")
        elif not is_hiragana(r["primary_reading"]):
            fail(f"{c}: primary_reading not hiragana ({r['primary_reading']})")
        if not r.get("romaji"):
            fail(f"{c}: empty romaji")
        elif not r["romaji"].isascii() or not r["romaji"].isalpha():
            fail(f"{c}: romaji not ascii-alpha ({r['romaji']})")
        elif to_romaji(r["primary_reading"]) != r["romaji"]:
            fail(f"{c}: romaji '{r['romaji']}' != transliteration '{to_romaji(r['primary_reading'])}'")
        if r.get("strokes", 0) <= 0:
            fail(f"{c}: bad stroke count {r.get('strokes')}")
        if r.get("level") != level.upper():
            fail(f"{c}: level {r.get('level')} != {level.upper()}")
        for o in r.get("onyomi", []):
            if not is_katakana(o):
                fail(f"{c}: onyomi '{o}' not katakana")
        for k in r.get("kunyomi", []):
            if not is_hiragana(k):
                fail(f"{c}: kunyomi '{k}' not hiragana")
        if c in r.get("confusions", []):
            fail(f"{c}: self-confusable")
    return set(seen)

def main():
    failures = []
    n5 = load("n5"); n4 = load("n4")
    c5 = check_level(n5, "n5", failures)
    c4 = check_level(n4, "n4", failures)
    overlap = c5 & c4
    if overlap:
        failures.append(f"N5/N4 overlap: {' '.join(sorted(overlap))}")

    total = len(n5["kanji"]) + len(n4["kanji"])
    print(f"Data tests — N5={len(n5['kanji'])}, N4={len(n4['kanji'])}, total={total}")
    if failures:
        for f in failures:
            print(f"  FAIL  {f}")
        print(f"\n{len(failures)} failure(s).")
        sys.exit(1)
    print("  OK    all data-level checks passed "
          "(schema, types, uniqueness, levels, readings, romaji, strokes, no overlap)")

if __name__ == "__main__":
    main()
