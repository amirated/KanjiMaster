using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KanjiRush.Data
{
    /// <summary>
    /// Loads the bundled N5 dataset and provides fast lookups. The full set is
    /// small (~50 entries) and ships inside the app, so the game plays instantly
    /// and offline — no network round-trip in the 30-second core loop.
    /// </summary>
    public sealed class KanjiDatabase
    {
        /// <summary>Resources path (no extension) for the bundled dataset.</summary>
        public const string DefaultResourcePath = "kanji_n5";

        private readonly List<Kanji> _all;
        private readonly Dictionary<string, Kanji> _byChar;

        public IReadOnlyList<Kanji> All => _all;
        public int Count => _all.Count;
        public string JlptLevel { get; private set; }
        public int SchemaVersion { get; private set; }

        private KanjiDatabase(List<Kanji> all, string jlptLevel, int schemaVersion)
        {
            _all = all;
            _byChar = all.ToDictionary(k => k.Character, k => k);
            JlptLevel = jlptLevel;
            SchemaVersion = schemaVersion;
        }

        /// <summary>Load and parse the dataset from Resources.</summary>
        public static KanjiDatabase Load(string resourcePath = DefaultResourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Kanji dataset not found at Resources/{resourcePath}.json. " +
                    "Run tools/build_dataset.py to regenerate it.");
            }
            return Parse(asset.text);
        }

        /// <summary>Parse a dataset from raw JSON text (used by Load and by tests).</summary>
        public static KanjiDatabase Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Kanji dataset JSON was empty.", nameof(json));

            var dto = JsonUtility.FromJson<KanjiDatasetDto>(json);
            if (dto == null || dto.kanji == null || dto.kanji.Count == 0)
                throw new InvalidOperationException("Kanji dataset JSON contained no kanji.");

            var all = dto.kanji.Select(Kanji.FromDto).ToList();

            var seen = new HashSet<string>();
            foreach (var k in all)
            {
                if (!seen.Add(k.Character))
                    throw new InvalidOperationException($"Duplicate kanji in dataset: {k.Character}");
            }

            string level = dto.meta != null ? dto.meta.jlpt_level : "unknown";
            int schema = dto.meta != null ? dto.meta.schema_version : 0;
            return new KanjiDatabase(all, level, schema);
        }

        public bool TryGet(string character, out Kanji kanji) => _byChar.TryGetValue(character, out kanji);

        public Kanji ByChar(string character) =>
            _byChar.TryGetValue(character, out var k) ? k : null;

        public IReadOnlyList<Kanji> ByTier(int tier) =>
            _all.Where(k => k.Tier == tier).ToList();
    }
}
