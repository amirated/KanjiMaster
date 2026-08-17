using System.Collections.Generic;
using KanjiMaster.Core;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// The ordered, data-driven list of available levels. This is the SINGLE place
    /// that knows which levels exist, their player-facing names, their dataset, and
    /// their unlock prerequisite. Extend the game by appending a definition — the
    /// progression service, question selection and UI all pick it up with no other
    /// changes and no per-level branching.
    ///
    /// V1 ships two levels with content (Rising Star = N5, Young Master = N4). The
    /// remaining <see cref="PlayerLevel"/> values exist in the enum but are
    /// intentionally NOT defined here yet (Adept/Expert/Master/Legend are future work).
    /// </summary>
    public static class LevelCatalog
    {
        public static readonly IReadOnlyList<LevelDefinition> All = new List<LevelDefinition>
        {
            //                 id                        display         dataset        unlock prereq             order jlpt
            new LevelDefinition(PlayerLevel.RisingStar,  "Rising Star",  KanjiLevel.N5, null,                     0,    "N5"),
            new LevelDefinition(PlayerLevel.YoungMaster, "Young Master", KanjiLevel.N4, PlayerLevel.RisingStar,   1,    "N4"),
            // Future (Adept/Expert/Master/Legend = N3/N2/N1/Beyond) are added here with
            // their dataset + prerequisite once content exists — no other code changes.
        };

        /// <summary>The default/entry level (first defined — always unlocked).</summary>
        public static LevelDefinition Default => All.Count > 0 ? All[0] : null;

        public static LevelDefinition Get(PlayerLevel id)
        {
            foreach (var d in All) if (d.Id == id) return d;
            return null;
        }

        public static bool IsDefined(PlayerLevel id) => Get(id) != null;

        /// <summary>Player-facing name; falls back to the enum name for levels that
        /// have no definition yet (future tiers).</summary>
        public static string DisplayName(PlayerLevel id) => Get(id)?.DisplayName ?? id.ToString();
    }
}
