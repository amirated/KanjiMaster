using System.Threading.Tasks;

namespace KanjiMaster.Auth
{
    /// <summary>
    /// A platform sign-in provider (Google Play Games on Android → B3, Game Center on iOS →
    /// B4). It acquires a platform credential; it is deliberately NOT Firebase-specific — the
    /// Firebase exchange happens in the Firebase layer. Implementations live in
    /// platform-constrained assemblies added in their respective phases.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>Canonical provider id (see <see cref="AuthProviders"/>).</summary>
        string ProviderId { get; }

        /// <summary>Run the platform sign-in and return an app-level credential (or a
        /// cancelled/failed/unavailable result). Must never throw for expected outcomes.</summary>
        Task<ProviderCredentialResult> AcquireCredentialAsync();
    }
}
