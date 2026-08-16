using System;
using KanjiMaster.Learn;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// One row in the Learn kanji list. Put this on an authored list-item PREFAB
    /// (a Button with child texts) and assign the slots in the Inspector.
    /// LearnController instantiates one per kanji and calls <see cref="Bind"/>.
    /// </summary>
    public class KanjiListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text characterText;
        [SerializeField] private TMP_Text meaningText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button button;

        public void Bind(LearnKanji item, int index, Action<int> onSelect)
        {
            if (characterText) characterText.text = item.Kanji.Character;
            if (meaningText) meaningText.text = item.Kanji.PrimaryMeaning;
            if (levelText) levelText.text = LevelInfo.DisplayName(item.PlayerLevel);
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelect?.Invoke(index));
            }
        }
    }
}
