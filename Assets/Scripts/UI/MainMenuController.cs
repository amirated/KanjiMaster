using KanjiMaster.Core;
using KanjiMaster.Progression;
using KanjiMaster.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Main Menu presentation. The Play flow is Level → Answer mode → Timer → Start:
    /// the player picks one level (Rising Star / Young Master), one AnswerMode
    /// (English / Romaji / Kana) and one TimerMode (Timed / Untimed); Play queues a
    /// normal run into <see cref="SessionContext"/> and loads the Game scene. No
    /// gameplay logic here.
    ///
    /// Wire the two level toggles into one ToggleGroup, the three answer toggles into
    /// another, and the two timer toggles into a third (Allow Switch Off = off) in the
    /// editor. A locked level's toggle is made non-interactable and a message explains
    /// how to unlock it. See docs/PROGRESSION.md ("Editor wiring").
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Level (one ToggleGroup)")]
        [SerializeField] private Toggle risingStarToggle;
        [SerializeField] private Toggle youngMasterToggle;
        [SerializeField] private TMP_Text lockMessageText; // unlock requirement for a locked level

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

        [Header("Navigation")]
        [SerializeField] private Button learnButton; // opens the Learn section (Play stays the default)

        // Working copy; seeded from the last normal selection so the menu remembers it.
        private GameConfig _config;
        private PlayerLevel _selectedLevel = PlayerLevel.RisingStar;

        private void Start()
        {
            _config = SessionContext.LastNormalConfig.Clone();

            // Answer + timer toggles
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

            SetupLevelSelection();

            if (playButton) playButton.onClick.AddListener(OnPlay);
            if (settingsButton) settingsButton.onClick.AddListener(OnSettings);
            if (learnButton) learnButton.onClick.AddListener(SceneLoader.GoToLearn);
        }

        /// <summary>Evaluate progression (latch any unlock earned last session), reflect
        /// unlocked/locked state in the toggles, and wire level selection into the run.</summary>
        private void SetupLevelSelection()
        {
            LevelProgressionService.EvaluateAndPersist();
            var progress = PlayerProgressService.Default.Current;

            // Seed the selection from the remembered level; never start on a locked one.
            _selectedLevel = progress.learning.levelProgress.currentLevel;
            if (!LevelProgressionService.IsUnlocked(progress, _selectedLevel))
                _selectedLevel = LevelCatalog.Default.Id;
            ApplyLevelToConfig(_selectedLevel);

            bool youngUnlocked = LevelProgressionService.IsUnlocked(progress, PlayerLevel.YoungMaster);

            if (risingStarToggle)
                risingStarToggle.SetIsOnWithoutNotify(_selectedLevel == PlayerLevel.RisingStar);
            if (youngMasterToggle)
            {
                youngMasterToggle.interactable = youngUnlocked; // locked → cannot be chosen
                youngMasterToggle.SetIsOnWithoutNotify(_selectedLevel == PlayerLevel.YoungMaster);
            }

            if (risingStarToggle)
                risingStarToggle.onValueChanged.AddListener(on => { if (on) SelectLevel(PlayerLevel.RisingStar); });
            if (youngMasterToggle)
                youngMasterToggle.onValueChanged.AddListener(on => { if (on) SelectLevel(PlayerLevel.YoungMaster); });

            UpdateLockMessage(youngUnlocked);
        }

        private void SelectLevel(PlayerLevel level)
        {
            _selectedLevel = level;
            ApplyLevelToConfig(level);
        }

        private void ApplyLevelToConfig(PlayerLevel level)
        {
            var def = LevelCatalog.Get(level);
            if (def != null) _config.Level = def.Dataset; // level → dataset → existing selection
        }

        private void UpdateLockMessage(bool youngUnlocked)
        {
            if (!lockMessageText) return;
            // Show the requirement while Young Master is still locked; hide once earned.
            bool show = !youngUnlocked;
            lockMessageText.gameObject.SetActive(show);
            if (show) lockMessageText.text = LevelProgressionService.RequirementText(PlayerLevel.YoungMaster);
        }

        private void OnPlay()
        {
            // Defensive: never launch a level that is locked OR has no content. Fall back
            // to the default entry level (Rising Star), which is always unlocked + populated.
            var progress = PlayerProgressService.Default.Current;
            if (!LevelProgressionService.IsPlayable(progress, _selectedLevel))
                _selectedLevel = LevelCatalog.Default.Id;
            ApplyLevelToConfig(_selectedLevel);

            // Remember the selected level for next time.
            progress.learning.levelProgress.currentLevel = _selectedLevel;
            PlayerProgressService.Default.Save();

            SessionContext.QueueNormal(_config);
            SceneLoader.GoToGame();
        }

        private void OnSettings() => Debug.Log("Settings pressed (not implemented yet).");
    }
}
