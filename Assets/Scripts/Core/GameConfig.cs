using System;

namespace KanjiMaster.Core
{
    /// <summary>
    /// The set of options that define a game session. Deliberately a bag of
    /// independent settings (not four fixed "modes"), so new dimensions —
    /// difficulty, JLPT level, question count, deck, etc. — can be added later
    /// as fields without redesigning anything that reads GameConfig.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public AnswerLanguage AnswerLanguage = AnswerLanguage.English;
        public TimerMode TimerMode = TimerMode.Timed;

        // Future dimensions go here, e.g.:
        // public JlptLevel Level = JlptLevel.N5;
        // public int QuestionsPerRun = 15;

        public GameConfig Clone()
        {
            return new GameConfig
            {
                AnswerLanguage = AnswerLanguage,
                TimerMode = TimerMode,
            };
        }

        public override string ToString() => $"{AnswerLanguage} / {TimerMode}";
    }
}
