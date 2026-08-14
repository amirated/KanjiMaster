namespace KanjiMaster.Kanji
{
    /// <summary>
    /// A ready-to-present multiple-choice question. The prompt is always the kanji
    /// glyph; options are English meanings, romaji, or kana readings depending on the
    /// selected AnswerMode (all four options share that one mode). Pure data.
    /// </summary>
    public class Question
    {
        public string Prompt;          // the kanji glyph to display
        public string KanjiCharacter;  // same glyph, kept for lookups/telemetry later
        public string[] Options;
        public int CorrectIndex;

        public string CorrectAnswer =>
            (Options != null && CorrectIndex >= 0 && CorrectIndex < Options.Length)
                ? Options[CorrectIndex]
                : string.Empty;
    }
}
