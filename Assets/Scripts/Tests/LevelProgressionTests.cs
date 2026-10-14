using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the V1 level progression system: the data-driven level catalog,
    /// unlock/completed/progress rules (LevelProgressionService), latching, backward
    /// compatibility and persistence of the selected level.
    ///
    /// The rules read mastery scores through an injectable kanji-id provider, so these
    /// tests use small fake datasets (Rising Star → ids 1,2,3; Young Master → 4,5,6)
    /// and never depend on the bundled Resources.
    /// </summary>
    public class LevelProgressionTests
    {
        private static readonly IReadOnlyList<int> RisingStarIds = new List<int> { 1, 2, 3 };
        private static readonly IReadOnlyList<int> YoungMasterIds = new List<int> { 4, 5, 6 };

        // saved static config, restored after each test
        private System.Func<KanjiLevel, IReadOnlyList<int>> _savedProvider;
        private int _savedThreshold;
        private float _savedFraction;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _savedProvider = LevelProgressionService.KanjiIdsProvider;
            _savedThreshold = LevelProgressionService.MasteryThreshold;
            _savedFraction = LevelProgressionService.CompletionFraction;

            LevelProgressionService.KanjiIdsProvider =
                ds => ds == KanjiLevel.N4 ? YoungMasterIds : RisingStarIds;
            LevelProgressionService.MasteryThreshold = 2;
            LevelProgressionService.CompletionFraction = 1f;

            _path = Path.Combine(Application.persistentDataPath,
                "lvltest_" + System.Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            LevelProgressionService.KanjiIdsProvider = _savedProvider;
            LevelProgressionService.MasteryThreshold = _savedThreshold;
            LevelProgressionService.CompletionFraction = _savedFraction;

            foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                if (File.Exists(_path + ext)) File.Delete(_path + ext);
        }

        // helpers
        private static PlayerProgress Fresh() { var p = new PlayerProgress(); LevelProgressionService.Ensure(p); return p; }
        private static void SetMastery(PlayerProgress p, IEnumerable<int> ids, int score)
        {
            foreach (var id in ids) p.learning.kanjiMastery.SetScore(id, score);
        }

        // ---- 1/2: default unlock state -----------------------------------------
        [Test]
        public void RisingStar_Is_Unlocked_By_Default()
        {
            Assert.IsTrue(LevelProgressionService.IsUnlocked(Fresh(), PlayerLevel.RisingStar));
        }

        [Test]
        public void YoungMaster_Is_Locked_By_Default()
        {
            Assert.IsFalse(LevelProgressionService.IsUnlocked(Fresh(), PlayerLevel.YoungMaster));
        }

        // ---- 3: locked messaging -----------------------------------------------
        [Test]
        public void RequirementText_Explains_How_To_Unlock_YoungMaster()
        {
            Assert.AreEqual("Complete Rising Star to unlock Young Master.",
                LevelProgressionService.RequirementText(PlayerLevel.YoungMaster));
            Assert.AreEqual("", LevelProgressionService.RequirementText(PlayerLevel.RisingStar),
                "an entry level has no prerequisite");
        }

        // ---- 4/5: progress metric ----------------------------------------------
        [Test]
        public void Progress_Is_Zero_With_No_Mastery()
        {
            Assert.AreEqual(0f, LevelProgressionService.Progress01(Fresh(), PlayerLevel.RisingStar));
        }

        [Test]
        public void Progress_Reflects_Fraction_Of_Kanji_At_Threshold()
        {
            var p = Fresh();
            SetMastery(p, new[] { 1, 2 }, 2); // 2 of 3 Rising Star kanji at threshold
            var (done, total) = LevelProgressionService.Counts(p, PlayerLevel.RisingStar);
            Assert.AreEqual(2, done);
            Assert.AreEqual(3, total);
            Assert.AreEqual(2f / 3f, LevelProgressionService.Progress01(p, PlayerLevel.RisingStar), 1e-4f);
        }

        // ---- 6: threshold boundary ---------------------------------------------
        [Test]
        public void Mastery_Below_Threshold_Does_Not_Count()
        {
            var p = Fresh();
            SetMastery(p, new[] { 1 }, 1); // below threshold (2)
            SetMastery(p, new[] { 2 }, 2); // exactly at threshold
            Assert.AreEqual(1, LevelProgressionService.Counts(p, PlayerLevel.RisingStar).done);
        }

        // ---- 7: completion ------------------------------------------------------
        [Test]
        public void RisingStar_Not_Completed_Until_All_Kanji_At_Threshold()
        {
            var p = Fresh();
            SetMastery(p, new[] { 1, 2 }, 4); // only 2 of 3
            Assert.IsFalse(LevelProgressionService.Evaluate(p));
            Assert.IsFalse(LevelProgressionService.IsCompleted(p, PlayerLevel.RisingStar));
        }

        [Test]
        public void Completing_RisingStar_Marks_It_Completed_And_Unlocks_YoungMaster()
        {
            var p = Fresh();
            SetMastery(p, RisingStarIds, 4); // all Rising Star kanji correct
            bool changed = LevelProgressionService.Evaluate(p);

            Assert.IsTrue(changed, "evaluate reports a change");
            Assert.IsTrue(LevelProgressionService.IsCompleted(p, PlayerLevel.RisingStar));
            Assert.IsTrue(LevelProgressionService.IsUnlocked(p, PlayerLevel.YoungMaster));
        }

        // ---- 8/9: latching ------------------------------------------------------
        [Test]
        public void Unlock_Is_Latched_When_Mastery_Regresses()
        {
            var p = Fresh();
            SetMastery(p, RisingStarIds, 4);
            LevelProgressionService.Evaluate(p);          // unlock earned
            SetMastery(p, RisingStarIds, 1);              // mastery drops below threshold
            LevelProgressionService.Evaluate(p);

            Assert.IsTrue(LevelProgressionService.IsUnlocked(p, PlayerLevel.YoungMaster),
                "an earned unlock is never revoked");
        }

        [Test]
        public void Completion_Is_Latched_When_Mastery_Regresses()
        {
            var p = Fresh();
            SetMastery(p, RisingStarIds, 4);
            LevelProgressionService.Evaluate(p);
            SetMastery(p, RisingStarIds, 1);
            LevelProgressionService.Evaluate(p);

            Assert.IsTrue(LevelProgressionService.IsCompleted(p, PlayerLevel.RisingStar));
            Assert.Less(LevelProgressionService.Progress01(p, PlayerLevel.RisingStar), 1f,
                "live progress can dip even though completion stays latched");
        }

        // ---- 10: no unlock while prerequisite incomplete ------------------------
        [Test]
        public void YoungMaster_Stays_Locked_While_RisingStar_Incomplete()
        {
            var p = Fresh();
            SetMastery(p, new[] { 1 }, 4); // partial
            LevelProgressionService.Evaluate(p);
            Assert.IsFalse(LevelProgressionService.IsUnlocked(p, PlayerLevel.YoungMaster));
        }

        // ---- 11: idempotent evaluate -------------------------------------------
        [Test]
        public void Evaluate_Returns_False_When_Nothing_Changes()
        {
            var p = Fresh();
            SetMastery(p, RisingStarIds, 4);
            Assert.IsTrue(LevelProgressionService.Evaluate(p));  // first: unlock happens
            Assert.IsFalse(LevelProgressionService.Evaluate(p)); // second: no change
        }

        // ---- 12: ensure is idempotent ------------------------------------------
        [Test]
        public void Ensure_Seeds_Each_Level_Once_And_Is_Idempotent()
        {
            var p = new PlayerProgress();
            LevelProgressionService.Ensure(p);
            LevelProgressionService.Ensure(p);
            Assert.AreEqual(LevelCatalog.All.Count, p.learning.levelProgress.levels.Count,
                "no duplicate level states");
        }

        // ---- 13: catalog data ---------------------------------------------------
        [Test]
        public void Catalog_Maps_Levels_To_Their_Datasets()
        {
            Assert.AreEqual(KanjiLevel.N5, LevelCatalog.Get(PlayerLevel.RisingStar).Dataset);
            Assert.AreEqual(KanjiLevel.N4, LevelCatalog.Get(PlayerLevel.YoungMaster).Dataset);
        }

        // ---- 14: display names --------------------------------------------------
        [Test]
        public void Display_Names_Are_Player_Facing()
        {
            Assert.AreEqual("Rising Star", LevelCatalog.DisplayName(PlayerLevel.RisingStar));
            Assert.AreEqual("Young Master", LevelCatalog.DisplayName(PlayerLevel.YoungMaster));
        }

        // ---- 15: backward compatibility ----------------------------------------
        [Test]
        public void Legacy_V2_Save_Loads_With_Level_Defaults_And_Preserves_Data()
        {
            // A v2 nested save (no `levels` list yet), with mastery + sessions + xp.
            File.WriteAllText(_path,
                "{\"schemaVersion\":2,\"learning\":{\"kanjiMastery\":{\"entries\":" +
                "[{\"kanjiId\":27700,\"score\":8}]}},\"activity\":{\"sessions\":{\"totalSessions\":4}}," +
                "\"progression\":{\"xp\":{\"totalXp\":30}}}");

            var loaded = new LocalPlayerProgressStore(_path).Load();

            Assert.AreEqual(ProgressMigration.CurrentSchemaVersion, loaded.schemaVersion, "bumped to current");
            Assert.IsTrue(LevelProgressionService.IsUnlocked(loaded, PlayerLevel.RisingStar), "default unlocked");
            Assert.IsFalse(LevelProgressionService.IsUnlocked(loaded, PlayerLevel.YoungMaster), "default locked");
            Assert.AreEqual(8, loaded.learning.kanjiMastery.GetScore(27700), "mastery preserved");
            Assert.AreEqual(4, loaded.activity.sessions.totalSessions, "sessions preserved");
            Assert.AreEqual(30, loaded.progression.xp.totalXp, "xp preserved");
        }

        // ---- 16: selected-level persistence ------------------------------------
        [Test]
        public void Selected_Level_Persists_Across_Save_And_Load()
        {
            var store = new LocalPlayerProgressStore(_path);
            var p = new PlayerProgress();
            LevelProgressionService.Ensure(p);
            p.learning.levelProgress.currentLevel = PlayerLevel.YoungMaster;
            store.Save(p);

            var loaded = store.Load();
            Assert.AreEqual(PlayerLevel.YoungMaster, loaded.learning.levelProgress.currentLevel);
        }
    }
}
