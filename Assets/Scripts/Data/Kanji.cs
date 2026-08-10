using System.Collections.Generic;

namespace KanjiRush.Data
{
    /// <summary>
    /// Clean, immutable domain model for a single kanji. Gameplay code consumes
    /// this rather than the raw <see cref="KanjiEntryDto"/> transport type.
    /// </summary>
    public sealed class Kanji
    {
        public int Id { get; private set; }
        public string Character { get; private set; }
        public IReadOnlyList<string> Meanings { get; private set; }
        public IReadOnlyList<string> Onyomi { get; private set; }
        public IReadOnlyList<string> Kunyomi { get; private set; }

        /// <summary>Reading tested first in the reading→kanji prompt.</summary>
        public string PrimaryReading { get; private set; }
        public string Romaji { get; private set; }
        public int Strokes { get; private set; }

        /// <summary>Teaching order tier (1 = introduced first).</summary>
        public int Tier { get; private set; }

        /// <summary>Visually/semantically confusable kanji (may be outside the set).</summary>
        public IReadOnlyList<string> Confusions { get; private set; }

        /// <summary>Confusables that exist in the loaded set — preferred distractors.</summary>
        public IReadOnlyList<string> ConfusionsInSet { get; private set; }

        public string PrimaryMeaning =>
            (Meanings != null && Meanings.Count > 0) ? Meanings[0] : string.Empty;

        public bool HasOnyomi => Onyomi != null && Onyomi.Count > 0;
        public bool HasKunyomi => Kunyomi != null && Kunyomi.Count > 0;

        internal static Kanji FromDto(KanjiEntryDto d)
        {
            return new Kanji
            {
                Id = d.id,
                Character = d.@char,
                Meanings = (d.meanings ?? new List<string>()).AsReadOnly(),
                Onyomi = (d.onyomi ?? new List<string>()).AsReadOnly(),
                Kunyomi = (d.kunyomi ?? new List<string>()).AsReadOnly(),
                PrimaryReading = d.primary_reading,
                Romaji = d.romaji,
                Strokes = d.strokes,
                Tier = d.tier,
                Confusions = (d.confusions ?? new List<string>()).AsReadOnly(),
                ConfusionsInSet = (d.confusions_in_set ?? new List<string>()).AsReadOnly(),
            };
        }

        public override string ToString() => $"{Character} ({PrimaryMeaning})";
    }
}
