using System;

namespace KanjiMaster.Progression
{
    /// <summary>Accumulated XP — DATA ONLY. No XP formula, awarding, or UI here; the
    /// XP economy is intentionally deferred. Extra XP breakdowns can be added later.</summary>
    [Serializable]
    public class XpData
    {
        public int totalXp;
    }

    /// <summary>Progression: currently just XP.</summary>
    [Serializable]
    public class ProgressionProgress
    {
        public XpData xp = new XpData();
    }
}
