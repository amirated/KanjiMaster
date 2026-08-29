namespace KanjiMaster.Auth
{
    /// <summary>
    /// Compile-time platform selection of the authentication provider, so a build only
    /// contains its own provider (Android → Play Games in B3; iOS → Game Center in B4). Each
    /// provider will live in a platform-constrained assembly behind <c>#if UNITY_ANDROID</c> /
    /// <c>#if UNITY_IOS</c>. Until a platform's provider exists this returns null, so
    /// <see cref="AuthenticationService"/> reports <see cref="AuthStatus.ProviderUnavailable"/>
    /// rather than pretending authentication succeeded.
    /// </summary>
    public static class AuthenticationProviderFactory
    {
        public static IAuthenticationProvider Create()
        {
            // Later phases add the platform providers here, e.g.:
            //   #if UNITY_ANDROID
            //       return new AndroidPlayGamesAuthenticationProvider(); // B3
            //   #elif UNITY_IOS
            //       return new IosGameCenterAuthenticationProvider();     // B4
            //   #endif
            // No provider is implemented in B2:
            return null;
        }
    }
}
