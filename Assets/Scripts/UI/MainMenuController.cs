using KanjiMaster.Core;
using KanjiMaster.Services;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Presentation for the Main Menu. Lets the player pick answer language and
    /// timer mode, then starts a game. It only reads/writes <see cref="GameSession.Config"/>
    /// and triggers navigation — no gameplay logic lives here.
    ///
    /// Wire the four toggles into two ToggleGroups (one per dimension) in the editor.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Answer language")]
        [SerializeField] private Toggle englishToggle;
        [SerializeField] private Toggle japaneseToggle;

        [Header("Timer")]
        [SerializeField] private Toggle timedToggle;
        [SerializeField] private Toggle untimedToggle;

        [Header("Actions")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;

        private void Start()
        {
            var config = GameSession.Config;

            // Reflect current config in the toggles without firing listeners yet.
            if (englishToggle) englishToggle.SetIsOnWithoutNotify(config.AnswerLanguage == AnswerLanguage.English);
            if (japaneseToggle) japaneseToggle.SetIsOnWithoutNotify(config.AnswerLanguage == AnswerLanguage.Japanese);
            if (timedToggle) timedToggle.SetIsOnWithoutNotify(config.TimerMode == TimerMode.Timed);
            if (untimedToggle) untimedToggle.SetIsOnWithoutNotify(config.TimerMode == TimerMode.Untimed);

            if (englishToggle) englishToggle.onValueChanged.AddListener(on => { if (on) SetLanguage(AnswerLanguage.English); });
            if (japaneseToggle) japaneseToggle.onValueChanged.AddListener(on => { if (on) SetLanguage(AnswerLanguage.Japanese); });
            if (timedToggle) timedToggle.onValueChanged.AddListener(on => { if (on) SetTimer(TimerMode.Timed); });
            if (untimedToggle) untimedToggle.onValueChanged.AddListener(on => { if (on) SetTimer(TimerMode.Untimed); });

            if (playButton) playButton.onClick.AddListener(OnPlay);
            if (settingsButton) settingsButton.onClick.AddListener(OnSettings);
        }

        private static void SetLanguage(AnswerLanguage language) => GameSession.Config.AnswerLanguage = language;
        private static void SetTimer(TimerMode mode) => GameSession.Config.TimerMode = mode;

        private void OnPlay() => SceneLoader.GoToGame();

        private void OnSettings()
        {
            // Placeholder — settings screen comes later.
            Debug.Log("Settings pressed (not implemented yet).");
        }
    }
}
