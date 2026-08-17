using KanjiMaster.Core;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Data-driven description of one player-facing level. Adding a level means adding
    /// a <see cref="LevelDefinition"/> to <see cref="LevelCatalog"/> — no code branches
    /// on specific levels. Pure data (no logic); the unlock/completion RULES live in
    /// <see cref="LevelProgressionService"/>.
    /// </summary>
    public sealed class LevelDefinition
    {
        /// <summary>Internal level identity.</summary>
        public PlayerLevel Id { get; }

        /// <summary>Player-facing name, e.g. "Rising Star" (no JLPT terminology).</summary>
        public string DisplayName { get; }

        /// <summary>Which bundled dataset this level's questions are drawn from.</summary>
        public KanjiLevel Dataset { get; }

        /// <summary>The level that must be COMPLETED before this one unlocks.
        /// Null means "unlocked by default" (no prerequisite).</summary>
        public PlayerLevel? UnlockPrereq { get; }

        /// <summary>Ordering index in the progression (0 = entry level).</summary>
        public int Order { get; }

        /// <summary>Internal JLPT correspondence (metadata only — NOT a player-facing
        /// label). e.g. "N5". Kept for documentation / study reference.</summary>
        public string JlptEquivalent { get; }

        public LevelDefinition(PlayerLevel id, string displayName, KanjiLevel dataset,
            PlayerLevel? unlockPrereq, int order, string jlptEquivalent)
        {
            Id = id;
            DisplayName = displayName;
            Dataset = dataset;
            UnlockPrereq = unlockPrereq;
            Order = order;
            JlptEquivalent = jlptEquivalent;
        }
    }
}
