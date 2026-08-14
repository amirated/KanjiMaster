using System.Collections.Generic;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Cross-scene handoff (static data holder, no logic). MainMenu queues what the
    /// next run should be; GameController reads it and, when done, publishes the
    /// completed <see cref="LastSession"/>; Results reads that. Kept as a plain
    /// static to avoid singletons/asset wiring.
    /// </summary>
    public static class SessionContext
    {
        /// <summary>Config for the next Game load.</summary>
        public static GameConfig NextConfig = new GameConfig();

        /// <summary>Explicit kanji for the next run (revision). Null = normal random selection.</summary>
        public static IReadOnlyList<string> NextKanji;

        public static bool NextIsRevision;

        /// <summary>Remembers the last normal (non-revision) selection, for "Play Again".</summary>
        public static GameConfig LastNormalConfig = new GameConfig();

        /// <summary>The most recently completed run (normal or revision).</summary>
        public static GameSession LastSession;

        public static void QueueNormal(GameConfig config)
        {
            NextConfig = config.Clone();
            LastNormalConfig = config.Clone();
            NextKanji = null;
            NextIsRevision = false;
        }

        public static void QueueReplayNormal()
        {
            NextConfig = LastNormalConfig.Clone();
            NextKanji = null;
            NextIsRevision = false;
        }

        public static void QueueRevision(GameSession original)
        {
            NextConfig = Revision.ConfigFor(original);
            NextKanji = Revision.KanjiFor(original);
            NextIsRevision = true;
        }
    }
}
