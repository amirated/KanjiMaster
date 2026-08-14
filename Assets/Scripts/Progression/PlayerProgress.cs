using System;
using System.Collections.Generic;

namespace KanjiMaster.Progression
{
    /// <summary>One kanji's persistent mastery score.</summary>
    [Serializable]
    public class KanjiMasteryEntry
    {
        public string kanji;
        public int score;
    }

    /// <summary>
    /// The persistent player progression model — the foundation a future global XP
    /// system will extend. It holds per-kanji mastery plus placeholders for the
    /// eventual XP inputs (sessions played, daily streak, XP). Only mastery and
    /// SessionsPlayed are populated in this task; DailyStreak/Xp are reserved and
    /// intentionally left uncomputed (no XP formula, no streak logic yet).
    ///
    /// Serialized as JSON (a list, since JsonUtility can't serialize dictionaries);
    /// a runtime dictionary index is rebuilt lazily for O(1) access.
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        public int SessionsPlayed;

        // --- reserved for the future XP system (not calculated in this task) ---
        public int DailyStreak;
        public string LastPlayedDate;  // yyyy-MM-dd, for future streak logic
        public int Xp;

        public List<KanjiMasteryEntry> Mastery = new List<KanjiMasteryEntry>();

        [NonSerialized] private Dictionary<string, int> _index;

        private void EnsureIndex()
        {
            if (_index != null) return;
            _index = new Dictionary<string, int>();
            foreach (var e in Mastery)
                if (e != null && e.kanji != null) _index[e.kanji] = e.score;
        }

        /// <summary>Current mastery for a kanji; 0 if never seen.</summary>
        public int GetScore(string kanji)
        {
            EnsureIndex();
            return _index.TryGetValue(kanji, out var s) ? s : 0;
        }

        public void SetScore(string kanji, int score)
        {
            EnsureIndex();
            _index[kanji] = score;
            var entry = Mastery.Find(e => e.kanji == kanji);
            if (entry != null) entry.score = score;
            else Mastery.Add(new KanjiMasteryEntry { kanji = kanji, score = score });
        }

        /// <summary>Force the lazy index to rebuild (e.g. after JSON deserialization).</summary>
        public void InvalidateIndex() => _index = null;
    }
}
