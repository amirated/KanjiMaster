using System.IO;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the persistence boundary: LocalPlayerProgressStore (JSON, migration,
    /// atomic write, corrupt recovery), PlayerProgressService (goes through the
    /// store), and that MasteryService persists via the service rather than the file.
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

        // ---- Store: load / default / missing ------------------------------------
        [Test]
        public void Missing_File_Returns_Default_And_Exists_Is_False()
        {
            var store = new LocalPlayerProgressStore(_path);
            Assert.IsFalse(store.Exists());
            var p = store.Load();
            Assert.IsNotNull(p);
            Assert.AreEqual(ProgressMigration.CurrentSchemaVersion, p.schemaVersion);
            Assert.AreEqual(0, p.GetScore("水"));
            Assert.AreEqual(0, p.SessionsPlayed);
        }

        [Test]
        public void Save_Then_Load_Preserves_Mastery_And_Sessions()
        {
            var store = new LocalPlayerProgressStore(_path);
            var p = new PlayerProgress { SessionsPlayed = 3 };
            p.SetScore("水", 8);
            p.SetScore("学", 1);
            store.Save(p);

            var loaded = store.Load();
            Assert.AreEqual(8, loaded.GetScore("水"));
            Assert.AreEqual(1, loaded.GetScore("学"));
            Assert.AreEqual(3, loaded.SessionsPlayed);
        }

        [Test]
        public void Saved_File_Contains_SchemaVersion()
        {
            var store = new LocalPlayerProgressStore(_path);
            store.Save(new PlayerProgress());
            string json = File.ReadAllText(_path);
            StringAssert.Contains("schemaVersion", json);
            Assert.AreEqual(1, JsonUtility.FromJson<PlayerProgress>(json).schemaVersion);
        }

        // ---- Migration: legacy save (no schemaVersion) --------------------------
        [Test]
        public void Legacy_Save_Without_SchemaVersion_Migrates_To_1_And_Keeps_Data()
        {
            // Written in the pre-version format (no schemaVersion field).
            File.WriteAllText(_path,
                "{\"SessionsPlayed\":5,\"DailyStreak\":0,\"LastPlayedDate\":\"\",\"Xp\":0," +
                "\"Mastery\":[{\"kanji\":\"水\",\"score\":8},{\"kanji\":\"学\",\"score\":1}]}");

            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(1, loaded.schemaVersion, "legacy → schemaVersion 1");
            Assert.AreEqual(5, loaded.SessionsPlayed, "legacy sessions kept");
            Assert.AreEqual(8, loaded.GetScore("水"), "legacy mastery kept");
            Assert.AreEqual(1, loaded.GetScore("学"));
        }

        [Test]
        public void SchemaVersion_1_Loads_Without_Migration()
        {
            var store = new LocalPlayerProgressStore(_path);
            var p = new PlayerProgress();
            p.SetScore("水", 8);
            store.Save(p); // stamped to version 1
            var loaded = store.Load();
            Assert.AreEqual(1, loaded.schemaVersion);
            Assert.AreEqual(8, loaded.GetScore("水"));
        }

        // ---- Corrupt / invalid → safe fallback + preserved ----------------------
        [Test]
        public void Empty_File_Falls_Back_To_Default_And_Preserves_File()
        {
            File.WriteAllText(_path, "");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(0, loaded.GetScore("水"));
            Assert.IsTrue(File.Exists(_path + ".corrupt"), "empty file preserved for inspection");
        }

        [Test]
        public void Invalid_Json_Falls_Back_To_Default_And_Preserves_File()
        {
            File.WriteAllText(_path, "{ this is not valid json ");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.GetScore("水"));
            Assert.IsTrue(File.Exists(_path + ".corrupt"), "corrupt file preserved");
        }

        [Test]
        public void Future_SchemaVersion_Is_Not_Downgraded_And_Is_Preserved()
        {
            File.WriteAllText(_path,
                "{\"schemaVersion\":999,\"Mastery\":[{\"kanji\":\"水\",\"score\":8}]}");
            var loaded = new LocalPlayerProgressStore(_path).Load();
            Assert.AreEqual(0, loaded.GetScore("水"), "did not read a save from a newer app");
            Assert.AreEqual(ProgressMigration.CurrentSchemaVersion, loaded.schemaVersion);
            Assert.IsTrue(File.Exists(_path + ".corrupt"));
        }

        // ---- Atomic save keeps a backup -----------------------------------------
        [Test]
        public void Overwriting_Save_Keeps_A_Backup_Of_The_Previous_Good_File()
        {
            var store = new LocalPlayerProgressStore(_path);
            var first = new PlayerProgress(); first.SetScore("水", 8); store.Save(first);
            var second = new PlayerProgress(); second.SetScore("水", 10); store.Save(second);

            Assert.IsTrue(File.Exists(_path), "final save present");
            Assert.IsTrue(File.Exists(_path + ".bak"), "previous save kept as .bak");
            Assert.AreEqual(10, store.Load().GetScore("水"), "final save is the latest");
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
            current.SetScore("水", 8);
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
                Assert.AreEqual(8, newScore);            // scoring unchanged
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
