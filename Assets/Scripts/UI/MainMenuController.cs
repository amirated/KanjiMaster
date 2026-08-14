using KanjiMaster.Core;
using KanjiMaster.Services;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Main Menu presentation. Player picks exactly one AnswerMode (English / Romaji
    /// / Kana) and one TimerMode (Timed / Untimed); Play queues a normal run into
    /// <see cref="SessionContext"/> and loads the Game scene. No gameplay logic here.
    ///
    /// Wire the three answer toggles into one ToggleGroup and the two timer toggles
    /// into another (Allow Switch Off = off) in the editor.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Answer mode (one ToggleGroup)")]
        [SerializeField] private Toggle englishToggle;
        [SerializeField] private Toggle romajiToggle;
        [FormerlySerializedAs("japaneseToggle")]
        [SerializeField] private Toggle kanaToggle;

        [Header("Timer (one ToggleGroup)")]
        [SerializeField] private Toggle timedToggle;
        [SerializeField] private Toggle untimedToggle;

        [Header("Actions")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;

        // Working copy; seeded from the last normal selection so the menu remembers it.
        private GameConfig _config;

        private void Start()
        {
            _config = SessionContext.LastNormalConfig.Clone();

            if (englishToggle) englishToggle.SetIsOnWithoutNotify(_config.Answer == AnswerMode.English);
            if (romajiToggle) romajiToggle.SetIsOnWithoutNotify(_config.Answer == AnswerMode.Romaji);
            if (kanaToggle) kanaToggle.SetIsOnWithoutNotify(_config.Answer == AnswerMode.Kana);
            if (timedToggle) timedToggle.SetIsOnWithoutNotify(_config.Timer == TimerMode.Timed);
            if (untimedToggle) untimedToggle.SetIsOnWithoutNotify(_config.Timer == TimerMode.Untimed);

            if (englishToggle) englishToggle.onValueChanged.AddListener(on => { if (on) _config.Answer = AnswerMode.English; });
            if (romajiToggle) romajiToggle.onValueChanged.AddListener(on => { if (on) _config.Answer = AnswerMode.Romaji; });
            if (kanaToggle) kanaToggle.onValueChanged.AddListener(on => { if (on) _config.Answer = AnswerMode.Kana; });
            if (timedToggle) timedToggle.onValueChanged.AddListener(on => { if (on) _config.Timer = TimerMode.Timed; });
            if (untimedToggle) untimedToggle.onValueChanged.AddListener(on => { if (on) _config.Timer = TimerMode.Untimed; });

            if (playButton) playButton.onClick.AddListener(OnPlay);
            if (settingsButton) settingsButton.onClick.AddListener(OnSettings);
        }

        private void OnPlay()
        {
            SessionContext.QueueNormal(_config); // Level stays at its default (N5)
            SceneLoader.GoToGame();
        }

        private void OnSettings() => Debug.Log("Settings pressed (not implemented yet).");
    }
}
