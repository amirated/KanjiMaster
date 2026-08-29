using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using KanjiMaster.Auth;
using KanjiMaster.Progression;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Orchestration tests for the B2 authentication foundation. Real Firebase can't run in
    /// EditMode, so it is isolated behind <see cref="IFirebaseAuthenticator"/> and the platform
    /// provider behind <see cref="IAuthenticationProvider"/> — both faked here. Verifies the
    /// mapping outcome→IdentityService, that failures/cancellations leave identity
    /// unauthenticated, that "no provider" reports ProviderUnavailable (never a fake/anonymous
    /// success), and that the guest path never touches authentication.
    /// </summary>
    public class AuthenticationServiceTests
    {
        private string _idPath;
        private IdentityService _identity;

        [SetUp]
        public void SetUp()
        {
            _idPath = Path.Combine(Application.persistentDataPath, "authtest_" + Guid.NewGuid().ToString("N") + ".json");
            _identity = new IdentityService(new LocalPlayerIdentityStore(_idPath));
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var ext in new[] { "", ".tmp", ".bak", ".corrupt" })
                if (File.Exists(_idPath + ext)) File.Delete(_idPath + ext);
        }

        // ---- fakes --------------------------------------------------------------
        private sealed class FakeProvider : IAuthenticationProvider
        {
            private readonly ProviderCredentialResult _result;
            private readonly bool _throw;
            public FakeProvider(ProviderCredentialResult result, bool throwOnAcquire = false)
            { _result = result; _throw = throwOnAcquire; }
            public string ProviderId => AuthProviders.Google;
            public Task<ProviderCredentialResult> AcquireCredentialAsync()
            {
                if (_throw) throw new Exception("boom");
                return Task.FromResult(_result);
            }
        }

        private sealed class FakeFirebase : IFirebaseAuthenticator
        {
            private readonly FirebaseSignInResult _result;
            public FakeFirebase(FirebaseSignInResult result) { _result = result; }
            public Task<FirebaseSignInResult> SignInAsync(PlatformCredential credential) => Task.FromResult(_result);
        }

        private AuthOutcome Run(Func<IAuthenticationProvider> providerFactory, IFirebaseAuthenticator firebase)
        {
            var svc = new AuthenticationService(providerFactory, firebase, _identity);
            return svc.SignInWithPlatformAsync().GetAwaiter().GetResult(); // fakes complete synchronously
        }

        private static PlatformCredential GoogleCred() => new PlatformCredential(AuthProviders.Google, idToken: "tok");

        // ---- success ------------------------------------------------------------
        [Test]
        public void Success_Sets_Identity_Authenticated_With_Firebase_Uid()
        {
            var outcome = Run(
                () => new FakeProvider(ProviderCredentialResult.Acquired(GoogleCred())),
                new FakeFirebase(FirebaseSignInResult.Ok("firebase-uid-1")));

            Assert.AreEqual(AuthStatus.Success, outcome.Status);
            Assert.AreEqual("firebase-uid-1", outcome.FirebaseUid);
            Assert.IsTrue(_identity.IsAuthenticated);
            Assert.AreEqual("firebase-uid-1", _identity.Current.userId, "Firebase UID is the canonical id");
            Assert.AreEqual(AuthProviders.Google, _identity.Current.provider);
        }

        // ---- cancellation -------------------------------------------------------
        [Test]
        public void Provider_Cancelled_Leaves_Identity_Unauthenticated()
        {
            var outcome = Run(
                () => new FakeProvider(ProviderCredentialResult.Cancelled()),
                new FakeFirebase(FirebaseSignInResult.Ok("should-not-be-used")));

            Assert.AreEqual(AuthStatus.Cancelled, outcome.Status);
            Assert.IsFalse(_identity.IsAuthenticated);
            Assert.AreEqual(AuthState.Unknown, _identity.Current.state);
        }

        // ---- failures -----------------------------------------------------------
        [Test]
        public void Provider_Failure_Leaves_Identity_Unauthenticated()
        {
            var outcome = Run(
                () => new FakeProvider(ProviderCredentialResult.Failed("no network")),
                new FakeFirebase(FirebaseSignInResult.Ok("nope")));

            Assert.AreEqual(AuthStatus.Failed, outcome.Status);
            Assert.IsFalse(_identity.IsAuthenticated);
        }

        [Test]
        public void Provider_Throwing_Is_Reported_As_Failed_Not_A_Crash()
        {
            var outcome = Run(
                () => new FakeProvider(null, throwOnAcquire: true),
                new FakeFirebase(FirebaseSignInResult.Ok("nope")));

            Assert.AreEqual(AuthStatus.Failed, outcome.Status);
            Assert.IsFalse(_identity.IsAuthenticated);
        }

        [Test]
        public void Firebase_Failure_Leaves_Identity_Unauthenticated()
        {
            var outcome = Run(
                () => new FakeProvider(ProviderCredentialResult.Acquired(GoogleCred())),
                new FakeFirebase(FirebaseSignInResult.Fail("firebase down")));

            Assert.AreEqual(AuthStatus.Failed, outcome.Status);
            Assert.IsFalse(_identity.IsAuthenticated);
        }

        // ---- provider unavailable / no anonymous fallback ----------------------
        [Test]
        public void No_Provider_Reports_ProviderUnavailable_And_Never_Authenticates()
        {
            var outcome = Run(
                () => null, // no platform provider (the real B2 state)
                new FakeFirebase(FirebaseSignInResult.Ok("should-not-be-used")));

            Assert.AreEqual(AuthStatus.ProviderUnavailable, outcome.Status);
            Assert.IsFalse(_identity.IsAuthenticated, "no anonymous / fake sign-in");
            Assert.AreEqual(AuthState.Unknown, _identity.Current.state);
        }

        [Test]
        public void Real_Factory_Has_No_Provider_Yet()
        {
            // The SDK/foundation is in, but no platform sign-in provider exists yet (B3/B4),
            // so the real path reports ProviderUnavailable rather than a fake success.
            Assert.IsNull(AuthenticationProviderFactory.Create(), "no platform provider until B3/B4");
        }

        [Test]
        public void Firebase_Sdk_Is_Compiled_In()
        {
            // With Firebase installed and the FIREBASE_AUTH define set, the real backend is active.
            Assert.IsTrue(FirebaseInitializer.IsFirebaseCompiledIn);
        }

        // ---- guest isolation ----------------------------------------------------
        [Test]
        public void Guest_Flow_Never_Authenticates_And_Does_Not_Use_Firebase()
        {
            // The guest path is IdentityService.ContinueAsGuest — it must not authenticate,
            // and must never touch Firebase even though the SDK is now installed.
            _identity.ContinueAsGuest();

            Assert.AreEqual(AuthState.Guest, _identity.Current.state);
            Assert.IsFalse(_identity.IsAuthenticated);
            Assert.IsNull(_identity.Current.userId, "guest has no Firebase user id");
            Assert.IsNull(_identity.Current.provider, "guest has no auth provider");
        }
    }
}
