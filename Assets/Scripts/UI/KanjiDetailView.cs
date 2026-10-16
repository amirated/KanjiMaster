using System.Collections.Generic;
using KanjiMaster.Learn;
using KanjiMaster.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Data = KanjiRush.Data;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Reusable, data-driven presentation of a single kanji. It is an AUTHORED view:
    /// you build the layout in the scene/prefab and assign the text/image slots in the
    /// Inspector; this component only fills them from a <see cref="Data.Kanji"/>.
    ///
    /// It renders whatever the kanji provides and shows "—" for missing fields (never
    /// broken UI), hardcodes no kanji, and owns no navigation. One component, many
    /// hosts — Learn uses it now; the future Revision flow reuses the SAME component/
    /// prefab by calling Show(...). Presentation differences should be handled by
    /// parameters (e.g. passing no level hides the level chip), not a second copy.
    /// </summary>
    public class KanjiDetailView : MonoBehaviour
    {
        [Header("Text slots")]
        [SerializeField] private TMP_Text levelChip;      // player-facing level (optional)
        [SerializeField] private TMP_Text characterText;  // large kanji glyph
        [SerializeField] private TMP_Text meaningText;    // English meaning(s)
        [SerializeField] private TMP_Text romajiText;     // romaji reading
        [SerializeField] private TMP_Text kanaText;       // kana reading
        [SerializeField] private TMP_Text onyomiText;
        [SerializeField] private TMP_Text kunyomiText;
        [SerializeField] private TMP_Text strokesText;

        [Header("Illustration slot")]
        [SerializeField] private Image illustrationImage;
        [SerializeField] private GameObject illustrationPlaceholder; // shown when no illustration

        /// <summary>Render a kanji. <paramref name="level"/> is optional — pass null
        /// (e.g. from Revision) to hide the level chip.</summary>
        public void Show(Data.Kanji kanji, IKanjiIllustrationProvider illustrations, PlayerLevel? level = null)
        {
            if (levelChip)
            {
                levelChip.gameObject.SetActive(level.HasValue);
                if (level.HasValue) levelChip.text = LevelInfo.DisplayName(level.Value);
            }

            Set(characterText, kanji != null ? kanji.Character : "—"); // the big glyph stays unlabelled
            Set(meaningText, Labeled("Meaning", Join(kanji?.Meanings, ", ")));
            Set(romajiText, Labeled("Romaji", Value(kanji?.Romaji)));
            Set(kanaText, Labeled("Kana", Value(kanji?.PrimaryReading)));
            Set(onyomiText, Labeled("Onyomi", Join(kanji?.Onyomi, "、")));
            Set(kunyomiText, Labeled("Kunyomi", Join(kanji?.Kunyomi, "、")));
            Set(strokesText, Labeled("Number of strokes",
                (kanji != null && kanji.Strokes > 0) ? kanji.Strokes.ToString() : "—"));

            Sprite sprite = (kanji != null && illustrations != null)
                ? illustrations.Get(KanjiId.Of(kanji.Character))
                : null;
            bool has = sprite != null;
            if (illustrationImage)
            {
                illustrationImage.sprite = sprite;
                illustrationImage.preserveAspect = true;
                illustrationImage.enabled = has;
            }
            if (illustrationPlaceholder) illustrationPlaceholder.SetActive(!has);
        }

        private static void Set(TMP_Text field, string value)
        {
            if (field) field.text = value;
        }

        // A small, muted caption on its own line above the value. Uses TMP rich text with a
        // RELATIVE size so it adapts to whatever font size each value slot is styled at, and
        // the value keeps the slot's own colour/style. Requires Rich Text enabled (TMP default).
        private const string LabelColorHex = "#9198A5";
        private static string Labeled(string label, string value)
            => $"<size=65%><color={LabelColorHex}>{label}</color></size>\n<size=115%>{value}</size>";

        private static string Value(string s) => string.IsNullOrEmpty(s) ? "—" : s;

        private static string Join(IReadOnlyList<string> values, string sep)
        {
            if (values == null || values.Count == 0) return "—"; // graceful for missing fields
            return string.Join(sep, values);
        }
    }
}
