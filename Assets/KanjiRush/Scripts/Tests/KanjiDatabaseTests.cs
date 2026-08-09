using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KanjiRush.Data;

namespace KanjiRush.Tests
{
    /// <summary>
    /// EditMode tests mirroring the Python pipeline's guarantees, but exercised
    /// through the real Unity load path. Run via Window > General > Test Runner.
    /// </summary>
    public class KanjiDatabaseTests
    {
        private KanjiDatabase _db;

        [SetUp]
        public void SetUp() => _db = KanjiDatabase.Load();

        [Test]
        public void Loads_The_Bundled_N5_Set()
        {
            Assert.AreEqual("N5", _db.JlptLevel);
            Assert.AreEqual(50, _db.Count);
        }

        [Test]
        public void All_Characters_Are_Unique()
        {
            var distinct = _db.All.Select(k => k.Character).Distinct().Count();
            Assert.AreEqual(_db.Count, distinct);
        }

        [Test]
        public void Every_Kanji_Has_Meaning_And_A_Reading()
        {
            foreach (var k in _db.All)
            {
                Assert.IsNotEmpty(k.Meanings, $"{k.Character} has no meanings");
                Assert.IsTrue(k.HasOnyomi || k.HasKunyomi, $"{k.Character} has no readings");
                Assert.IsFalse(string.IsNullOrEmpty(k.PrimaryReading), $"{k.Character} has no primary reading");
            }
        }

        [Test]
        public void No_Kanji_Lists_Itself_As_A_Confusable()
        {
            foreach (var k in _db.All)
                CollectionAssert.DoesNotContain(k.Confusions, k.Character, $"{k.Character} confuses itself");
        }

        [Test]
        public void Every_Kanji_Can_Build_A_Four_Option_ReadingToKanji_Question()
        {
            var pool = _db.All.Select(k => k.Character).ToList();
            foreach (var k in _db.All)
            {
                var distractors = k.ConfusionsInSet.Where(c => c != k.Character).ToList();
                var extra = pool.Where(c => c != k.Character && !distractors.Contains(c));
                var options = new List<string> { k.Character };
                options.AddRange(distractors);
                options.AddRange(extra);
                var four = options.Take(4).ToList();
                Assert.AreEqual(4, four.Distinct().Count(), $"{k.Character} could not build 4 unique options");
                CollectionAssert.Contains(four, k.Character);
            }
        }

        [Test]
        public void Meaning_Distractor_Pool_Is_Large_Enough()
        {
            var allMeanings = new HashSet<string>(_db.All.SelectMany(k => k.Meanings));
            foreach (var k in _db.All)
            {
                var others = allMeanings.Except(k.Meanings).Count();
                Assert.GreaterOrEqual(others, 3, $"{k.Character} lacks 3 distractor meanings");
            }
        }

        [Test]
        public void ByChar_Lookup_Works()
        {
            Assert.AreEqual("water", _db.ByChar("水").PrimaryMeaning);
            Assert.IsNull(_db.ByChar("龍"));
        }
    }
}
