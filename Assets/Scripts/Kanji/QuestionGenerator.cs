using System.Collections.Generic;
using System.Linq;
using KanjiMaster.Core;
using Data = KanjiRush.Data;

namespace KanjiMaster.Kanji
{
    /// <summary>
    /// Produces multiple-choice questions from the bundled kanji database.
    /// The prompt is the kanji; the options are English meanings (AnswerLanguage.English)
    /// or kana readings (AnswerLanguage.Japanese). Distractors prefer the target's
    /// in-set confusables so wrong answers are plausible, then fall back to random
    /// entries. Avoids repeating the same kanji twice in a row.
    ///
    /// (The data namespace is aliased to <c>Data</c> because the enclosing namespace
    /// is also called "Kanji", which would otherwise shadow the KanjiRush.Data.Kanji type.)
    /// </summary>
    public class QuestionGenerator
    {
        private const int OptionCount = 4;

        private readonly Data.KanjiDatabase _db;
        private readonly System.Random _rng;
        private int _lastId = -1;

        public int PoolSize => _db.Count;

        /// <summary>Loads the bundled database. Throws if the dataset is missing.</summary>
        public QuestionGenerator(System.Random rng = null)
            : this(Data.KanjiDatabase.Load(), rng) { }

        public QuestionGenerator(Data.KanjiDatabase db, System.Random rng = null)
        {
            _db = db;
            _rng = rng ?? new System.Random();
        }

        public Question Next(AnswerLanguage language)
        {
            var target = PickTarget();
            string correct = Value(target, language);

            var used = new HashSet<string> { correct };
            var distractors = new List<string>();

            // Prefer confusable kanji as distractor sources.
            foreach (var c in target.ConfusionsInSet)
            {
                var k = _db.ByChar(c);
                if (k == null) continue;
                string v = Value(k, language);
                if (!string.IsNullOrEmpty(v) && used.Add(v)) distractors.Add(v);
                if (distractors.Count >= OptionCount - 1) break;
            }

            // Fill the rest from random other kanji.
            if (distractors.Count < OptionCount - 1)
            {
                foreach (int i in ShuffledIndices())
                {
                    string v = Value(_db.All[i], language);
                    if (!string.IsNullOrEmpty(v) && used.Add(v)) distractors.Add(v);
                    if (distractors.Count >= OptionCount - 1) break;
                }
            }

            var options = new List<string> { correct };
            options.AddRange(distractors);
            Shuffle(options);

            _lastId = target.Id;

            return new Question
            {
                Prompt = target.Character,
                KanjiCharacter = target.Character,
                Options = options.ToArray(),
                CorrectIndex = options.IndexOf(correct),
            };
        }

        private Data.Kanji PickTarget()
        {
            if (_db.Count == 1) return _db.All[0];
            Data.Kanji target;
            do { target = _db.All[_rng.Next(_db.Count)]; }
            while (target.Id == _lastId);
            return target;
        }

        private static string Value(Data.Kanji k, AnswerLanguage language)
            => language == AnswerLanguage.Japanese ? k.PrimaryReading : k.PrimaryMeaning;

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
