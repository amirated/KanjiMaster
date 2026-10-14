namespace KanjiMaster.UI
{
    /// <summary>
    /// Pure paging state for the onboarding sequence — no Unity, no UI — so navigation
    /// (Next / Back / first-has-no-Back / last-is-CTA) is deterministic and unit-testable.
    /// The controller renders the current page and asks this for transitions.
    /// </summary>
    public class OnboardingFlow
    {
        private readonly int _count;

        public int Index { get; private set; }

        public OnboardingFlow(int pageCount)
        {
            _count = pageCount < 1 ? 1 : pageCount;
            Index = 0;
        }

        public int Count => _count;
        public bool IsFirst => Index == 0;
        public bool IsLast => Index == _count - 1;

        /// <summary>Advance to the next page. Returns true if it moved; false if already
        /// on the last page (the caller should then complete onboarding).</summary>
        public bool Next()
        {
            if (Index < _count - 1) { Index++; return true; }
            return false;
        }

        /// <summary>Go back one page. Returns true if it moved; false if already first.</summary>
        public bool Back()
        {
            if (Index > 0) { Index--; return true; }
            return false;
        }
    }
}
