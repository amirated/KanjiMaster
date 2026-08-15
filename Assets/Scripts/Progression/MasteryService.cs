using KanjiMaster.Core;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Facade the gameplay layer uses to update persistent mastery. Mastery SCORING
    /// is its responsibility; PERSISTENCE is delegated to <see cref="PlayerProgressService"/>
    /// (which sits over <see cref="IPlayerProgressStore"/>), so this class is no longer
    /// coupled to a concrete storage implementation.
    ///
    /// This updates the persistent per-kanji mastery only — it is completely separate
    /// from the visible per-run game score, and it does NOT do question selection,
    /// XP, or mastery UI.
    /// </summary>
    public static class MasteryService
    {
        /// <summary>The progress service used for persistence. Defaults to the shared
        /// local-JSON-backed instance; settable for tests/composition.</summary>
        public static PlayerProgressService Service
        {
            get => PlayerProgressService.Default;
            set => PlayerProgressService.Default = value;
        }

        /// <summary>Cached progress (lazily loaded via the service). Settable for tests.</summary>
        public static PlayerProgress Progress
        {
            get => Service.Current;
            set => Service.Current = value;
        }

        /// <summary>Persistence hook — overridable in tests to avoid disk I/O.
        /// Defaults to saving through the service (local JSON store).</summary>
        public static System.Action<PlayerProgress> Persist = _ => Service.Save();

        /// <summary>
        /// Record one answer attempt for a kanji and persist the new mastery score.
        /// Applies to normal AND revision runs — a revision answer is a real attempt.
        /// Returns the new mastery score.
        /// </summary>
        public static int RecordAttempt(string kanji, ScoringProfile profile, bool correct)
        {
            var progress = Progress;
            int current = progress.GetScore(kanji);
            int next = MasteryScoring.Apply(current, profile.CorrectPoints, profile.WrongPenalty, correct);
            progress.SetScore(kanji, next);
            Persist(progress);
            return next;
        }

        /// <summary>Convenience overload resolving the profile from the run's modes.</summary>
        public static int RecordAttempt(string kanji, AnswerMode answer, TimerMode timer, bool correct)
            => RecordAttempt(kanji, ScoringProfileResolver.Resolve(answer, timer), correct);

        /// <summary>Count a completed run (used later by the XP system).</summary>
        public static void RegisterSessionPlayed()
        {
            var progress = Progress;
            progress.SessionsPlayed++;
            Persist(progress);
        }

        public static int GetScore(string kanji) => Progress.GetScore(kanji);
    }
}
