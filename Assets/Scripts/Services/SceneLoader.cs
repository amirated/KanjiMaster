using UnityEngine.SceneManagement;

namespace KanjiMaster.Services
{
    /// <summary>
    /// Single choke point for scene transitions. Keeps SceneManager out of UI/
    /// gameplay scripts so navigation can later become async, add loading screens,
    /// or fire analytics from one place.
    /// </summary>
    public static class SceneLoader
    {
        public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);

        public static void GoToMainMenu() => Load(SceneNames.MainMenu);
        public static void GoToGame() => Load(SceneNames.Game);
        public static void GoToResults() => Load(SceneNames.Results);
        public static void GoToLearn() => Load(SceneNames.Learn);
    }
}
