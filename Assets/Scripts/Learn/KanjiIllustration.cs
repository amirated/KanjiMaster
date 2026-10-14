using UnityEngine;

namespace KanjiMaster.Learn
{
    /// <summary>
    /// Loosely-coupled illustration lookup: given a kanji's stable id (Unicode code
    /// point, same key as mastery), return its illustration sprite, or null if none
    /// is available yet. Keeping this behind an interface means the temporary
    /// placeholder can be swapped for the final generated assets (Resources,
    /// Addressables, or a bundle) without touching the views.
    /// </summary>
    public interface IKanjiIllustrationProvider
    {
        /// <summary>Illustration for a kanji id, or null if unavailable.</summary>
        Sprite Get(int kanjiId);
    }

    /// <summary>No illustrations available — always null. The view renders a
    /// placeholder. Useful until the illustration pipeline ships assets.</summary>
    public class PlaceholderIllustrationProvider : IKanjiIllustrationProvider
    {
        public Sprite Get(int kanjiId) => null;
    }

    /// <summary>
    /// Loads <c>Resources/Illustrations/&lt;kanjiId&gt;</c> when present (the final
    /// pipeline can drop generated sprites there, named by code point, or this can be
    /// replaced by an Addressables-backed provider). Returns null when absent, so the
    /// view falls back to the placeholder. No illustrations exist yet.
    /// </summary>
    public class ResourcesKanjiIllustrationProvider : IKanjiIllustrationProvider
    {
        public const string Folder = "Illustrations/";
        public Sprite Get(int kanjiId) => Resources.Load<Sprite>(Folder + kanjiId);
    }
}
