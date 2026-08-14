#!/usr/bin/env python3
"""
Kanji content build + enrichment pipeline (N5 + N4).

Pipeline:
    KanjiAPI (jlpt-*-enriched)  ->  map into OUR schema  ->  merge curated
    precedence  ->  validate  ->  emit game datasets + data-quality report

Our schema (canonical, source of truth for Unity) is UNCHANGED for existing
fields. See docs/DATA_QUALITY_REPORT.md for the additive keys (level, freq,
review) introduced by this build and why.

Outputs:
    Assets/Resources/kanji_n5.json   (complete N5: curated 50 + new)
    Assets/Resources/kanji_n4.json   (N4, excludes anything already in N5)
    docs/DATA_QUALITY_REPORT.md

Sources (fetched via web_fetch, saved raw):
    tools/sources/jlpt5_enriched.json
    tools/sources/jlpt4_enriched.json

Run:  python3 tools/build_dataset.py
"""

import json
import os
import sys
from datetime import date

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
SRC_DIR = os.path.join(HERE, "sources")
OUT_DIR = os.path.join(REPO, "Assets", "Resources")
REPORT_PATH = os.path.join(REPO, "docs", "DATA_QUALITY_REPORT.md")
SCHEMA_VERSION = 2
MIN_OPTIONS = 4

# =============================================================================
# Curated N5 (source of truth for existing entries — takes precedence).
# Fields: char, meanings[], on[] (katakana), kun[] (hiragana stems),
#         primary_reading, romaji, strokes, tier (1..), confusions[]
# =============================================================================
CURATED_N5 = [
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

# =============================================================================
# Kana / romaji helpers  (deterministic — transliteration, not invention)
# =============================================================================
_KATA_TO_HIRA = {chr(c): chr(c - 0x60) for c in range(0x30A1, 0x30F7)}

def kata_to_hira(s):
    return "".join(_KATA_TO_HIRA.get(c, c) for c in s)

_DIGRAPHS = {
    "きゃ": "kya", "きゅ": "kyu", "きょ": "kyo", "しゃ": "sha", "しゅ": "shu", "しょ": "sho",
    "ちゃ": "cha", "ちゅ": "chu", "ちょ": "cho", "にゃ": "nya", "にゅ": "nyu", "にょ": "nyo",
    "ひゃ": "hya", "ひゅ": "hyu", "ひょ": "hyo", "みゃ": "mya", "みゅ": "myu", "みょ": "myo",
    "りゃ": "rya", "りゅ": "ryu", "りょ": "ryo", "ぎゃ": "gya", "ぎゅ": "gyu", "ぎょ": "gyo",
    "じゃ": "ja", "じゅ": "ju", "じょ": "jo", "びゃ": "bya", "びゅ": "byu", "びょ": "byo",
    "ぴゃ": "pya", "ぴゅ": "pyu", "ぴょ": "pyo",
}
_MONO = {
    "あ": "a", "い": "i", "う": "u", "え": "e", "お": "o",
    "か": "ka", "き": "ki", "く": "ku", "け": "ke", "こ": "ko",
    "が": "ga", "ぎ": "gi", "ぐ": "gu", "げ": "ge", "ご": "go",
    "さ": "sa", "し": "shi", "す": "su", "せ": "se", "そ": "so",
    "ざ": "za", "じ": "ji", "ず": "zu", "ぜ": "ze", "ぞ": "zo",
    "た": "ta", "ち": "chi", "つ": "tsu", "て": "te", "と": "to",
    "だ": "da", "ぢ": "ji", "づ": "zu", "で": "de", "ど": "do",
    "な": "na", "に": "ni", "ぬ": "nu", "ね": "ne", "の": "no",
    "は": "ha", "ひ": "hi", "ふ": "fu", "へ": "he", "ほ": "ho",
    "ば": "ba", "び": "bi", "ぶ": "bu", "べ": "be", "ぼ": "bo",
    "ぱ": "pa", "ぴ": "pi", "ぷ": "pu", "ぺ": "pe", "ぽ": "po",
    "ま": "ma", "み": "mi", "む": "mu", "め": "me", "も": "mo",
    "や": "ya", "ゆ": "yu", "よ": "yo",
    "ら": "ra", "り": "ri", "る": "ru", "れ": "re", "ろ": "ro",
    "わ": "wa", "ゐ": "wi", "ゑ": "we", "を": "wo", "ん": "n",
    "ぁ": "a", "ぃ": "i", "ぅ": "u", "ぇ": "e", "ぉ": "o", "ー": "",
}

def to_romaji(kana):
    """Wapuro-style Hepburn (long vowels written out, e.g. こう->kou, おお->oo)."""
    kana = kata_to_hira(kana)
    tokens = []
    i = 0
    while i < len(kana):
        two = kana[i:i + 2]
        if two in _DIGRAPHS:
            tokens.append(_DIGRAPHS[two]); i += 2
        elif kana[i] == "っ":
            tokens.append("SOKUON"); i += 1
        else:
            tokens.append(_MONO.get(kana[i], kana[i])); i += 1
    out = []
    for idx, t in enumerate(tokens):
        if t == "SOKUON":
            nxt = tokens[idx + 1] if idx + 1 < len(tokens) else ""
            if nxt.startswith("ch"):
                out.append("t")
            elif nxt and nxt[0] not in "aiueo":
                out.append(nxt[0])
        else:
            out.append(t)
    return "".join(out)

# =============================================================================
# KanjiAPI -> our schema mapping
# =============================================================================
_MEANING_SKIP = ("radical", "counter for", "counter", "sign of", "**", "no.",
                 "zodiac", "10,000", "10**")

def clean_meanings(raw):
    kept = [m for m in raw if not any(tok in m.lower() for tok in _MEANING_SKIP)]
    if not kept:  # never drop everything
        kept = raw[:1]
    return kept

def clean_onyomi(raw):
    """On'yomi kept as katakana; strip affix markers ('-') and dedupe."""
    out = []
    for o in raw:
        o2 = o.replace("-", "")
        if o2 and o2 not in out:
            out.append(o2)
    return out

def clean_kunyomi(raw):
    """Cleaned kun stems (hiragana): drop pure-affix markers, strip '-' and okurigana."""
    stems = []
    for k in raw:
        stem = k.replace("-", "")
        if "." in stem:
            stem = stem.split(".")[0]
        if stem and stem not in stems:
            stems.append(stem)
    return stems

def primary_reading_and_flags(entry):
    """Return (primary_reading_hiragana, ambiguous: bool). Documented heuristic:
       1) nouns first: a kun reading with no okurigana ('.') and not an affix -> use whole
       2) else a kun verb/adjective: first kun -> join stem+okurigana (み.る -> みる)
       3) else (no kun): first on'yomi, katakana->hiragana (百 -> ひゃく)
    """
    kun = entry.get("kun_readings", [])
    on = clean_onyomi(entry.get("on_readings", []))
    usable_kun = [k for k in kun if not k.startswith("-")]
    # More than one candidate reading (kun and/or on) => the canonical choice is a
    # judgement call, so flag it for review rather than trusting the heuristic pick.
    ambiguous = (len(usable_kun) + len(on)) > 1
    nouns = [k for k in usable_kun if "." not in k and not k.endswith("-")]
    if nouns:
        return nouns[0].rstrip("-"), ambiguous
    if usable_kun:
        return usable_kun[0].replace("-", "").replace(".", ""), ambiguous
    if on:
        return kata_to_hira(on[0]), ambiguous
    return "", True

def convert_new(entry, level):
    """Convert a KanjiAPI entry to a new record in our schema."""
    primary, ambiguous = primary_reading_and_flags(entry)
    review = ["tier", "confusions"]      # never curated for new kanji
    if ambiguous:
        review.append("primary_reading")
    if not primary:
        review.append("primary_reading_missing")
    rec = {
        "char": entry["kanji"],
        "meanings": clean_meanings(entry.get("meanings", [])),
        "onyomi": clean_onyomi(entry.get("on_readings", [])),
        "kunyomi": clean_kunyomi(entry.get("kun_readings", [])),
        "primary_reading": primary,
        "romaji": to_romaji(primary) if primary else "",
        "strokes": entry.get("stroke_count", 0),
        "tier": 0,                        # 0 = difficulty not yet curated
        "confusions": [],                 # needs curation
        "confusions_in_set": [],
        "level": level,
        "freq": entry.get("freq_mainichi_shinbun"),
        "review": review,
    }
    return rec

def curated_record(tuple_row, level, api_by_char):
    char, meanings, on, kun, primary, romaji, strokes, tier, conf = tuple_row
    api = api_by_char.get(char)
    rec = {
        "char": char,
        "meanings": list(meanings),
        "onyomi": list(on),
        "kunyomi": list(kun),
        "primary_reading": primary,
        "romaji": romaji,
        "strokes": strokes,
        "tier": tier,
        "confusions": list(conf),
        "confusions_in_set": [],          # filled once the level charset is known
        "level": level,
        "freq": api.get("freq_mainichi_shinbun") if api else None,  # additive only
    }
    return rec

# =============================================================================
# Discrepancy comparison (curated vs source — report only, never overwrite)
# =============================================================================
def compare_curated(tuple_row, api):
    char, meanings, on, kun, primary, romaji, strokes, tier, conf = tuple_row
    issues = []
    if api is None:
        return ["not present in KanjiAPI N5 source (retained from curation)"]
    if strokes != api.get("stroke_count"):
        issues.append(f"strokes: ours={strokes} vs source={api.get('stroke_count')}")
    # romaji consistency vs our own transliteration of our primary_reading
    expected = to_romaji(primary)
    if expected != romaji:
        issues.append(f"romaji: stored='{romaji}' vs transliterated='{expected}'")
    # is our primary_reading attested by the source?
    src_readings = set(clean_kunyomi(api.get("kun_readings", [])))
    src_readings |= {k.replace("-", "").replace(".", "") for k in api.get("kun_readings", [])}
    src_readings |= {kata_to_hira(o) for o in api.get("on_readings", [])}
    if primary not in src_readings:
        issues.append(f"primary_reading '{primary}' not directly attested in source readings")
    return issues

# =============================================================================
# Build
# =============================================================================
def load_source(name):
    with open(os.path.join(SRC_DIR, name), encoding="utf-8") as f:
        return json.load(f)

def fill_confusions_in_set(records):
    charset = {r["char"] for r in records}
    for r in records:
        r["confusions_in_set"] = [c for c in r["confusions"] if c in charset and c != r["char"]]

def assign_ids(records):
    for i, r in enumerate(records):
        r["id"] = i
    # move id to front for readability
    return [{"id": r.pop("id"), **r} for r in records]

def make_dataset(records, level):
    return {
        "meta": {
            "schema_version": SCHEMA_VERSION,
            "jlpt_level": level,
            "count": len(records),
            "generated": date.today().isoformat(),
            "sources": [
                "Existing curated entries (game source of truth).",
                "New/enriched entries mapped from KanjiAPI jlpt-*-enriched "
                "(derived from KANJIDIC2/JMdict, EDRDG, CC BY-SA).",
            ],
            "license_note": (
                "Kanji facts for new entries are mapped from KanjiAPI "
                "(KANJIDIC2/JMdict, EDRDG CC BY-SA). Attribution + share-alike apply "
                "to derived data — see docs/LICENSES.md."
            ),
            "schema_additions": {
                "level": "JLPT level of this record (N5|N4).",
                "freq": "Newspaper frequency rank (lower=more common) or null; from KanjiAPI.",
                "review": "Optional list of fields needing manual curation (new kanji).",
            },
            "prompt_types": ["meaning", "onyomi", "kunyomi", "reading_to_kanji"],
        },
        "kanji": records,
    }

def validate(records, level, seen_global):
    errors, warnings = [], []
    seen = set()
    for r in records:
        c = r["char"]
        if c in seen:
            errors.append(f"[{level}] duplicate char: {c}")
        seen.add(c)
        if c in seen_global:
            errors.append(f"[{level}] {c} also present in {seen_global[c]} (N5/N4 overlap)")
        # required keys
        for key, typ in (("char", str), ("meanings", list), ("onyomi", list),
                         ("kunyomi", list), ("primary_reading", str), ("romaji", str),
                         ("strokes", int), ("tier", int), ("level", str),
                         ("confusions", list), ("confusions_in_set", list)):
            if key not in r:
                errors.append(f"[{level}] {c}: missing key '{key}'")
            elif not isinstance(r[key], typ):
                errors.append(f"[{level}] {c}: '{key}' wrong type ({type(r[key]).__name__})")
        # required values
        if not r.get("meanings"):
            errors.append(f"[{level}] {c}: no meanings")
        if not r.get("onyomi") and not r.get("kunyomi"):
            errors.append(f"[{level}] {c}: no readings at all")
        if not r.get("primary_reading"):
            errors.append(f"[{level}] {c}: missing primary_reading")
        if not r.get("romaji"):
            errors.append(f"[{level}] {c}: missing romaji")
        if r.get("strokes", 0) <= 0:
            errors.append(f"[{level}] {c}: invalid stroke count {r.get('strokes')}")
        if r.get("level") not in ("N5", "N4"):
            errors.append(f"[{level}] {c}: invalid level {r.get('level')}")
        if c in r.get("confusions", []):
            errors.append(f"[{level}] {c}: lists itself as a confusable")
        if r.get("freq") is not None and not isinstance(r["freq"], int):
            errors.append(f"[{level}] {c}: freq must be int or null")
        if not r["confusions_in_set"]:
            warnings.append(f"[{level}] {c}: no in-set confusables (distractors fall back to random)")
    # distractor pools (fatal — needed for meaning/reading prompts)
    all_mean = {m for r in records for m in r["meanings"]}
    all_primary = {r["primary_reading"] for r in records}
    for r in records:
        if len(all_mean - set(r["meanings"])) < MIN_OPTIONS - 1:
            errors.append(f"[{level}] {r['char']}: not enough distractor meanings")
        if len(all_primary - {r["primary_reading"]}) < MIN_OPTIONS - 1:
            errors.append(f"[{level}] {r['char']}: not enough distractor readings")
    return errors, warnings


def main():
    api_n5 = load_source("jlpt5_enriched.json")
    api_n4 = load_source("jlpt4_enriched.json")
    api_n5_by = {e["kanji"]: e for e in api_n5}
    api_n4_by = {e["kanji"]: e for e in api_n4}
    api_by_all = {**api_n4_by, **api_n5_by}

    curated_chars = [t[0] for t in CURATED_N5]
    curated_set = set(curated_chars)

    # --- N5: curated (precedence) + KanjiAPI N5 not already curated ----------
    n5_records = []
    discrepancies = {}
    for row in CURATED_N5:
        char = row[0]
        n5_records.append(curated_record(row, "N5", api_by_all))
        issues = compare_curated(row, api_n5_by.get(char) or api_by_all.get(char))
        if issues:
            discrepancies[char] = issues

    new_n5_chars = [e["kanji"] for e in api_n5 if e["kanji"] not in curated_set]
    for e in api_n5:
        if e["kanji"] not in curated_set:
            n5_records.append(convert_new(e, "N5"))

    n5_charset = {r["char"] for r in n5_records}

    # --- N4: KanjiAPI N4, excluding anything already in N5 -------------------
    source_n4_in_n5 = [e["kanji"] for e in api_n4 if e["kanji"] in n5_charset]
    n4_records = []
    for e in api_n4:
        if e["kanji"] not in n5_charset:
            n4_records.append(convert_new(e, "N4"))

    # confusions_in_set per level
    fill_confusions_in_set(n5_records)
    fill_confusions_in_set(n4_records)

    n5_records = assign_ids(n5_records)
    n4_records = assign_ids(n4_records)

    # --- validation ----------------------------------------------------------
    seen_global = {}
    errs5, warn5 = validate(n5_records, "N5", seen_global)
    for r in n5_records:
        seen_global[r["char"]] = "N5"
    errs4, warn4 = validate(n4_records, "N4", seen_global)
    errors = errs5 + errs4
    warnings = warn5 + warn4

    # --- missing-data + review collection ------------------------------------
    def missing_fields(records):
        out = {}
        for r in records:
            miss = []
            if not r["meanings"]:
                miss.append("meanings")
            if not r["primary_reading"]:
                miss.append("primary_reading")
            if not r["romaji"]:
                miss.append("romaji")
            if not r.get("onyomi") and not r.get("kunyomi"):
                miss.append("readings")
            if r["strokes"] <= 0:
                miss.append("strokes")
            if miss:
                out[r["char"]] = miss
        return out

    missing5 = missing_fields(n5_records)
    missing4 = missing_fields(n4_records)

    print(f"Kanji content build (schema v{SCHEMA_VERSION})")
    print("-" * 64)
    print(f"  N5: {len(n5_records)}  (curated {len(CURATED_N5)} + new {len(new_n5_chars)})")
    print(f"  N4: {len(n4_records)}")
    print(f"  source N4 kept in N5 (overlap, curated precedence): {source_n4_in_n5}")
    print(f"  curated N5 not in KanjiAPI N5 source: "
          f"{[c for c in curated_chars if c not in api_n5_by]}")
    for w in warnings[:0]:
        pass
    print(f"  warnings (no in-set confusables etc.): {len(warnings)}")

    if errors:
        for e in errors:
            print(f"  FAIL  {e}")
        print(f"\nBuild FAILED with {len(errors)} error(s).")
        sys.exit(1)

    os.makedirs(OUT_DIR, exist_ok=True)
    for level, records in (("N5", n5_records), ("N4", n4_records)):
        path = os.path.join(OUT_DIR, f"kanji_{level.lower()}.json")
        with open(path, "w", encoding="utf-8") as f:
            json.dump(make_dataset(records, level), f, ensure_ascii=False, indent=2)
        print(f"  OK    wrote {os.path.relpath(path, REPO)} ({os.path.getsize(path)} bytes)")

    write_report(n5_records, n4_records, new_n5_chars, source_n4_in_n5,
                 curated_chars, api_n5_by, discrepancies, missing5, missing4, warnings)
    print(f"  OK    wrote {os.path.relpath(REPORT_PATH, REPO)}")


def write_report(n5, n4, new_n5, overlap_n4_in_n5, curated_chars, api_n5_by,
                 discrepancies, missing5, missing4, warnings):
    def review_list(records):
        return {r["char"]: r["review"] for r in records if r.get("review")}
    rev5, rev4 = review_list(n5), review_list(n4)
    curated_not_in_source = [c for c in curated_chars if c not in api_n5_by]
    lines = []
    A = lines.append
    A("# Data-quality report\n")
    A(f"_Generated {date.today().isoformat()} by `tools/build_dataset.py`._\n")

    A("## Dataset counts\n")
    A(f"- **N5:** {len(n5)}  (curated {len(curated_chars)} retained + {len(new_n5)} new)")
    A(f"- **N4:** {len(n4)}\n")

    A("## Existing N5 records\n")
    A(f"- Retained: {len(curated_chars)} (all curated values preserved verbatim)")
    A(f"- Enriched: {len(curated_chars)} gained the additive `freq` field where the source had it")
    A(f"- Modified (curated values changed): 0 — curated data was never overwritten")
    A(f"- Discrepancies found vs KanjiAPI (reported, not applied): {len(discrepancies)}\n")
    if discrepancies:
        A("| Kanji | Discrepancy (kept our value) |")
        A("|---|---|")
        for c, iss in discrepancies.items():
            A(f"| {c} | {'; '.join(iss)} |")
        A("")

    A("## New N5 records\n")
    A(f"- Count: {len(new_n5)}")
    A(f"- Characters: {' '.join(new_n5)}\n")

    A("## N4 records\n")
    A(f"- Count: {len(n4)}")
    A(f"- Each tagged `\"level\": \"N4\"`; stored in a separate file `kanji_n4.json`.\n")

    A("## Duplicate / overlap detection\n")
    A("- Duplicate characters within N5: none (validation would fail otherwise).")
    A("- Duplicate characters within N4: none.")
    A("- N5 ∩ N4 overlap in output: none (N4 excludes anything already in N5).")
    A(f"- Source disagreement — KanjiAPI classifies these as N4 but our curation "
      f"places them in N5, so they were **kept in N5** and removed from N4: "
      f"{' '.join(overlap_n4_in_n5) if overlap_n4_in_n5 else 'none'}")
    A(f"- Curated N5 kanji not found in KanjiAPI's N5 list (retained via curation): "
      f"{' '.join(curated_not_in_source) if curated_not_in_source else 'none'}\n")

    A("## Missing / unresolved data\n")
    if not missing5 and not missing4:
        A("- No records are missing required fields (meanings, a reading, primary_reading, romaji, strokes).")
    else:
        for lvl, miss in (("N5", missing5), ("N4", missing4)):
            for c, fields in miss.items():
                A(f"- [{lvl}] {c}: missing {', '.join(fields)}")
    A("")
    A("### Fields flagged for manual curation (not blocking)\n")
    A(f"- New kanji with uncurated `tier` (set to sentinel `0`): {len(new_n5) + len(n4)}")
    amb5 = [c for c, v in rev5.items() if "primary_reading" in v]
    amb4 = [c for c, v in rev4.items() if "primary_reading" in v]
    A(f"- Primary reading needs confirmation (kanji with >1 candidate reading, "
      f"heuristic auto-picked — verify the canonical one): {len(amb5) + len(amb4)}")
    A(f"  - N5: {' '.join(amb5) if amb5 else 'none'}")
    A(f"  - N4: {' '.join(amb4) if amb4 else 'none'}")
    A(f"- New kanji needing confusable/distractor curation: {len(new_n5) + len(n4)} "
      f"(all new entries; `confusions` left empty, game falls back to random distractors)")
    no_conf = len(warnings)
    A(f"- Records with no in-set confusables (warning count): {no_conf}\n")

    A("## Source usage\n")
    A("- **KanjiAPI `jlpt-5-enriched` / `jlpt-4-enriched`** (https://kanjiapi.dev) — "
      "primary source for new-kanji meanings, on/kun readings, stroke counts, "
      "frequency (`freq_mainichi_shinbun`), and JLPT level. KanjiAPI is itself "
      "derived from KANJIDIC2 + JMdict (EDRDG).")
    A("  - Fields obtained: meanings, onyomi, kunyomi, strokes, freq, level.")
    A("  - Transformed (not copied): kun readings cleaned to hiragana stems; "
      "on readings kept katakana; `primary_reading` chosen by heuristic; "
      "`romaji` transliterated (wāpuro Hepburn) from the primary reading.")
    A("- No third source was required: KanjiAPI supplied every game-required field "
      "for all N5/N4 kanji, so no values were left unresolved or invented.\n")

    A("## Schema changes (additive, backwards-compatible)\n")
    A("| Key | Type | Why |")
    A("|---|---|---|")
    A("| `level` | string `N5`\\|`N4` | Explicit JLPT level per record; lets the game select a level later without changing existing fields. |")
    A("| `freq` | int \\| null | Newspaper frequency rank from KanjiAPI; supports future difficulty/tier modelling and distractor plausibility. |")
    A("| `review` | string[] (optional) | Lists fields on new records that still need human curation (tier, confusions, ambiguous primary_reading). Absent on curated records. |")
    A("\nAll pre-existing keys are unchanged. Unity's `KanjiEntryDto` ignores unknown "
      "JSON keys (JsonUtility), so **no Unity code change is required**; the new keys "
      "can be added to the DTO later if/when the game consumes them.\n")

    A("## Primary reading & romaji convention\n")
    A("- `primary_reading` heuristic for new kanji: (1) a kun reading with no okurigana "
      "and not an affix (noun-like, e.g. 水→みず); else (2) the first kun verb/adjective "
      "as its full form (見→みる); else (3) the first on'yomi in hiragana (百→ひゃく). "
      "Ambiguous cases are flagged above rather than chosen silently.")
    A("- `romaji` is transliterated deterministically from `primary_reading` (wāpuro "
      "Hepburn: long vowels written out, e.g. こう→kou, おお→oo; っ geminates; ん→n).\n")

    A("## Tier / difficulty\n")
    A("- `tier` is a game-specific curated difficulty (existing N5: 1 = taught first, 2 = later). "
      "It is **not** derived from JLPT level. New kanji get sentinel `tier: 0` (uncurated) and "
      "are flagged for review; a difficulty model can be defined separately later.\n")

    with open(REPORT_PATH, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


if __name__ == "__main__":
    main()
