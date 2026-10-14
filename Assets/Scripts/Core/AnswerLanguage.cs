namespace KanjiMaster.Core
{
    /// <summary>
    /// How answer options are presented. Independent of <see cref="TimerMode"/>.
    ///   English → English meanings   (dataset: meanings[0])
    ///   Romaji  → romaji reading      (dataset: romaji)
    ///   Kana    → kana reading        (dataset: primary_reading)
    /// The question prompt is always the kanji glyph.
    ///
    /// NOTE: this file is named AnswerLanguage.cs for history; the type is AnswerMode.
    /// (File deletion isn't available in this environment — safe to rename the file.)
    /// </summary>
    public enum AnswerMode
    {
        English,
        Romaji,
        Kana,
    }
}
