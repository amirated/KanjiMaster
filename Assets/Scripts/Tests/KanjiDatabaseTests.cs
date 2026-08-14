using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KanjiRush.Data;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Data-level EditMode tests exercised through the real Unity load path, for
    /// both the N5 and N4 datasets. Content reliability only — no gameplay logic.
    /// Run via Window > General > Test Runner.
    /// </summary>
    public class KanjiDatabaseTests
    {
        // Expected counts are asserted so unexpected dataset changes are caught.
        private const int ExpectedN5 = 84;
        private const int ExpectedN4 = 162;

        private KanjiDatabase _n5;
        private KanjiDatabase _n4;

        [SetUp]
        public void SetUp()
        {
            _n5 = KanjiDatabase.Load("kanji_n5");
            _n4 = KanjiDatabase.Load("kanji_n4");
        }

        [Test]
        public void Loads_N5_And_N4_With_Expected_Levels_And_Counts()
        {
            Assert.AreEqual("N5", _n5.JlptLevel);
            Assert.AreEqual(ExpectedN5, _n5.Count);
            Assert.AreEqual("N4", _n4.JlptLevel);
            Assert.AreEqual(ExpectedN4, _n4.Count);
        }

        [Test]
        public void All_Characters_Are_Unique_Per_Level()
        {
            foreach (var db in new[] { _n5, _n4 })
                Assert.AreEqual(db.Count, db.All.Select(k => k.Character).Distinct().Count());
        }

        [Test]
        public void No_Overlap_Between_N5_And_N4()
        {
            var n5 = new HashSet<string>(_n5.All.Select(k => k.Character));
            var overlap = _n4.All.Select(k => k.Character).Where(n5.Contains).ToList();
            CollectionAssert.IsEmpty(overlap, "N5 and N4 share characters: " + string.Join(",", overlap));
        }

        [Test]
        public void Every_Kanji_Has_Required_Content()
        {
            foreach (var db in new[] { _n5, _n4 })
            foreach (var k in db.All)
            {
                Assert.IsNotEmpty(k.Meanings, $"{k.Character} has no meanings");
                Assert.IsTrue(k.HasOnyomi || k.HasKunyomi, $"{k.Character} has no readings");
                Assert.IsFalse(string.IsNullOrEmpty(k.PrimaryReading), $"{k.Character} has no primary reading");
                Assert.IsFalse(string.IsNullOrEmpty(k.Romaji), $"{k.Character} has no romaji");
                Assert.Greater(k.Strokes, 0, $"{k.Character} has invalid stroke count");
            }
        }

        [Test]
        public void No_Kanji_Lists_Itself_As_A_Confusable()
        {
            foreach (var db in new[] { _n5, _n4 })
            foreach (var k in db.All)
                CollectionAssert.DoesNotContain(k.Confusions, k.Character, $"{k.Character} confuses itself");
        }

        [Test]
        public void Every_Kanji_Can_Build_A_Four_Option_ReadingToKanji_Question()
        {
            foreach (var db in new[] { _n5, _n4 })
            {
                var pool = db.All.Select(k => k.Character).ToList();
                foreach (var k in db.All)
                {
                    var distractors = k.ConfusionsInSet.Where(c => c != k.Character).ToList();
                    var extra = pool.Where(c => c != k.Character && !distractors.Contains(c));
                    var four = new List<string> { k.Character };
                    four.AddRange(distractors);
                    four.AddRange(extra);
                    Assert.AreEqual(4, four.Take(4).Distinct().Count(), $"{k.Character} could not build 4 unique options");
                }
            }
        }

        [Test]
        public void Meaning_And_Reading_Distractor_Pools_Are_Large_Enough()
        {
            foreach (var db in new[] { _n5, _n4 })
            {
                var allMeanings = new HashSet<string>(db.All.SelectMany(k => k.Meanings));
                var allReadings = new HashSet<string>(db.All.Select(k => k.PrimaryReading));
                foreach (var k in db.All)
                {
                    Assert.GreaterOrEqual(allMeanings.Except(k.Meanings).Count(), 3, $"{k.Character} lacks 3 distractor meanings");
                    Assert.GreaterOrEqual(allReadings.Except(new[] { k.PrimaryReading }).Count(), 3, $"{k.Character} lacks 3 distractor readings");
                }
            }
        }

        [Test]
        public void ByChar_Lookup_Works()
        {
            Assert.AreEqual("water", _n5.ByChar("水").PrimaryMeaning);
            Assert.IsNull(_n5.ByChar("龍"));
            Assert.IsNotNull(_n4.ByChar("会"));
        }
    }
}
