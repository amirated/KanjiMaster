using KanjiMaster.Core;

namespace KanjiMaster.Progression
{
    /// <summary>
    /// Facade the gameplay layer uses to update persistent mastery. Loads
    /// <see cref="PlayerProgress"/> once (cached, so it spans scenes) and persists
    /// after each change.
    ///
    /// This updates the PERSISTENT per-kanji mastery only — it is completely
    /// separate from the visible per-run game score, and it does NOT do question
    /// selection, XP, or mastery UI.
    /// </summary>
    public static class MasteryService
    {
        private static PlayerProgress _progress;

        /// <summary>Cached progress (lazily loaded). Settable for tests.</summary>
        public static PlayerProgress Progress
        {
            get => _progress ??= ProgressStore.Load();
            set => _progress = value;
        }

        /// <summary>Persistence hook — overridable in tests to avoid disk I/O.</summary>
        public static System.Action<PlayerProgress> Persist = p => ProgressStore.Save(p);

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
