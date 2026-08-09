#!/usr/bin/env python3
"""
Kanji Rush — N5 dataset build pipeline.

Reads the curated N5 source (embedded below), validates it, and emits the
runtime JSON consumed by the Unity client:
    Assets/KanjiRush/Resources/kanji_n5.json

Design notes
------------
* V0.1 ships a hand-curated N5 set (~50 kanji). The schema is intentionally
  aligned with KANJIDIC2 field semantics (on'yomi / kun'yomi / meanings /
  stroke count) so we can later enrich or expand straight from KANJIDIC2 +
  JLPT tags without reshaping the runtime format.
* `confusions` are hand-tuned visually/semantically confusable kanji. They let
  the runtime distractor picker choose *plausible* wrong answers instead of
  random giveaways. The build splits them into in-set vs external so we can
  measure distractor quality.
* Validation is the point of this script: it fails the build if any kanji can't
  produce a fair 4-option multiple-choice question for every prompt type.

Run:  python3 tools/build_dataset.py
"""

import json
import os
import sys
from datetime import date

# --- Curated N5 source -------------------------------------------------------
# Fields per entry:
#   char, meanings[], on[] (katakana), kun[] (hiragana),
#   primary  = reading tested first (reading->kanji prompt),
#   romaji   = romanization of `primary`,
#   strokes, tier (1=taught first .. 3), confusions[] (confusable kanji)
KANJI = [
    ("一", ["one"], ["イチ", "イツ"], ["ひと"], "いち", "ichi", 1, 1, ["二", "三", "十"]),
    ("二", ["two"], ["ニ"], ["ふた"], "に", "ni", 2, 1, ["一", "三"]),
    ("三", ["three"], ["サン"], ["みっ"], "さん", "san", 3, 1, ["二", "一", "川"]),
    ("四", ["four"], ["シ"], ["よん", "よっ"], "よん", "yon", 5, 1, ["西", "匹", "口"]),
    ("五", ["five"], ["ゴ"], ["いつ"], "ご", "go", 4, 1, ["互", "二", "三"]),
    ("六", ["six"], ["ロク"], ["むっ"], "ろく", "roku", 4, 1, ["大", "穴"]),
    ("七", ["seven"], ["シチ"], ["なな"], "なな", "nana", 2, 1, ["十", "九"]),
    ("八", ["eight"], ["ハチ"], ["やっ"], "はち", "hachi", 2, 1, ["人", "入"]),
    ("九", ["nine"], ["キュウ", "ク"], ["ここの"], "きゅう", "kyuu", 2, 1, ["力", "七"]),
    ("十", ["ten"], ["ジュウ"], ["とお"], "じゅう", "juu", 2, 1, ["千", "土", "七"]),
    ("百", ["hundred"], ["ヒャク"], [], "ひゃく", "hyaku", 6, 2, ["白", "自", "目"]),
    ("千", ["thousand"], ["セン"], ["ち"], "せん", "sen", 3, 2, ["干", "十"]),
    ("大", ["big", "large"], ["ダイ", "タイ"], ["おお"], "おお", "oo", 3, 1, ["太", "犬", "人"]),
    ("小", ["small", "little"], ["ショウ"], ["ちい", "こ"], "ちいさい", "chiisai", 3, 1, ["少", "大"]),
    ("日", ["day", "sun"], ["ニチ", "ジツ"], ["ひ", "か"], "ひ", "hi", 4, 1, ["目", "白", "月"]),
    ("月", ["month", "moon"], ["ゲツ", "ガツ"], ["つき"], "つき", "tsuki", 4, 1, ["日", "肉", "用"]),
    ("火", ["fire"], ["カ"], ["ひ"], "ひ", "hi", 4, 1, ["大", "人", "水"]),
    ("水", ["water"], ["スイ"], ["みず"], "みず", "mizu", 4, 1, ["氷", "永", "火"]),
    ("木", ["tree", "wood"], ["モク", "ボク"], ["き"], "き", "ki", 4, 1, ["本", "休", "林"]),
    ("金", ["gold", "money"], ["キン", "コン"], ["かね"], "かね", "kane", 8, 1, ["全", "食", "土"]),
    ("土", ["earth", "soil"], ["ド", "ト"], ["つち"], "つち", "tsuchi", 3, 1, ["士", "王", "十"]),
    ("山", ["mountain"], ["サン"], ["やま"], "やま", "yama", 3, 1, ["川", "止"]),
    ("川", ["river"], ["セン"], ["かわ"], "かわ", "kawa", 3, 1, ["山", "三"]),
    ("田", ["rice field"], ["デン"], ["た"], "た", "ta", 5, 2, ["由", "申", "町", "中"]),
    ("天", ["heaven", "sky"], ["テン"], ["あま"], "てん", "ten", 4, 2, ["夫", "大", "矢"]),
    ("人", ["person"], ["ジン", "ニン"], ["ひと"], "ひと", "hito", 2, 1, ["入", "八"]),
    ("男", ["man", "male"], ["ダン", "ナン"], ["おとこ"], "おとこ", "otoko", 7, 1, ["田", "力"]),
    ("女", ["woman", "female"], ["ジョ", "ニョ"], ["おんな"], "おんな", "onna", 3, 1, ["安", "好", "大"]),
    ("子", ["child"], ["シ", "ス"], ["こ"], "こ", "ko", 3, 1, ["了", "字", "女"]),
    ("目", ["eye"], ["モク"], ["め"], "め", "me", 5, 1, ["日", "自", "月"]),
    ("口", ["mouth"], ["コウ", "ク"], ["くち"], "くち", "kuchi", 3, 1, ["日", "回", "四"]),
    ("手", ["hand"], ["シュ"], ["て"], "て", "te", 4, 1, ["毛", "才", "千"]),
    ("上", ["up", "above"], ["ジョウ"], ["うえ", "あ"], "うえ", "ue", 3, 1, ["下", "止"]),
    ("下", ["down", "below"], ["カ", "ゲ"], ["した", "さ"], "した", "shita", 3, 1, ["上", "不"]),
    ("中", ["middle", "inside"], ["チュウ"], ["なか"], "なか", "naka", 4, 1, ["申", "虫", "口"]),
    ("左", ["left"], ["サ"], ["ひだり"], "ひだり", "hidari", 5, 1, ["右", "在"]),
    ("右", ["right"], ["ウ", "ユウ"], ["みぎ"], "みぎ", "migi", 5, 1, ["左", "石"]),
    ("本", ["book", "origin"], ["ホン"], ["もと"], "ほん", "hon", 5, 1, ["木", "休", "体"]),
    ("名", ["name"], ["メイ", "ミョウ"], ["な"], "な", "na", 6, 2, ["各", "多", "口"]),
    ("年", ["year"], ["ネン"], ["とし"], "とし", "toshi", 6, 2, ["午", "牛", "生"]),
    ("時", ["time", "hour"], ["ジ"], ["とき"], "とき", "toki", 10, 2, ["持", "寺", "待", "日"]),
    ("分", ["minute", "part"], ["ブン", "フン", "ブ"], ["わ"], "ふん", "fun", 4, 2, ["今", "公", "八"]),
    ("行", ["to go"], ["コウ", "ギョウ"], ["い", "おこな"], "いく", "iku", 6, 2, ["何", "街", "見"]),
    ("見", ["to see"], ["ケン"], ["み"], "みる", "miru", 7, 2, ["貝", "目", "買"]),
    ("食", ["to eat", "food"], ["ショク"], ["た"], "たべる", "taberu", 9, 2, ["飲", "飯", "金"]),
    ("国", ["country"], ["コク"], ["くに"], "くに", "kuni", 8, 2, ["図", "園", "口"]),
    ("学", ["study", "learning"], ["ガク"], ["まな"], "がく", "gaku", 8, 2, ["字", "覚", "子"]),
    ("校", ["school"], ["コウ"], [], "こう", "kou", 10, 2, ["交", "木"]),
    ("先", ["previous", "ahead"], ["セン"], ["さき"], "さき", "saki", 6, 2, ["生", "失"]),
    ("生", ["life", "birth"], ["セイ", "ショウ"], ["い", "う", "なま"], "せい", "sei", 5, 2, ["先", "牛", "主"]),
]

MIN_OPTIONS = 4  # every prompt must offer 4 choices (1 correct + 3 distractors)


def build():
    chars = [k[0] for k in KANJI]
    charset = set(chars)

    records = []
    for idx, (char, meanings, on, kun, primary, romaji, strokes, tier, conf) in enumerate(KANJI):
        conf_in_set = [c for c in conf if c in charset]
        records.append({
            "id": idx,
            "char": char,
            "meanings": meanings,
            "onyomi": on,
            "kunyomi": kun,
            "primary_reading": primary,
            "romaji": romaji,
            "strokes": strokes,
            "tier": tier,
            "confusions": conf,
            "confusions_in_set": conf_in_set,
        })

    dataset = {
        "meta": {
            "schema_version": 1,
            "jlpt_level": "N5",
            "count": len(records),
            "generated": date.today().isoformat(),
            "sources": [
                "Curated for Kanji Rush V0.1. Schema aligned to KANJIDIC2 (EDRDG) "
                "for future enrichment; JLPT N5 classification.",
            ],
            "license_note": (
                "V0.1 kanji facts (readings/meanings) are a curated compilation. "
                "If enriched from KANJIDIC2/KanjiVG (EDRDG, CC BY-SA), attribution "
                "and share-alike obligations apply — see docs/LICENSES.md."
            ),
            "prompt_types": ["meaning", "onyomi", "kunyomi", "reading_to_kanji"],
        },
        "kanji": records,
    }
    return dataset, records, charset


def validate(records, charset):
    """Fail the build unless every kanji yields a fair 4-option question per type."""
    errors, warnings = [], []

    # 1. unique chars
    seen = set()
    for r in records:
        if r["char"] in seen:
            errors.append(f"duplicate char: {r['char']}")
        seen.add(r["char"])

    # 2. required fields + at least one reading
    for r in records:
        if not r["meanings"]:
            errors.append(f"{r['char']}: no meanings")
        if not r["onyomi"] and not r["kunyomi"]:
            errors.append(f"{r['char']}: no readings at all")
        if not r["primary_reading"]:
            errors.append(f"{r['char']}: no primary_reading")
        if r["char"] in r["confusions"]:
            errors.append(f"{r['char']}: lists itself as a confusable")

    # 3. distractor pools — is there a fair 4-option set for each prompt type?
    all_meanings = {m for r in records for m in r["meanings"]}
    all_on = {o for r in records for o in r["onyomi"]}
    all_kun = {k for r in records for k in r["kunyomi"]}

    for r in records:
        # meaning: need >=3 OTHER meanings
        others = all_meanings - set(r["meanings"])
        if len(others) < MIN_OPTIONS - 1:
            errors.append(f"{r['char']}: not enough distractor meanings")
        # onyomi prompt only applies if this kanji has on'yomi
        if r["onyomi"]:
            o_others = all_on - set(r["onyomi"])
            if len(o_others) < MIN_OPTIONS - 1:
                errors.append(f"{r['char']}: not enough distractor on'yomi")
        # kunyomi prompt only applies if this kanji has kun'yomi
        if r["kunyomi"]:
            k_others = all_kun - set(r["kunyomi"])
            if len(k_others) < MIN_OPTIONS - 1:
                errors.append(f"{r['char']}: not enough distractor kun'yomi")
        # reading_to_kanji: need >=3 other kanji glyphs (always true), but flag
        # weak distractor quality if no in-set confusables to draw from.
        if not r["confusions_in_set"]:
            warnings.append(f"{r['char']}: no in-set confusables (distractors will be random)")

    return errors, warnings


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    repo = os.path.dirname(here)
    out_dir = os.path.join(repo, "Assets", "KanjiRush", "Resources")
    out_path = os.path.join(out_dir, "kanji_n5.json")

    dataset, records, charset = build()
    errors, warnings = validate(records, charset)

    print(f"Kanji Rush dataset build — {len(records)} kanji (JLPT N5)")
    print("-" * 60)
    for w in warnings:
        print(f"  WARN  {w}")
    if errors:
        for e in errors:
            print(f"  FAIL  {e}")
        print(f"\nBuild FAILED with {len(errors)} error(s).")
        sys.exit(1)

    os.makedirs(out_dir, exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(dataset, f, ensure_ascii=False, indent=2)

    # Coverage report
    tier_counts = {}
    for r in records:
        tier_counts[r["tier"]] = tier_counts.get(r["tier"], 0) + 1
    in_set_conf = sum(1 for r in records if r["confusions_in_set"])
    print(f"  OK    {len(records)} kanji validated, {len(warnings)} warning(s)")
    print(f"  OK    tiers: {dict(sorted(tier_counts.items()))}")
    print(f"  OK    {in_set_conf}/{len(records)} have in-set confusables")
    print(f"  OK    wrote {os.path.relpath(out_path, repo)} "
          f"({os.path.getsize(out_path)} bytes)")


if __name__ == "__main__":
    main()
