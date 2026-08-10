namespace KanjiMaster.Core
{
    /// <summary>
    /// Lightweight cross-scene state holder. MainMenu writes <see cref="Config"/>;
    /// the gameplay layer reads it and writes <see cref="LastResult"/>; the Results
    /// screen reads that. A plain static keeps this dependency-free and avoids
    /// singletons/DontDestroyOnLoad wiring — it is a data holder only, never logic.
    ///
    /// (If this later needs to be swapped for a ScriptableObject or DI container for
    /// testability, only these accessors change — callers stay the same.)
    /// </summary>
    public static class GameSession
    {
        public static GameConfig Config { get; set; } = new GameConfig();
        public static GameResult LastResult { get; set; } = new GameResult();

        public static void Reset()
        {
            Config = new GameConfig();
            LastResult = new GameResult();
        }
    }
}
