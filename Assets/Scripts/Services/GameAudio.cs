using UnityEngine;

namespace KanjiMaster.Services
{
    /// <summary>
    /// Holds the game's sound-effect clips, assigned in the Inspector. <see cref="AudioService"/>
    /// loads the single instance from a Resources folder at runtime, so no scene wiring is needed.
    ///
    /// SETUP (you do this in the editor): Create ▸ KanjiMaster ▸ Game Audio, save the asset as
    /// <c>Assets/Resources/GameAudio.asset</c> (the name must be "GameAudio"), then drop your
    /// clips into the three slots. Until the asset/clips exist, SFX simply don't play (no errors).
    /// </summary>
    [CreateAssetMenu(fileName = "GameAudio", menuName = "KanjiMaster/Game Audio")]
    public class GameAudio : ScriptableObject
    {
        [Tooltip("Played on a button press (buttons with ButtonPressAnimator).")]
        public AudioClip buttonClick;

        [Tooltip("Played when the player answers correctly.")]
        public AudioClip answerCorrect;

        [Tooltip("Played when the player answers wrong or times out.")]
        public AudioClip answerWrong;

        [Range(0f, 1f)] public float buttonClickVolume = 1f;
        [Range(0f, 1f)] public float answerVolume = 1f;
    }
}
