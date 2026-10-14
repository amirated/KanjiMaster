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
    /// Persisted state for one level. <c>unlocked</c> and <c>completed</c> are LATCHED
    /// (only ever set true) so a later mastery dip can never re-lock a level. The
    /// progress percentage is derived live from mastery (see LevelProgressionService),
    /// so it is intentionally NOT stored here and can never go stale.
    /// </summary>
    [Serializable]
    public class LevelState
    {
        public PlayerLevel level;
        public bool unlocked;
        public bool completed;
    }

    /// <summary>
    /// The player's progression through the player-facing levels: the last selected
    /// level plus a per-level unlocked/completed state. Data only — the unlock/
    /// completion RULES live in <see cref="LevelProgressionService"/>. Legacy saves
    /// have an empty level list; the service seeds sensible defaults on load (the
    /// entry level, Rising Star, unlocked).
    /// </summary>
    [Serializable]
    public class LevelProgress
    {
        /// <summary>The level the player last selected (remembered across sessions).</summary>
        public PlayerLevel currentLevel = PlayerLevel.RisingStar;

        /// <summary>Per-level unlocked/completed flags (seeded by the service).</summary>
        public List<LevelState> levels = new List<LevelState>();

        public LevelState Find(PlayerLevel level)
        {
            foreach (var s in levels) if (s != null && s.level == level) return s;
            return null;
        }

        public LevelState GetOrAdd(PlayerLevel level)
        {
            var s = Find(level);
            if (s == null) { s = new LevelState { level = level }; levels.Add(s); }
            return s;
        }
    }

    /// <summary>Learning-related progress: mastery + level progression.</summary>
    [Serializable]
    public class LearningProgress
    {
        public KanjiMastery kanjiMastery = new KanjiMastery();
        public LevelProgress levelProgress = new LevelProgress();
    }
}
