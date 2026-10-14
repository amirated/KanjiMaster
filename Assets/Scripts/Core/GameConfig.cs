using System;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Independent settings that define a run. A bag of dimensions (not fixed
    /// "modes"), so more can be added later without redesign. AnswerMode, TimerMode
    /// and Level are orthogonal — the same game flow is configured by them.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public AnswerMode Answer = AnswerMode.English;
        public TimerMode Timer = TimerMode.Timed;
        public KanjiLevel Level = KanjiLevel.N5;

        public GameConfig Clone() => new GameConfig { Answer = Answer, Timer = Timer, Level = Level };

        public override string ToString() => $"{Answer} / {Timer} / {Level}";
    }
}
