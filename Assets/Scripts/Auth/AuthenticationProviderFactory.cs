namespace KanjiMaster.Auth
{
    /// <summary>
    /// Compile-time platform selection of the authentication provider, so a build only
    /// contains its own provider. Android → Google Play Games. iOS Game Center (B4) and web
    /// Google/Apple are added here later. On platforms without a provider this returns null,
    /// so <see cref="AuthenticationService"/> reports <see cref="AuthStatus.ProviderUnavailable"/>
    /// rather than pretending authentication succeeded.
    /// </summary>
    public static class AuthenticationProviderFactory
    {
        public static IAuthenticationProvider Create()
        {
#if UNITY_ANDROID
            return new AndroidPlayGamesAuthenticationProvider();
#elif UNITY_IOS
            // return new IosGameCenterAuthenticationProvider(); // B4
            return null;
#else
            return null; // Editor(non-Android) / WebGL / Standalone: no platform provider
#endif
        }
    }
}
