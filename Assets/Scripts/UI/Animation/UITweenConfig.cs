namespace KanjiMaster.UI
{
    /// <summary>
    /// Lightweight, central animation constants (durations, ease amounts, distances) so
    /// tween values are not scattered as magic numbers across UI code. Deliberately
    /// minimal and easy to replace — the full UI Theme system is a separate future task.
    /// These are sensible starting points to be tuned visually later; nothing here
    /// references LeanTween.
    /// </summary>
    public static class UITweenConfig
    {
        // Durations (seconds)
        public const float FadeDuration  = 0.25f;
        public const float ScaleDuration = 0.18f;
        public const float PopDuration   = 0.28f;
        public const float SlideDuration = 0.30f;

        // Button press feedback
        public const float PressDownDuration = 0.07f;
        public const float PressUpDuration   = 0.10f;
        public const float PressScale        = 0.92f;  // pressed size, relative to 1

        // Pop starts here and overshoots up to 1 (via an ease-out-back curve)
        public const float PopFromScale = 0.6f;

        // Convenience default for slide offsets (pixels, anchoredPosition space)
        public const float DefaultSlideDistance = 64f;
    }
}
