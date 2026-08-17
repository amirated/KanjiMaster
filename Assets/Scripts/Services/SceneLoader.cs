using UnityEngine.SceneManagement;

namespace KanjiMaster.Services
{
    /// <summary>
    /// Single choke point for scene transitions. Keeps SceneManager out of UI/
    /// gameplay scripts so navigation can later become async, add loading screens,
    /// or fire analytics from one place.
    ///
    /// It also guards against re-entrancy: once a transition starts, further Load calls
    /// are ignored until the new scene has loaded. This means rapid/duplicate button
    /// taps (Play, Play Again, Main Menu, Revise) can only trigger ONE transition — no
    /// double scene loads, no double-queued runs.
    /// </summary>
    public static class SceneLoader
    {
        private static bool _loading;

        public static void Load(string sceneName)
        {
            if (_loading) return;                       // ignore duplicate/rapid transitions
            _loading = true;
            SceneManager.sceneLoaded += OnSceneLoaded;  // reset the guard once the load completes
            SceneManager.LoadScene(sceneName);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _loading = false;
        }

        public static void GoToOnboarding() => Load(SceneNames.Onboarding);
        public static void GoToMainMenu() => Load(SceneNames.MainMenu);
        public static void GoToGame() => Load(SceneNames.Game);
        public static void GoToResults() => Load(SceneNames.Results);
        public static void GoToLearn() => Load(SceneNames.Learn);
    }
}
