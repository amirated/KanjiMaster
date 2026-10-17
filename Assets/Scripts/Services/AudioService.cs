using KanjiMaster.Settings;
using UnityEngine;

namespace KanjiMaster.Services
{
    /// <summary>
    /// Minimal sound-effects facade. The game currently plays NO sound effects, so this is the
    /// single routing point future SFX should go through: call <see cref="PlaySfx"/> instead of
    /// using AudioSource directly, and the Sound-effects setting is honoured automatically.
    ///
    /// It deliberately does NOT touch music — if/when background music is added it should use its
    /// own path so the SFX toggle never silences music.
    /// </summary>
    public static class AudioService
    {
        private const string LibraryResource = "GameAudio"; // Assets/Resources/GameAudio.asset
        private static AudioSource _sfxSource;
        private static GameAudio _library;
        private static bool _libraryLoaded;

        /// <summary>Whether SFX are enabled (the persisted player preference).</summary>
        public static bool SfxEnabled => SettingsService.Default.SoundEffectsEnabled;

        /// <summary>The clip library (null until you create Assets/Resources/GameAudio.asset).</summary>
        private static GameAudio Library
        {
            get
            {
                if (!_libraryLoaded) { _library = Resources.Load<GameAudio>(LibraryResource); _libraryLoaded = true; }
                return _library;
            }
        }

        // ---- semantic SFX (what gameplay/UI call) -------------------------------
        public static void PlayButtonClick()
            => PlaySfx(Library ? Library.buttonClick : null, Library ? Library.buttonClickVolume : 1f);

        public static void PlayAnswerCorrect()
            => PlaySfx(Library ? Library.answerCorrect : null, Library ? Library.answerVolume : 1f);

        public static void PlayAnswerWrong()
            => PlaySfx(Library ? Library.answerWrong : null, Library ? Library.answerVolume : 1f);

        /// <summary>Play a one-shot sound effect, unless SFX are disabled in Settings.</summary>
        public static void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null || !SfxEnabled) return;
            EnsureSource().PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private static AudioSource EnsureSource()
        {
            if (_sfxSource != null) return _sfxSource;
            var go = new GameObject("~SfxAudioSource");
            Object.DontDestroyOnLoad(go);
            _sfxSource = go.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f; // 2D UI sfx
            return _sfxSource;
        }
    }
}
