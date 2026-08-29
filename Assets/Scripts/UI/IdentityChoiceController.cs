using KanjiMaster.Auth;
using KanjiMaster.Progression;
using KanjiMaster.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// The Identity scene controller: "Continue with Account" vs "Continue as Guest".
    /// AUTHORED scene — build the layout in Identity.unity and assign the slots in the
    /// Inspector; this script only wires the two actions (see docs/IDENTITY.md).
    ///
    /// Guest works fully. Account is intentionally an isolated stub in Phase A: it does NOT
    /// authenticate, does NOT navigate, and does NOT fall back to guest — a later task
    /// connects a real provider and, on success, calls
    /// <see cref="IdentityService.SetAuthenticated"/> then <see cref="ContinueAfterIdentity"/>.
    /// </summary>
    public class IdentityChoiceController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button accountButton;
        [SerializeField] private Button guestButton;

        [Header("Copy (optional — assigned text is filled from the constants below)")]
        [SerializeField] private TMP_Text accountLabel;
        [SerializeField] private TMP_Text accountSupportingText;
        [SerializeField] private TMP_Text guestLabel;
        [SerializeField] private TMP_Text guestSupportingText;
        [SerializeField] private TMP_Text messageText; // optional: error/status line

        // Player-facing copy kept in one place; visual styling/placement is done in-scene.
        private const string AccountLabelText = "Continue with Account";
        private const string AccountSupport = "Keep your progress safe and continue across devices.";
        private const string GuestLabelText = "Continue as Guest";
        private const string GuestSupport =
            "Your progress is stored on this device and may be lost if you uninstall the game " +
            "or move to another device.";

        private bool _navigating;      // guards double taps (SceneLoader also guards transitions)
        private bool _authInProgress;  // guards re-entrant account sign-in

        private void Start()
        {
            Set(accountLabel, AccountLabelText);
            Set(accountSupportingText, AccountSupport);
            Set(guestLabel, GuestLabelText);
            Set(guestSupportingText, GuestSupport);
            Set(messageText, string.Empty);

            if (accountButton) accountButton.onClick.AddListener(OnContinueWithAccount);
            if (guestButton) guestButton.onClick.AddListener(OnContinueAsGuest);
        }

        private void OnContinueAsGuest()
        {
            if (_navigating) return;
            _navigating = true;

            // Sets state = Guest (+ a stable guestId on first time) and persists it.
            // Does NOT touch PlayerProgress — existing local progress is retained as-is.
            IdentityService.Default.ContinueAsGuest();
            ContinueAfterIdentity();
        }

        private async void OnContinueWithAccount()
        {
            if (_navigating || _authInProgress) return;
            _authInProgress = true;
            if (accountButton) accountButton.interactable = false;
            Set(messageText, string.Empty);

            AuthOutcome outcome;
            try { outcome = await AuthenticationService.Default.SignInWithPlatformAsync(); }
            catch { outcome = AuthOutcome.Failed("Sign-in failed. Please try again."); }

            switch (outcome.Status)
            {
                case AuthStatus.Success:
                    // Identity is now Authenticated (Firebase UID recorded); route as normal.
                    _navigating = true;
                    ContinueAfterIdentity();
                    return; // scene changing — do not re-enable UI

                case AuthStatus.Cancelled:
                    break; // stay on the Identity screen, no error

                case AuthStatus.ProviderUnavailable:
                    Set(messageText, "Account sign-in isn't available on this platform yet.");
                    break;

                default: // Failed
                    Set(messageText, string.IsNullOrEmpty(outcome.Message)
                        ? "Sign-in failed. Please try again."
                        : outcome.Message);
                    break;
            }

            _authInProgress = false;
            if (accountButton) accountButton.interactable = true;
        }

        /// <summary>Post-identity routing, shared by Guest now and the future auth-success
        /// path: onboarding if it is not yet complete, otherwise the Main Menu. Reuses the
        /// existing onboarding logic rather than duplicating it.</summary>
        private void ContinueAfterIdentity()
        {
            var progress = PlayerProgressService.Default.Current;
            string destination = OnboardingRouter.StartDestination(OnboardingService.IsComplete(progress));
            SceneLoader.Load(destination);
        }

        private static void Set(TMP_Text field, string value)
        {
            if (field) field.text = value;
        }
    }
}
