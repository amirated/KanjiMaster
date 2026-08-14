using System.Collections.Generic;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Derives a revision run from a completed session, without mutating it.
    /// A revision is forced Untimed, preserves the original AnswerMode and Level,
    /// and asks exactly the kanji that were answered incorrectly (no padding).
    /// </summary>
    public static class Revision
    {
        public static GameConfig ConfigFor(GameSession original)
        {
            var config = original.Config.Clone(); // preserves Answer + Level
            config.Timer = TimerMode.Untimed;     // revision is always untimed
            return config;
        }

        public static List<string> KanjiFor(GameSession original) => original.IncorrectKanji();
    }
}
