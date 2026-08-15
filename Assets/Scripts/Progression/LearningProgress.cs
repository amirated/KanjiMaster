using System;
using System.Collections.Generic;

namespace KanjiMaster.Progression
{
    /// <summary>One kanji's persistent mastery, keyed by its stable code-point id.</summary>
    [Serializable]
    public class KanjiMasteryEntry
    {
        public int kanjiId;   // Unicode code point — see KanjiId
        public int score;
    }

    /// <summary>
    /// Per-kanji mastery scores. Serialized as a list (JsonUtility can't do
    /// dictionaries); a runtime int→score index is rebuilt lazily for O(1) access.
    /// Data only — the scoring RULES live in MasteryScoring, not here.
    /// </summary>
    [Serializable]
    public class KanjiMastery
    {
        public List<KanjiMasteryEntry> entries = new List<KanjiMasteryEntry>();

        [NonSerialized] private Dictionary<int, int> _index;

        private void EnsureIndex()
        {
            if (_index != null) return;
            _index = new Dictionary<int, int>();
            foreach (var e in entries)
                if (e != null) _index[e.kanjiId] = e.score;
        }

        /// <summary>Current mastery for a kanji id; 0 if never seen.</summary>
        public int GetScore(int kanjiId)
        {
            EnsureIndex();
            return _index.TryGetValue(kanjiId, out var s) ? s : 0;
        }

        public void SetScore(int kanjiId, int score)
        {
            EnsureIndex();
            _index[kanjiId] = score;
            var entry = entries.Find(e => e.kanjiId == kanjiId);
            if (entry != null) entry.score = score;
            else entries.Add(new KanjiMasteryEntry { kanjiId = kanjiId, score = score });
        }

        public int Count => entries.Count;

        /// <summary>Force the index to rebuild (e.g. after JSON deserialization).</summary>
        public void InvalidateIndex() => _index = null;
    }

    /// <summary>
    /// The player's progression through the user-facing levels. Minimal for now:
    /// just the current level. The advancement formula is intentionally deferred, so
    /// this only needs a stable place to store the current level. Legacy saves had
    /// no level data, so migrated players default to Novice.
    /// </summary>
    [Serializable]
    public class LevelProgress
    {
        public PlayerLevel currentLevel = PlayerLevel.Novice;
    }

    /// <summary>Learning-related progress: mastery + level progression.</summary>
    [Serializable]
    public class LearningProgress
    {
        public KanjiMastery kanjiMastery = new KanjiMastery();
        public LevelProgress levelProgress = new LevelProgress();
    }
}
