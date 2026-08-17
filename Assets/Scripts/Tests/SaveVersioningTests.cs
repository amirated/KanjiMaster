using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the save-versioning boundary: the versioned SaveData envelope, loading
    /// current + legacy (pre-envelope) saves without data loss, deterministic migration,
    /// safe handling of malformed / invalid / future saves, default-player creation, and
    /// a full-progression round trip. The envelope wraps the existing PlayerProgress; the
    /// inner schema migration (ProgressMigration) is exercised by PersistenceTests.
    /// </summary>
    public class SaveVersioningTests
    {
        private string _path;

        [SetUp]
        public void SetUp() =>
            _path = Path.Combine(Application.persistentDataPath, "savever_" + System.Guid.NewGuid().ToString("N") + ".json");

        [TearDown]
        public void TearDown()
        {
            foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                if (File.Exists(_path + ext)) File.Delete(_path + ext);
        }

        // Representative, fully-populated progress used by several tests.
        private static PlayerProgress Representative()
        {
            var p = new PlayerProgress();
            p.learning.kanjiMastery.SetScore(KanjiId.Of("水"), 8);
            p.learning.kanjiMastery.SetScore(KanjiId.Of("火"), 3);
            p.learning.kanjiMastery.SetScore(KanjiId.Of("山"), 24);
            LevelProgressionService.Ensure(p);
            p.learning.levelProgress.currentLevel = PlayerLevel.YoungMaster;
            p.learning.levelProgress.GetOrAdd(PlayerLevel.RisingStar).completed = true;
            p.learning.levelProgress.GetOrAdd(PlayerLevel.YoungMaster).unlocked = true;
            p.activity.sessions.totalSessions = 12;
            p.activity.streak.currentStreak = 4;
            p.activity.streak.longestStreak = 9;
            p.activity.streak.lastPlayedDate = "2026-08-16";
            p.progression.xp.totalXp = 1340;
            p.progression.xp.xpSystemVersion = XpConfig.CurrentVersion;
            return p;
        }

        private static void AssertSameProgress(PlayerProgress a, PlayerProgress b)
        {
            Assert.AreEqual(a.progression.xp.totalXp, b.progression.xp.totalXp, "XP");
            Assert.AreEqual(a.progression.xp.xpSystemVersion, b.progression.xp.xpSystemVersion, "XP version");
            Assert.AreEqual(a.learning.kanjiMastery.GetScore(KanjiId.Of("水")), b.learning.kanjiMastery.GetScore(KanjiId.Of("水")));
            Assert.AreEqual(a.learning.kanjiMastery.GetScore(KanjiId.Of("火")), b.learning.kanjiMastery.GetScore(KanjiId.Of("火")));
            Assert.AreEqual(a.learning.kanjiMastery.GetScore(KanjiId.Of("山")), b.learning.kanjiMastery.GetScore(KanjiId.Of("山")));
            Assert.AreEqual(a.learning.levelProgress.currentLevel, b.learning.levelProgress.currentLevel, "current level");
            Assert.AreEqual(a.learning.levelProgress.Find(PlayerLevel.RisingStar).completed,
                            b.learning.levelProgress.Find(PlayerLevel.RisingStar).completed, "RS completed");
            Assert.AreEqual(a.learning.levelProgress.Find(PlayerLevel.YoungMaster).unlocked,
                            b.learning.levelProgress.Find(PlayerLevel.YoungMaster).unlocked, "YM unlocked");
            Assert.AreEqual(a.activity.sessions.totalSessions, b.activity.sessions.totalSessions, "sessions");
            Assert.AreEqual(a.activity.streak.currentStreak, b.activity.streak.currentStreak, "current streak");
            Assert.AreEqual(a.activity.streak.longestStreak, b.activity.streak.longestStreak, "longest streak");
            Assert.AreEqual(a.activity.streak.lastPlayedDate, b.activity.streak.lastPlayedDate, "last active date");
        }

        // ---- New save (1–3) -----------------------------------------------------
        [Test]
        public void New_Player_Produces_Valid_Version_1_Envelope()
        {
            string json = SaveDataMigration.Serialize(new PlayerProgress());
            var env = JsonUtility.FromJson<SaveData>(json);
            Assert.IsNotNull(env.playerProgress);          // (3) PlayerProgress serialized
            Assert.AreEqual(1, env.version);               // (1)(2) version stored as 1
            Assert.AreEqual(SaveData.CurrentVersion, env.version);
        }

        // ---- Load Version 1 (4–10) ---------------------------------------------
        [Test]
        public void Version_1_Save_Loads_With_All_Values_Unchanged()
        {
            var original = Representative();
            var result = SaveDataMigration.Load(SaveDataMigration.Serialize(original));
            Assert.AreEqual(SaveLoadStatus.Ok, result.Status);   // (4)
            AssertSameProgress(original, result.Progress);        // (5–10) XP/mastery/levels/sessions/streak
        }

        // ---- Legacy / unversioned (11–14) --------------------------------------
        [Test]
        public void Legacy_Pre_Envelope_Root_PlayerProgress_Loads_Without_Loss()
        {
            // The pre-envelope format: a root PlayerProgress (schemaVersion inside), no envelope.
            string legacy = JsonUtility.ToJson(Representative());
            Assert.IsFalse(legacy.Contains("playerProgress"), "sanity: legacy is not an envelope");

            var result = SaveDataMigration.Load(legacy);
            Assert.AreEqual(SaveLoadStatus.Ok, result.Status);        // (11) loads
            AssertSameProgress(Representative(), result.Progress);     // (12)(13) interpreted correctly, no loss
        }

        [Test]
        public void Saving_A_Loaded_Legacy_Save_Produces_The_Versioned_Envelope()
        {
            string legacy = JsonUtility.ToJson(Representative());
            var loaded = SaveDataMigration.Load(legacy).Progress;

            string resaved = SaveDataMigration.Serialize(loaded);      // (14)
            var env = JsonUtility.FromJson<SaveData>(resaved);
            Assert.AreEqual(SaveData.CurrentVersion, env.version);
            Assert.IsNotNull(env.playerProgress);
        }

        // ---- Regression: legacy root must not be misread as an envelope ---------
        [Test]
        public void Legacy_Root_Save_With_Xp_Is_Not_Misread_As_A_Version_0_Envelope()
        {
            // JsonUtility ALWAYS instantiates SaveData.playerProgress (nested serializable
            // class), so envelope detection must key off the raw "playerProgress" field —
            // not a null check. Otherwise a legacy root save is misread as version-0 →
            // Corrupt → progress reset (the reported XP-loss bug).
            string legacyRoot =
                "{\"schemaVersion\":5,\"progression\":{\"xp\":{\"totalXp\":1340,\"xpSystemVersion\":1}}," +
                "\"activity\":{\"sessions\":{\"totalSessions\":12},\"streak\":{\"currentStreak\":4," +
                "\"longestStreak\":9,\"lastPlayedDate\":\"2026-08-16\"}}}";
            Assert.IsFalse(legacyRoot.Contains("playerProgress"), "sanity: not an envelope");

            var result = SaveDataMigration.Load(legacyRoot);
            Assert.AreEqual(SaveLoadStatus.Ok, result.Status, "legacy save must load, not go Corrupt");
            Assert.AreEqual(1340, result.Progress.progression.xp.totalXp, "XP preserved");
            Assert.AreEqual(12, result.Progress.activity.sessions.totalSessions, "sessions preserved");
        }

        [Test]
        public void Existing_Legacy_Save_File_On_Disk_Loads_With_Xp_Preserved()
        {
            // The exact reported scenario: an on-disk pre-envelope save must load via the
            // store with XP intact (not reset to a fresh default).
            File.WriteAllText(_path, "{\"schemaVersion\":5,\"progression\":{\"xp\":{\"totalXp\":1340}}}");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(1340, loaded.progression.xp.totalXp);
        }

        // ---- Migration (15–16) --------------------------------------------------
        [Test]
        public void Version_1_Passes_Through_Migration_Unchanged()
        {
            var original = Representative();
            var loaded = SaveDataMigration.Load(SaveDataMigration.Serialize(original)).Progress;
            AssertSameProgress(original, loaded); // (15) no unintended changes
        }

        [Test]
        public void Migration_Is_Deterministic()
        {
            string json = SaveDataMigration.Serialize(Representative());
            string a = SaveDataMigration.Serialize(SaveDataMigration.Load(json).Progress);
            string b = SaveDataMigration.Serialize(SaveDataMigration.Load(json).Progress);
            Assert.AreEqual(a, b); // (16) same input → same output
        }

        // ---- Invalid data (17–21) ----------------------------------------------
        [Test]
        public void Malformed_Json_Is_Corrupt_Not_A_Crash()
        {
            var result = SaveDataMigration.Load("{ not valid json ::");
            Assert.AreEqual(SaveLoadStatus.Corrupt, result.Status); // (17)
            Assert.IsNull(result.Progress);
        }

        [Test]
        public void Invalid_Envelope_Version_Is_Handled_Safely()
        {
            var result = SaveDataMigration.Load("{\"version\":0,\"playerProgress\":{\"schemaVersion\":5}}");
            Assert.AreEqual(SaveLoadStatus.Corrupt, result.Status); // (18)
        }

        [Test]
        public void Unsupported_Future_Envelope_Version_Is_Detected()
        {
            var result = SaveDataMigration.Load("{\"version\":999,\"playerProgress\":{\"schemaVersion\":5}}");
            Assert.AreEqual(SaveLoadStatus.UnsupportedFutureVersion, result.Status); // (19)
        }

        [Test]
        public void Future_Inner_Schema_In_Envelope_Is_Also_Detected()
        {
            var result = SaveDataMigration.Load("{\"version\":1,\"playerProgress\":{\"schemaVersion\":999}}");
            Assert.AreEqual(SaveLoadStatus.UnsupportedFutureVersion, result.Status);
        }

        [Test]
        public void Future_Version_Save_Is_Not_Overwritten_By_Load()
        {
            // (20) A future save on disk must survive a Load (which returns a default).
            string future = "{\"version\":999,\"playerProgress\":{\"schemaVersion\":5}}";
            File.WriteAllText(_path, future);

            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.IsNotNull(loaded, "load returns a safe default");
            Assert.AreEqual(future, File.ReadAllText(_path), "original future save left intact by load");
            Assert.IsTrue(File.Exists(_path + ".corrupt"), "preserved copy kept");
        }

        [Test]
        public void Migration_Failure_Does_Not_Silently_Reset_The_File()
        {
            // (21) A corrupt save is preserved (not overwritten) when load falls back to default.
            string corrupt = "{ totally broken ";
            File.WriteAllText(_path, corrupt);

            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(0, loaded.progression.xp.totalXp, "fresh default in memory");
            Assert.AreEqual(corrupt, File.ReadAllText(_path), "original file not reset by load");
            Assert.IsTrue(File.Exists(_path + ".corrupt"), "preserved for recovery");
        }

        // ---- Default state (22–23) ---------------------------------------------
        [Test]
        public void No_Save_Creates_Default_PlayerProgress()
        {
            var p = new LocalPlayerProgressStore(_path).Load(); // missing file
            Assert.AreEqual(0, p.progression.xp.totalXp);        // (22)
            Assert.AreEqual(0, p.activity.sessions.totalSessions);
            Assert.IsTrue(LevelProgressionService.IsUnlocked(p, PlayerLevel.RisingStar), "default seeded");
        }

        [Test]
        public void Default_Is_Not_Confused_With_A_Migrated_Save()
        {
            // (23) A migrated save carries data; a default does not.
            var migrated = SaveDataMigration.Load(SaveDataMigration.Serialize(Representative())).Progress;
            var fresh = new LocalPlayerProgressStore(_path).Load();
            Assert.AreNotEqual(fresh.progression.xp.totalXp, migrated.progression.xp.totalXp);
            Assert.AreEqual(0, fresh.progression.xp.totalXp);
            Assert.AreEqual(1340, migrated.progression.xp.totalXp);
        }

        // ---- Round trip (24–26) -------------------------------------------------
        [Test]
        public void Full_Progression_Survives_Save_And_Load_Round_Trip()
        {
            var original = Representative();
            var store = new LocalPlayerProgressStore(_path);
            store.Save(original);            // (24)
            var loaded = store.Load();       // (25)
            AssertSameProgress(original, loaded); // (26)
        }
    }
}
