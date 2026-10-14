using System;

namespace KanjiMaster.Core
{
    /// <summary>
    /// The outcome of a single answered (or timed-out) question. Pure data — one
    /// attempt. It records only what happened; it is NOT responsible for scoring
    /// persistence, mastery, or XP (those belong to a future system).
    /// </summary>
    [Serializable]
    public class QuestionResult
    {
        public string KanjiCharacter;   // the prompt kanji
        public string CorrectAnswer;    // in the session's AnswerMode
        public string SelectedAnswer;   // what the player chose ("" if timed out)
        public bool IsCorrect;
        public bool TimedOut;
        public float ResponseTime;      // seconds from question shown to answered
    }
}
