using System;

namespace KanjiMaster.Progression
{
    /// <summary>Accumulated Global XP — DATA ONLY. The XP rules (awarding, multipliers,
    /// bonuses, rounding) live in XpConfig/XpCalculator/XpService, not here. <c>totalXp</c>
    /// is never negative. <c>xpSystemVersion</c> records which XP ruleset produced this
    /// data so future rule changes can be migrated without corrupting existing progress
    /// (see XpMigration). It is separate from Kanji mastery and level progression.</summary>
    [Serializable]
    public class XpData
    {
        public int totalXp;
        public int xpSystemVersion = XpConfig.CurrentVersion;
    }

    /// <summary>Progression: currently just XP.</summary>
    [Serializable]
    public class ProgressionProgress
    {
        public XpData xp = new XpData();
    }
}
