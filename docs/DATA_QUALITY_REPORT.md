# Data-quality report

_Generated 2026-08-14 by `tools/build_dataset.py`._

## Dataset counts

- **N5:** 84  (curated 50 retained + 34 new)
- **N4:** 162

## Existing N5 records

- Retained: 50 (all curated values preserved verbatim)
- Enriched: 50 gained the additive `freq` field where the source had it
- Modified (curated values changed): 0 — curated data was never overwritten
- Discrepancies found vs KanjiAPI (reported, not applied): 1

| Kanji | Discrepancy (kept our value) |
|---|---|
| 分 | not present in KanjiAPI N5 source (retained from curation) |

## New N5 records

- Count: 34
- Characters: 万 今 休 何 入 円 出 前 北 午 半 南 友 外 後 書 来 東 母 毎 気 父 白 聞 西 話 語 読 車 長 間 雨 電 高

## N4 records

- Count: 162
- Each tagged `"level": "N4"`; stored in a separate file `kanji_n4.json`.

## Duplicate / overlap detection

- Duplicate characters within N5: none (validation would fail otherwise).
- Duplicate characters within N4: none.
- N5 ∩ N4 overlap in output: none (N4 excludes anything already in N5).
- Source disagreement — KanjiAPI classifies these as N4 but our curation places them in N5, so they were **kept in N5** and removed from N4: 口 手 田 目
- Curated N5 kanji not found in KanjiAPI's N5 list (retained via curation): 田 目 口 手 分

## Missing / unresolved data

- No records are missing required fields (meanings, a reading, primary_reading, romaji, strokes).

### Fields flagged for manual curation (not blocking)

- New kanji with uncurated `tier` (set to sentinel `0`): 196
- Primary reading needs confirmation (kanji with >1 candidate reading, heuristic auto-picked — verify the canonical one): 180
  - N5: 万 今 休 何 入 円 出 前 北 午 半 南 友 外 後 書 来 東 母 毎 気 父 白 聞 西 話 語 読 車 長 間 雨 高
  - N4: 不 世 主 事 京 仕 代 以 会 住 体 作 使 借 元 兄 公 写 冬 切 別 力 勉 動 医 去 古 台 同 味 品 問 図 地 場 売 夏 夕 多 夜 妹 姉 始 字 安 室 家 少 屋 工 帰 広 店 度 建 弟 強 待 心 思 急 悪 持 教 文 新 方 旅 早 明 映 春 昼 有 朝 業 楽 歌 止 正 歩 死 注 海 牛 物 犬 理 用 町 画 病 発 真 着 知 研 社 私 秋 究 空 立 答 紙 終 習 考 者 肉 自 色 花 英 茶 親 言 計 試 買 貸 質 赤 走 起 足 転 近 送 通 運 道 重 野 銀 開 集 青 音 風 飯 飲 館 験 魚 鳥 黒
- New kanji needing confusable/distractor curation: 196 (all new entries; `confusions` left empty, game falls back to random distractors)
- Records with no in-set confusables (warning count): 196

## Source usage

- **KanjiAPI `jlpt-5-enriched` / `jlpt-4-enriched`** — primary source for new-kanji meanings, on/kun readings, stroke counts, frequency (`freq_mainichi_shinbun`), and JLPT level. KanjiAPI is itself derived from KANJIDIC2 + JMdict (EDRDG).
  - Fields obtained: meanings, onyomi, kunyomi, strokes, freq, level.
  - Transformed (not copied): kun readings cleaned to hiragana stems; on readings kept katakana; `primary_reading` chosen by heuristic; `romaji` transliterated (wāpuro Hepburn) from the primary reading.
- No third source was required: KanjiAPI supplied every game-required field for all N5/N4 kanji, so no values were left unresolved or invented.

## Schema changes (additive, backwards-compatible)

| Key | Type | Why |
|---|---|---|
| `level` | string `N5`\|`N4` | Explicit JLPT level per record; lets the game select a level later without changing existing fields. |
| `freq` | int \| null | Newspaper frequency rank from KanjiAPI; supports future difficulty/tier modelling and distractor plausibility. |
| `review` | string[] (optional) | Lists fields on new records that still need human curation (tier, confusions, ambiguous primary_reading). Absent on curated records. |

All pre-existing keys are unchanged. Unity's `KanjiEntryDto` ignores unknown JSON keys (JsonUtility), so **no Unity code change is required**; the new keys can be added to the DTO later if/when the game consumes them.

## Primary reading & romaji convention

- `primary_reading` heuristic for new kanji: (1) a kun reading with no okurigana and not an affix (noun-like, e.g. 水→みず); else (2) the first kun verb/adjective as its full form (見→みる); else (3) the first on'yomi in hiragana (百→ひゃく). Ambiguous cases are flagged above rather than chosen silently.
- `romaji` is transliterated deterministically from `primary_reading` (wāpuro Hepburn: long vowels written out, e.g. こう→kou, おお→oo; っ geminates; ん→n).

## Tier / difficulty

- `tier` is a game-specific curated difficulty (existing N5: 1 = taught first, 2 = later). It is **not** derived from JLPT level. New kanji get sentinel `tier: 0` (uncurated) and are flagged for review; a difficulty model can be defined separately later.
