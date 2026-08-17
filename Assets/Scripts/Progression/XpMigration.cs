namespace KanjiMaster.Progression
{
    /// <summary>
    /// The XP-system compatibility layer. It brings a loaded XP block up to the current
    /// XP ruleset version WITHOUT ever changing earned <c>totalXp</c>. V1 has no
    /// predecessor, so this only stamps a missing/zero version to the current one.
    ///
    /// This is the single, documented place a FUTURE version bump adds its upgrade:
    ///   XPSystemVersion → (here) → current XP model.
    /// Deliberately minimal — no speculative framework.
    /// </summary>
    public static class XpMigration
    {
        public static void Ensure(PlayerProgress p)
        {
            var xp = p?.progression?.xp;
            if (xp == null) return;

            // Legacy/absent version → treat as current (no total change).
            if (xp.xpSystemVersion < 1) xp.xpSystemVersion = XpConfig.CurrentVersion;

            // Invariant: TotalXP is never negative (only ever added). Repair a corrupt /
            // hand-edited value rather than propagating it. Does not touch a valid total.
            if (xp.totalXp < 0) xp.totalXp = 0;

            // Future: when XpConfig.CurrentVersion > 1, upgrade older rulesets here, e.g.
            //   if (xp.xpSystemVersion == 1) { /* apply v1→v2 changes */ xp.xpSystemVersion = 2; }
            // Never rewrite existing totalXp except by an explicit, documented rule.
        }
    }
}
