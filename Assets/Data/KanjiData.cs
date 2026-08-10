using System;
using System.Collections.Generic;

namespace KanjiRush.Data
{
    // Serializable DTOs that mirror Assets/KanjiRush/Resources/kanji_n5.json 1:1.
    // Field names intentionally match the JSON keys so Unity's built-in
    // JsonUtility can deserialize with zero external dependencies.
    // These are internal transport types; gameplay code should use the clean
    // `Kanji` / `KanjiDatabase` domain types instead.

    [Serializable]
    public class KanjiDatasetDto
    {
        public KanjiMetaDto meta;
        public List<KanjiEntryDto> kanji = new List<KanjiEntryDto>();
    }

    [Serializable]
    public class KanjiMetaDto
    {
        public int schema_version;
        public string jlpt_level;
        public int count;
        public string generated;
        public List<string> sources = new List<string>();
        public string license_note;
        public List<string> prompt_types = new List<string>();
    }

    [Serializable]
    public class KanjiEntryDto
    {
        public int id;
        public string @char;            // JSON key "char" (C# keyword, hence @)
        public List<string> meanings = new List<string>();
        public List<string> onyomi = new List<string>();
        public List<string> kunyomi = new List<string>();
        public string primary_reading;
        public string romaji;
        public int strokes;
        public int tier;
        public List<string> confusions = new List<string>();
        public List<string> confusions_in_set = new List<string>();
    }
}
