namespace KanjiMaster.Services
{
    /// <summary>
    /// Pure Boot routing decision: where a player should go on launch based on whether
    /// onboarding is complete. Kept separate from the Boot MonoBehaviour so the decision
    /// is unit-testable without a scene. Returns a canonical <see cref="SceneNames"/>.
    /// </summary>
    public static class OnboardingRouter
    {
        public static string StartDestination(bool onboardingComplete) =>
            onboardingComplete ? SceneNames.MainMenu : SceneNames.Onboarding;
    }
}
