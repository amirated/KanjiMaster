namespace KanjiMaster.Services
{
    /// <summary>
    /// Pure Boot routing decision for identity: an authenticated player goes straight to
    /// the Main Menu; anyone else (Unknown or Guest) is sent to the Identity scene to make
    /// the choice. Kept separate from the Boot MonoBehaviour so it is unit-testable without
    /// a scene. The guest continuation that follows the choice reuses
    /// <see cref="OnboardingRouter"/> — it is not duplicated here.
    /// </summary>
    public static class IdentityRouter
    {
        public static string StartDestination(bool isAuthenticated) =>
            isAuthenticated ? SceneNames.MainMenu : SceneNames.Identity;
    }
}
