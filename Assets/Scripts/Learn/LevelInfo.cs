using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiMaster.Learn
{
    /// <summary>
    /// Maps internal JLPT datasets to the player-facing progression levels.
    /// The UI shows the player-facing name (Novice / Apprentice / …); N5/N4 stay
    /// internal metadata. Only Novice/Apprentice have content today.
    /// </summary>
    public static class LevelInfo
    {
        public static PlayerLevel ToPlayerLevel(KanjiLevel level) =>
            level == KanjiLevel.N4 ? PlayerLevel.Apprentice : PlayerLevel.Novice;

        /// <summary>Player-facing display name (no JLPT terminology).</summary>
        public static string DisplayName(PlayerLevel level) => level.ToString();
    }
}
