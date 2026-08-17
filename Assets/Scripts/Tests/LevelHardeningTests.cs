using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KanjiMaster.Core;
using KanjiMaster.Kanji;
using KanjiMaster.Progression;
using Data = KanjiRush.Data;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Progression-hardening tests: mastery-weighted, level-safe, unique selection
    /// (small/empty/all-high pools), level availability + content gating, and
    /// completion/unlock persistence. Uses the real level datasets for selection and a
    /// fake id-provider for the availability rules.
    /// </summary>
    public class LevelHardeningTests
    {
        private IReadOnlyList<Data.Kanji> _n5;
        private IReadOnlyList<Data.Kanji> _n4;

        private System.Func<KanjiLevel, IReadOnlyList<int>> _savedIds;
        private int _savedThreshold;
        private float _savedFraction;

        [SetUp]
        public void SetUp()
        {
            _n5 = QuestionPool.Load(KanjiLevel.N5).Database.All;
            _n4 = QuestionPool.Load(KanjiLevel.N4).Database.All;

            _savedIds = LevelProgressionService.KanjiIdsProvider;
            _savedThreshold = LevelProgressionService.MasteryThreshold;
            _savedFraction = LevelProgressionService.CompletionFraction;
        }

        [TearDown]
        public void TearDown()
        {
            LevelProgressionService.KanjiIdsProvider = _savedIds;
            LevelProgressionService.MasteryThreshold = _savedThreshold;
            LevelProgressionService.CompletionFraction = _savedFraction;
        }

        private static readonly System.Func<Data.Kanji, int> ZeroMastery = _ => 0;

        // ---- Uniqueness + level safety (5–9, 14) --------------------------------
        [Test]
        public void Normal_Run_Is_15_Unique_Kanji_From_The_Level_Pool()
        {
            var picked = MasteryWeightedSelector.SelectUnique(_n5, 15, ZeroMastery, new System.Random(1));
            Assert.AreEqual(15, picked.Count);
            Assert.AreEqual(15, picked.Select(k => k.Character).Distinct().Count(), "no duplicates in a run");
            var n5Set = new HashSet<string>(_n5.Select(k => k.Character));
            Assert.IsTrue(picked.All(k => n5Set.Contains(k.Character)), "only Rising Star kanji");
        }

        [Test]
        public void Selection_Never_Leaks_Across_Levels()
        {
            var n4Set = new HashSet<string>(_n4.Select(k => k.Character));
            for (int seed = 0; seed < 20; seed++)
            {
                var picked = MasteryWeightedSelector.SelectUnique(_n5, 15, ZeroMastery, new System.Random(seed));
                Assert.IsFalse(picked.Any(k => n4Set.Contains(k.Character)),
                    "a Rising Star run must contain no Young Master kanji");
            }
        }

        [Test]
        public void Repeated_Runs_Are_Each_Internally_Unique()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var picked = MasteryWeightedSelector.SelectUnique(_n5, 15, ZeroMastery, new System.Random(seed));
                Assert.AreEqual(picked.Count, picked.Select(k => k.Character).Distinct().Count());
            }
        }

        // ---- Mastery weighting (10–13) -----------------------------------------
        [Test]
        public void Weight_Decreases_With_Mastery_And_Is_Never_Zero()
        {
            Assert.Greater(MasteryWeightedSelector.Weight(0), MasteryWeightedSelector.Weight(5));
            Assert.Greater(MasteryWeightedSelector.Weight(5), MasteryWeightedSelector.Weight(10));
            Assert.Greater(MasteryWeightedSelector.Weight(10), MasteryWeightedSelector.Weight(18));
            Assert.Greater(MasteryWeightedSelector.Weight(18), MasteryWeightedSelector.Weight(30));
            Assert.Greater(MasteryWeightedSelector.Weight(30), MasteryWeightedSelector.Weight(100));
            Assert.GreaterOrEqual(MasteryWeightedSelector.Weight(9999), 1, "never zero → always selectable");
        }

        [Test]
        public void Low_Mastery_Kanji_Are_Strongly_Favoured()
        {
            // One low-mastery kanji (score 0 → weight 32) vs one high (score 100 → weight 1).
            var a = _n5[0];
            var b = _n5[1];
            System.Func<Data.Kanji, int> mastery = k => k.Character == a.Character ? 0 : 100;

            int aWins = 0;
            var rng = new System.Random(12345);
            const int trials = 3300;
            for (int i = 0; i < trials; i++)
            {
                var pick = MasteryWeightedSelector.SelectUnique(new[] { a, b }, 1, mastery, rng);
                if (pick[0].Character == a.Character) aWins++;
            }
            // Expected ≈ 32/33 ≈ 97%. Assert a clear, deterministic majority.
            Assert.Greater(aWins, (int)(trials * 0.9), "low-mastery kanji dominate selection");
        }

        // ---- Small pools (15–17) ------------------------------------------------
        [Test]
        public void Fewer_Than_15_Available_Returns_All_Unique_No_Padding()
        {
            var small = _n5.Take(5).ToList();
            var picked = MasteryWeightedSelector.SelectUnique(small, 15, ZeroMastery, new System.Random(1));
            Assert.AreEqual(5, picked.Count, "returns what exists — never pads to 15");
            Assert.AreEqual(5, picked.Select(k => k.Character).Distinct().Count(), "and no duplicates");
        }

        [TestCase(1)]
        [TestCase(14)]
        [TestCase(15)]
        public void Small_Pool_Sizes_Are_Handled_Safely(int size)
        {
            var pool = _n5.Take(size).ToList();
            var picked = MasteryWeightedSelector.SelectUnique(pool, 15, ZeroMastery, new System.Random(7));
            Assert.AreEqual(System.Math.Min(size, 15), picked.Count);
            Assert.AreEqual(picked.Count, picked.Select(k => k.Character).Distinct().Count());
        }

        [Test]
        public void More_Than_15_Returns_Exactly_15_Unique()
        {
            var big = _n5.Take(20).ToList();
            var picked = MasteryWeightedSelector.SelectUnique(big, 15, ZeroMastery, new System.Random(3));
            Assert.AreEqual(15, picked.Count);
            Assert.AreEqual(15, picked.Select(k => k.Character).Distinct().Count());
        }

        // ---- All-high-mastery (24) ---------------------------------------------
        [Test]
        public void All_High_Mastery_Still_Produces_A_Valid_Unique_Session()
        {
            System.Func<Data.Kanji, int> allHigh = _ => 999;
            var picked = MasteryWeightedSelector.SelectUnique(_n5, 15, allHigh, new System.Random(2));
            Assert.AreEqual(15, picked.Count, "weighting never empties the session");
            Assert.AreEqual(15, picked.Select(k => k.Character).Distinct().Count());
        }

        // ---- Empty pool (18–20) -------------------------------------------------
        [Test]
        public void Empty_Pool_Returns_Empty_No_Crash_No_Null()
        {
            var picked = MasteryWeightedSelector.SelectUnique(new List<Data.Kanji>(), 15, ZeroMastery, new System.Random(1));
            Assert.IsNotNull(picked);
            Assert.AreEqual(0, picked.Count);
        }

        // ---- Level availability + content gating (1–4, 18, 26) -----------------
        [Test]
        public void New_Player_Level_Availability_Is_Correct()
        {
            LevelProgressionService.KanjiIdsProvider = ds =>
                ds == KanjiLevel.N4 ? (IReadOnlyList<int>)new List<int> { 4, 5, 6 } : new List<int> { 1, 2, 3 };

            var p = new PlayerProgress();
            LevelProgressionService.Ensure(p);

            Assert.IsTrue(LevelProgressionService.IsUnlocked(p, PlayerLevel.RisingStar), "Rising Star unlocked");
            Assert.IsFalse(LevelProgressionService.IsUnlocked(p, PlayerLevel.YoungMaster), "Young Master locked");
            Assert.IsTrue(LevelProgressionService.IsPlayable(p, PlayerLevel.RisingStar));
            Assert.IsFalse(LevelProgressionService.IsPlayable(p, PlayerLevel.YoungMaster), "locked → not playable");
            Assert.IsFalse(LevelProgressionService.HasContent(PlayerLevel.Adept), "future level has no definition/content");
            Assert.IsFalse(LevelProgressionService.IsPlayable(p, PlayerLevel.Adept));
        }

        [Test]
        public void A_Defined_But_Empty_Level_Is_Not_Playable()
        {
            // Simulate a level whose dataset is present but empty.
            LevelProgressionService.KanjiIdsProvider = ds =>
                ds == KanjiLevel.N4 ? (IReadOnlyList<int>)new List<int>() : new List<int> { 1, 2, 3 };

            var p = new PlayerProgress();
            LevelProgressionService.Ensure(p);
            p.learning.levelProgress.GetOrAdd(PlayerLevel.YoungMaster).unlocked = true; // even if unlocked…

            Assert.IsFalse(LevelProgressionService.HasContent(PlayerLevel.YoungMaster), "empty pool → no content");
            Assert.IsFalse(LevelProgressionService.IsPlayable(p, PlayerLevel.YoungMaster), "…empty level can't start");
        }

        [Test]
        public void Missing_Dataset_Does_Not_Throw_And_Reports_No_Content()
        {
            // Provider throws (as the real loader does for a missing resource).
            LevelProgressionService.KanjiIdsProvider = _ => throw new System.InvalidOperationException("missing");
            Assert.DoesNotThrow(() => LevelProgressionService.ContentCount(PlayerLevel.RisingStar));
            Assert.AreEqual(0, LevelProgressionService.ContentCount(PlayerLevel.RisingStar));
            Assert.IsFalse(LevelProgressionService.HasContent(PlayerLevel.RisingStar));
        }

        // ---- Completion → unlock persists (23–26) ------------------------------
        [Test]
        public void Completing_Rising_Star_Unlocks_Young_Master_And_Persists()
        {
            LevelProgressionService.KanjiIdsProvider = ds =>
                ds == KanjiLevel.N4 ? (IReadOnlyList<int>)new List<int> { 4, 5, 6 } : new List<int> { 1, 2, 3 };
            LevelProgressionService.MasteryThreshold = 2;
            LevelProgressionService.CompletionFraction = 1f;

            var p = new PlayerProgress();
            foreach (var id in new[] { 1, 2, 3 }) p.learning.kanjiMastery.SetScore(id, 4); // all RS mastered

            Assert.IsTrue(LevelProgressionService.Evaluate(p), "completion changes state");
            Assert.IsTrue(LevelProgressionService.IsUnlocked(p, PlayerLevel.YoungMaster));

            // Survives a JSON round trip (persistence).
            string json = SaveDataMigration.Serialize(p);
            var loaded = SaveDataMigration.Load(json).Progress;
            Assert.IsTrue(LevelProgressionService.IsUnlocked(loaded, PlayerLevel.YoungMaster), "unlock persists");
            Assert.IsTrue(LevelProgressionService.IsCompleted(loaded, PlayerLevel.RisingStar));
        }

        // ---- Extensibility (31–32) ---------------------------------------------
        [Test]
        public void Level_Metadata_Is_Data_Driven_And_Ordered()
        {
            Assert.GreaterOrEqual(LevelCatalog.All.Count, 2);
            Assert.AreEqual(0, LevelCatalog.Get(PlayerLevel.RisingStar).Order);
            Assert.AreEqual("N5", LevelCatalog.Get(PlayerLevel.RisingStar).JlptEquivalent, "JLPT kept as metadata only");
            Assert.AreEqual("N4", LevelCatalog.Get(PlayerLevel.YoungMaster).JlptEquivalent);
            // The selector is level-agnostic: it operates on any pool handed to it, so a
            // future level needs only a definition + dataset, not selection-logic changes.
            var pickedFromN4 = MasteryWeightedSelector.SelectUnique(_n4, 15, ZeroMastery, new System.Random(9));
            Assert.AreEqual(15, pickedFromN4.Count);
        }
    }
}
