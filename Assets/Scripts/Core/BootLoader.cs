using KanjiMaster.Services;
using UnityEngine;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Entry point. Sits in the Boot scene and forwards to the Main Menu. This is
    /// the future home for one-time startup (loading the kanji database, restoring
    /// settings, initializing services) before the player sees any UI.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            // TODO (later): warm up KanjiDatabase, load saved settings/services here.
            SceneLoader.GoToMainMenu();
        }
    }
}
