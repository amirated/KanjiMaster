using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the persistence boundary and the nested PlayerProgress model:
    /// LocalPlayerProgressStore (JSON, migration, atomic write, corrupt recovery),
    /// PlayerProgressService (goes through the store), the default structure, and
    /// v1(flat) → v2(nested) migration.
    /// </summary>
    public class PersistenceTests
    {
        private string _path;

        [SetUp]
        public void MakeTempPath()
        {
            _path = Path.Combine(Application.persistentDataPath,
                "test_progress_" + System.Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void CleanTempFiles()
        {
            foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                if (File.Exists(_path + ext)) File.Delete(_path + ext);
        }

        // small helpers to read/write mastery by character in the nested model
        private static int Score(PlayerProgress p, string ch) => p.learning.kanjiMastery.GetScore(KanjiId.Of(ch));
        private static void Set(PlayerProgress p, string ch, int v) => p.learning.kanjiMastery.SetScore(KanjiId.Of(ch), v);

        // ---- Default structure --------------------------------------------------
        [Test]
        public void Default_Progress_Has_All_Sections_With_Sensible_Defaults()
        {
            var p = new LocalPlayerProgressStore(_path).Load(); // no file → default

            Assert.AreEqual(ProgressMigration.CurrentSchemaVersion, p.schemaVersion);
            Assert.IsNotNull(p.profile);
            Assert.IsNotNull(p.learning);
            Assert.IsNotNull(p.learning.kanjiMastery);
            Assert.AreEqual(0, p.learning.kanjiMastery.Count);
            Assert.IsNotNull(p.learning.levelProgress);
            Assert.AreEqual(PlayerLevel.Novice, p.learning.levelProgress.currentLevel);
            Assert.IsNotNull(p.activity);
            Assert.IsNotNull(p.activity.sessions);
            Assert.AreEqual(0, p.activity.sessions.totalSessions);
            Assert.IsNotNull(p.activity.streak);
            Assert.IsNotNull(p.progression);
            Assert.IsNotNull(p.progression.xp);
            Assert.AreEqual(0, p.progression.xp.totalXp);
        }

        [Test]
        public void Missing_File_Returns_Default_And_Exists_Is_False()
        {
            var store = new LocalPlayerProgressStore(_path);
            Assert.IsFalse(store.Exists());
            Assert.AreEqual(0, Score(store.Load(), "水"));
        }

        [Test]
        public void Save_Then_Load_Preserves_Mastery_And_Sessions()
        {
            var store = new LocalPlayerProgressStore(_path);
            var p = new PlayerProgress();
            p.activity.sessions.totalSessions = 3;
            Set(p, "水", 8);
            Set(p, "学", 1);
            store.Save(p);

            var loaded = store.Load();
            Assert.AreEqual(8, Score(loaded, "水"));
            Assert.AreEqual(1, Score(loaded, "学"));
            Assert.AreEqual(3, loaded.activity.sessions.totalSessions);
        }

        [Test]
        public void Saved_File_Is_V2_Nested_With_SchemaVersion()
        {
            new LocalPlayerProgressStore(_path).Save(new PlayerProgress());
            string json = File.ReadAllText(_path);
            StringAssert.Contains("schemaVersion", json);
            StringAssert.Contains("learning", json);
            StringAssert.Contains("kanjiMastery", json);
            Assert.AreEqual(2, JsonUtility.FromJson<PlayerProgress>(json).schemaVersion);
        }

        // ---- Migration: legacy flat → nested ------------------------------------
        [Test]
        public void Legacy_V0_Save_Without_SchemaVersion_Migrates_To_V2()
        {
            // Pre-version flat format (no schemaVersion).
            File.WriteAllText(_path,
                "{\"SessionsPlayed\":5,\"DailyStreak\":2,\"LastPlayedDate\":\"2026-08-15\",\"Xp\":40," +
                "\"Mastery\":[{\"kanji\":\"水\",\"score\":8},{\"kanji\":\"学\",\"score\":1}]}");

            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(2, loaded.schemaVersion, "migrated to v2");
            Assert.AreEqual(8, Score(loaded, "水"), "mastery kept, re-keyed by id");
            Assert.AreEqual(1, Score(loaded, "学"));
            Assert.AreEqual(5, loaded.activity.sessions.totalSessions, "sessions kept");
            Assert.AreEqual(2, loaded.activity.streak.currentStreak, "streak value kept");
            Assert.AreEqual("2026-08-15", loaded.activity.streak.lastPlayedDate, "last-played kept");
            Assert.AreEqual(40, loaded.progression.xp.totalXp, "xp kept");
            Assert.AreEqual(PlayerLevel.Novice, loaded.learning.levelProgress.currentLevel, "no legacy level → Novice");
        }

        [Test]
        public void Legacy_V1_Flat_Save_Migrates_To_V2_Nested()
        {
            File.WriteAllText(_path,
                "{\"schemaVersion\":1,\"SessionsPlayed\":7,\"DailyStreak\":0,\"LastPlayedDate\":\"\",\"Xp\":0," +
                "\"Mastery\":[{\"kanji\":\"日\",\"score\":24}]}");

            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(2, loaded.schemaVersion);
            Assert.AreEqual(24, Score(loaded, "日"));
            Assert.AreEqual(7, loaded.activity.sessions.totalSessions);
        }

        [Test]
        public void Migrated_Mastery_Is_Keyed_By_Codepoint_Not_Character()
        {
            File.WriteAllText(_path,
                "{\"schemaVersion\":1,\"Mastery\":[{\"kanji\":\"水\",\"score\":8}]}");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(1, loaded.learning.kanjiMastery.entries.Count);
            Assert.AreEqual(KanjiId.Of("水"), loaded.learning.kanjiMastery.entries[0].kanjiId);
            Assert.AreEqual(8, loaded.learning.kanjiMastery.entries[0].score);
        }

        // ---- Corrupt / invalid → safe fallback + preserved ----------------------
        [Test]
        public void Empty_File_Falls_Back_To_Default_And_Preserves_File()
        {
            File.WriteAllText(_path, "");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(0, Score(loaded, "水"));
            Assert.IsTrue(File.Exists(_path + ".corrupt"), "empty file preserved");
        }

        [Test]
        public void Invalid_Json_Falls_Back_To_Default_And_Preserves_File()
        {
            File.WriteAllText(_path, "{ this is not valid json ");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(0, Score(loaded, "水"));
            Assert.IsTrue(File.Exists(_path + ".corrupt"), "corrupt file preserved");
        }

        [Test]
        public void Future_SchemaVersion_Is_Not_Downgraded_And_Is_Preserved()
        {
            File.WriteAllText(_path,
                "{\"schemaVersion\":999,\"learning\":{\"kanjiMastery\":{\"entries\":[{\"kanjiId\":27700,\"score\":8}]}}}");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(0, Score(loaded, "水"), "did not read a save from a newer app");
            Assert.AreEqual(ProgressMigration.CurrentSchemaVersion, loaded.schemaVersion);
            Assert.IsTrue(File.Exists(_path + ".corrupt"));
        }

        // ---- Atomic save keeps a backup -----------------------------------------
        [Test]
        public void Overwriting_Save_Keeps_A_Backup_Of_The_Previous_Good_File()
        {
            var store = new LocalPlayerProgressStore(_path);
            var first = new PlayerProgress(); Set(first, "水", 8); store.Save(first);
            var second = new PlayerProgress(); Set(second, "水", 10); store.Save(second);

            Assert.IsTrue(File.Exists(_path), "final save present");
            Assert.IsTrue(File.Exists(_path + ".bak"), "previous save kept as .bak");
            Assert.AreEqual(10, Score(store.Load(), "水"), "final save is the latest");
            Assert.IsFalse(File.Exists(_path + ".tmp"), "no leftover temp file");
        }

        // ---- Service goes through the store, not the file -----------------------
        [Test]
        public void Service_Uses_Store_For_Load_And_Save()
        {
            var fake = new CountingStore();
            var service = new PlayerProgressService(fake);

            var current = service.Current;       // lazy load
            Assert.AreEqual(1, fake.LoadCount);
            Set(current, "水", 8);
            service.Save();
            Assert.AreEqual(1, fake.SaveCount);
            Assert.AreSame(current, fake.LastSaved);
        }

        [Test]
        public void MasteryService_Persists_Through_Service_Not_Disk()
        {
            var savedService = PlayerProgressService.Default;
            var savedPersist = MasteryService.Persist;
            try
            {
                var fake = new CountingStore();
                MasteryService.Service = new PlayerProgressService(fake);
                MasteryService.Persist = _ => MasteryService.Service.Save();

                int newScore = MasteryService.RecordAttempt("水", AnswerMode.Kana, TimerMode.Untimed, true);
                Assert.AreEqual(8, newScore);             // scoring unchanged
                Assert.GreaterOrEqual(fake.LoadCount, 1); // read via store
                Assert.GreaterOrEqual(fake.SaveCount, 1); // written via store (not the JSON file)
            }
            finally
            {
                PlayerProgressService.Default = savedService;
                MasteryService.Persist = savedPersist;
            }
        }

        /// <summary>In-memory store that counts calls — proves the service/mastery
        /// layer never touches the file directly.</summary>
        private class CountingStore : IPlayerProgressStore
        {
            public int LoadCount, SaveCount;
            public PlayerProgress LastSaved;
            private PlayerProgress _p = new PlayerProgress { schemaVersion = ProgressMigration.CurrentSchemaVersion };
            public bool Exists() => true;
            public PlayerProgress Load() { LoadCount++; return _p; }
            public void Save(PlayerProgress progress) { SaveCount++; LastSaved = progress; _p = progress; }
        }
    }
}
