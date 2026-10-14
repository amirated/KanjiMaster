namespace KanjiMaster.Progression
{
    /// <summary>
    /// Owns the first-time-onboarding completion state, which lives in
    /// <c>PlayerProgress.Profile.hasCompletedOnboarding</c> (no separate PlayerPrefs
    /// key, no second persistence system). Reads/writes only the profile flag — it does
    /// not touch mastery, XP, sessions, streak, or levels.
    ///
    /// Guest boundary: <see cref="CompleteAsGuest"/> is the local/guest completion path.
    /// A future Profile Setup step ("Continue as Guest" vs "Sign In") can wrap this
    /// without changing gameplay — see docs/ONBOARDING.md.
    /// </summary>
    public static class OnboardingService
    {
        public static bool IsComplete(PlayerProgress p) =>
            p?.profile != null && p.profile.hasCompletedOnboarding;

        public static void MarkComplete(PlayerProgress p)
        {
            if (p?.profile != null) p.profile.hasCompletedOnboarding = true;
        }

        /// <summary>Complete onboarding as a local/guest player and persist it. Order
        /// matters: the flag is set and saved BEFORE the caller navigates away.</summary>
        public static void CompleteAsGuest()
        {
            var service = PlayerProgressService.Default;
            MarkComplete(service.Current);
            service.Save();
        }

        /// <summary>
        /// Grandfather existing players past onboarding. Called on LOAD only (not on
        /// fresh-default creation), so a player who already has meaningful progress from
        /// before onboarding existed is never forced through it. Never un-sets the flag;
        /// only ever flips false → true. A brand-new player (no save) has no progress and
        /// keeps the default false, so they see onboarding.
        /// </summary>
        public static void GrandfatherIfExistingPlayer(PlayerProgress p)
        {
            if (p?.profile == null || p.profile.hasCompletedOnboarding) return;
            if (HasMeaningfulProgress(p)) p.profile.hasCompletedOnboarding = true;
        }

        /// <summary>Has the player actually engaged (played) already? Zero for a brand-new
        /// player; non-zero once any question has been answered or any session completed.</summary>
        public static bool HasMeaningfulProgress(PlayerProgress p)
        {
            if (p == null) return false;
            bool sessions = p.activity?.sessions != null && p.activity.sessions.totalSessions > 0;
            bool xp = p.progression?.xp != null && p.progression.xp.totalXp > 0;
            bool mastery = p.learning?.kanjiMastery != null && p.learning.kanjiMastery.Count > 0;
            return sessions || xp || mastery;
        }
    }
}
