namespace KanjiMaster.Progression
{
    /// <summary>
    /// The stable identity used to key kanji mastery: a kanji's Unicode code point.
    ///
    /// Why the code point (and not other options):
    ///   • The dataset's per-file <c>id</c> is just an index (0-based within each
    ///     file) and collides across levels (N5 id 0 ≠ N4 id 0) — not stable/global.
    ///   • The glyph string is disallowed as a key.
    /// The code point is globally unique per kanji, stable across dataset rebuilds
    /// and levels, and derivable from the character with no lookup (so migration
    /// from the old character-keyed saves is lossless).
    /// </summary>
    public static class KanjiId
    {
        /// <summary>Code point of the (first) character. Returns 0 for null/empty.</summary>
        public static int Of(string character)
        {
            if (string.IsNullOrEmpty(character)) return 0;
            return char.ConvertToUtf32(character, 0);
        }
    }
}
