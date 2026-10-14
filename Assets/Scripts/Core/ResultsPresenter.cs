namespace KanjiMaster.Core
{
    /// <summary>What the Results screen should present for a given completed run.
    /// Pure data so the decision is unit-testable without a scene.</summary>
    public readonly struct ResultsView
    {
        public bool IsRevision { get; }
        /// <summary>Normal numerical results (score / correct / incorrect / % …). False for revision.</summary>
        public bool ShowNumericResults { get; }
        /// <summary>The Revise button — only for a NORMAL run that has mistakes.</summary>
        public bool ShowReviseButton { get; }
        public bool ShowPlayAgain { get; }
        public bool ShowMainMenu { get; }
        /// <summary>Title/headline text: "Well Done!" for revision, otherwise "Results".</summary>
        public string Title { get; }

        public ResultsView(bool isRevision, bool showNumericResults, bool showReviseButton, string title)
        {
            IsRevision = isRevision;
            ShowNumericResults = showNumericResults;
            ShowReviseButton = showReviseButton;
            ShowPlayAgain = true;  // both flows always offer Play Again
            ShowMainMenu = true;   // and Main Menu
            Title = title;
        }
    }

    /// <summary>
    /// Decides how Results are presented for a completed run. Normal runs show the
    /// numerical summary (+ Revise when there are mistakes); a revision run shows ONLY
    /// "Well Done!" with Play Again / Main Menu — no numbers and no Revise (so there is
    /// no Revision → Revise → Revision recursion). No UI or Unity dependencies.
    /// </summary>
    public static class ResultsPresenter
    {
        public const string WellDoneMessage = "Well Done!";
        public const string NormalTitle = "Results";

        public static ResultsView For(GameSession session)
        {
            bool isRevision = session != null && session.IsRevision;
            return new ResultsView(
                isRevision: isRevision,
                showNumericResults: !isRevision,
                showReviseButton: !isRevision && session != null && session.HasMistakes,
                title: isRevision ? WellDoneMessage : NormalTitle);
        }
    }
}
