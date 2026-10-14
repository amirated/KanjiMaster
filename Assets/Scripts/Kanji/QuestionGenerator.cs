using System;
using System.Collections.Generic;
using System.Linq;
using KanjiMaster.Core;
using Data = KanjiRush.Data;

namespace KanjiMaster.Kanji
{
    /// <summary>Thrown when a kanji cannot produce a valid question for a mode —
    /// surfaced clearly rather than emitting a broken question.</summary>
    public class QuestionGenerationException : Exception
    {
        public QuestionGenerationException(string kanji, AnswerMode mode, string reason)
            : base($"Cannot build a {mode} question for '{kanji}': {reason}") { }
    }

    /// <summary>
    /// Question GENERATION: given a target kanji + AnswerMode, produce a 4-option
    /// multiple-choice question. The prompt is always the kanji; option VALUES come
    /// from the canonical dataset field for the mode (English=meaning, Romaji=romaji,
    /// Kana=primary_reading). Distractors prefer the target's in-set confusables,
    /// then fall back to random entries; the correct value is never a distractor.
    ///
    /// (KanjiRush.Data is aliased to <c>Data</c> because this namespace is also
    /// called "Kanji", which would otherwise shadow the KanjiRush.Data.Kanji type.)
    /// </summary>
    public class QuestionGenerator
    {
        public const int OptionCount = 4;

        private readonly Data.KanjiDatabase _db;
        private readonly System.Random _rng;

        public QuestionGenerator(Data.KanjiDatabase db, System.Random rng = null)
        {
            _db = db;
            _rng = rng ?? new System.Random();
        }

        /// <summary>The canonical answer value for a kanji in a given mode.</summary>
        public static string ValueFor(Data.Kanji k, AnswerMode mode)
        {
            switch (mode)
            {
                case AnswerMode.Romaji: return k.Romaji;
                case AnswerMode.Kana: return k.PrimaryReading;
                case AnswerMode.English:
                default: return k.PrimaryMeaning;
            }
        }

        public Question Build(Data.Kanji target, AnswerMode mode)
        {
            string correct = ValueFor(target, mode);
            if (string.IsNullOrEmpty(correct))
                throw new QuestionGenerationException(target.Character, mode, "target has no value for this mode");

            var used = new HashSet<string> { correct };
            var distractors = new List<string>();

            // Prefer confusable kanji as distractor sources (from the dataset).
            foreach (var c in target.ConfusionsInSet)
            {
                var k = _db.ByChar(c);
                if (k == null) continue;
                string v = ValueFor(k, mode);
                if (!string.IsNullOrEmpty(v) && used.Add(v)) distractors.Add(v);
                if (distractors.Count >= OptionCount - 1) break;
            }

            // Fill the rest from random other kanji.
            if (distractors.Count < OptionCount - 1)
            {
                foreach (int i in ShuffledIndices())
                {
                    string v = ValueFor(_db.All[i], mode);
                    if (!string.IsNullOrEmpty(v) && used.Add(v)) distractors.Add(v);
                    if (distractors.Count >= OptionCount - 1) break;
                }
            }

            if (distractors.Count < OptionCount - 1)
                throw new QuestionGenerationException(target.Character, mode,
                    $"only {distractors.Count} distinct distractor(s) available (need {OptionCount - 1})");

            var options = new List<string> { correct };
            options.AddRange(distractors);
            Shuffle(options);

            return new Question
            {
                Prompt = target.Character,
                KanjiCharacter = target.Character,
                Options = options.ToArray(),
                CorrectIndex = options.IndexOf(correct),
            };
        }

        private IEnumerable<int> ShuffledIndices()
        {
            var order = Enumerable.Range(0, _db.Count).ToList();
            Shuffle(order);
            return order;
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
