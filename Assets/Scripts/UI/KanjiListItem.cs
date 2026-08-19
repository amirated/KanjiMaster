using System;
using KanjiMaster.Learn;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// One row in the Learn kanji list. Put this on an authored list-item PREFAB
    /// (a Button with child texts) and assign the slots in the Inspector. Shows the
    /// kanji plus its three answer-mode values: English meaning, Romaji, and Kana.
    /// LearnController instantiates one per kanji and calls <see cref="Bind"/>.
    /// </summary>
    public class KanjiListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text characterText;
        [FormerlySerializedAs("meaningText")]
        [SerializeField] private TMP_Text englishText;  // English meaning
        [SerializeField] private TMP_Text romajiText;   // romaji reading
        [SerializeField] private TMP_Text kanaText;     // kana reading
        [SerializeField] private Button button;

        public void Bind(LearnKanji item, int index, Action<int> onSelect)
        {
            var k = item.Kanji;
            if (characterText) characterText.text = k.Character;
            if (englishText) englishText.text = k.PrimaryMeaning;
            if (romajiText) romajiText.text = k.Romaji;
            if (kanaText) kanaText.text = k.PrimaryReading;
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelect?.Invoke(index));
            }
        }
    }
}
