using System;
using UnityEngine;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Configurable feedback durations (seconds). Wrong feedback stays visible
    /// longer so the player can read the correct answer. These are configuration,
    /// not hard-coded delays — exposed as a serialized field on the gameplay layer.
    /// </summary>
    [Serializable]
    public class FeedbackTiming
    {
        [Tooltip("How long 'Correct' feedback shows before the next question.")]
        public float correctDuration = 0.6f;   // unchanged from the previous value

        [Tooltip("How long 'Wrong → answer' feedback shows (≈1.5–2× the correct duration).")]
        public float wrongDuration = 1.1f;

        [Tooltip("How long feedback shows during REVISION — longer, so the player can read " +
                 "the meaning / romaji / kana. Normal games are unaffected.")]
        public float revisionDuration = 2.5f;

        /// <summary>Duration to hold feedback for, based on correctness. During revision a
        /// single, longer duration is used (there is more to read).</summary>
        public float For(bool isCorrect, bool isRevision = false) =>
            isRevision ? revisionDuration : (isCorrect ? correctDuration : wrongDuration);
    }
}
