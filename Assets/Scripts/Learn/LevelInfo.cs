using KanjiMaster.Core;
using KanjiMaster.Progression;

namespace KanjiMaster.Learn
{
    /// <summary>
    /// Maps internal JLPT datasets to the player-facing progression levels and their
    /// display names. The UI shows the player-facing name (Rising Star / Young Master
    /// / …); N5/N4 stay internal metadata. Names come from <see cref="LevelCatalog"/>,
    /// so there is one source of truth for level labels.
    /// </summary>
    public static class LevelInfo
    {
        public static PlayerLevel ToPlayerLevel(KanjiLevel level) =>
            level == KanjiLevel.N4 ? PlayerLevel.YoungMaster : PlayerLevel.RisingStar;

        /// <summary>Player-facing display name (no JLPT terminology).</summary>
        public static string DisplayName(PlayerLevel level) => LevelCatalog.DisplayName(level);
    }
}
