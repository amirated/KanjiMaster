using KanjiMaster.Auth;
using KanjiMaster.Core;
using KanjiMaster.Progression;
using KanjiMaster.Services;
using KanjiMaster.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Settings screen (v1): Sound effects, Haptics, Account &amp; sync status, Sign out /
    /// switch account (with confirmation), Privacy policy, and App version. AUTHORED scene —
    /// build the layout in Settings.unity and assign the slots in the Inspector; this script only
    /// wires behaviour. All refs are optional/null-guarded. See SettingsSceneBuilder for a
    /// one-shot scaffold.
    ///
    /// Honesty note: the game has no backend sync yet, so this never claims "Synced". It reports
    /// the real identity state and that progress is on-device.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        [Header("Toggles")]
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle hapticsToggle;

        [Header("Account & sync")]
        [SerializeField] private TMP_Text accountStatusText;
        [SerializeField] private TMP_Text syncStatusText;

        [Header("Sign out / switch account")]
        [SerializeField] private Button signOutButton;
        [SerializeField] private TMP_Text signOutLabel;

        [Header("Confirmation dialog")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text confirmMessage;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("Privacy & version")]
        [SerializeField] private Button privacyButton;
        [SerializeField] private TMP_Text versionText;

        [Header("Navigation")]
        [SerializeField] private Button backButton; // → Main Menu

        private bool _busy;

        private void Start()
        {
            var settings = SettingsService.Default;

            // Toggles reflect persisted values; SetIsOnWithoutNotify avoids a feedback loop.
            if (soundToggle)
            {
                soundToggle.SetIsOnWithoutNotify(settings.SoundEffectsEnabled);
                soundToggle.onValueChanged.AddListener(v => settings.SoundEffectsEnabled = v);
            }
            if (hapticsToggle)
            {
                hapticsToggle.SetIsOnWithoutNotify(settings.HapticsEnabled);
                hapticsToggle.onValueChanged.AddListener(OnHapticsToggled);
            }

            if (versionText) versionText.text = $"Version {Application.version}";

            if (privacyButton)
            {
                privacyButton.onClick.AddListener(AppLinks.OpenPrivacyPolicy);
                // Still visibly actionable; it logs a warning if the URL isn't configured yet.
            }

            if (backButton) backButton.onClick.AddListener(SceneLoader.GoToMainMenu);

            if (signOutButton) signOutButton.onClick.AddListener(OnSignOutPressed);
            if (confirmNoButton) confirmNoButton.onClick.AddListener(HideConfirm);
            if (confirmYesButton) confirmYesButton.onClick.AddListener(DoSignOut);
            HideConfirm();

            RefreshAccountStatus();
        }

        private void OnHapticsToggled(bool on)
        {
            SettingsService.Default.HapticsEnabled = on;
            if (on) HapticService.Light(); // let the player feel it when turning it on
        }

        // ---- account & sync status (honest; no fake "Synced") -------------------

        private void RefreshAccountStatus()
        {
            var id = IdentityService.Default.Current;

            string account, sync, button;
            switch (id.state)
            {
                case AuthState.Authenticated:
                    account = $"Signed in · {ProviderName(id.provider)}";
                    sync = "Cloud backup isn't available yet — your progress is saved on this device.";
                    button = "Sign out";
                    break;
                case AuthState.Guest:
                    account = "Playing as Guest";
                    sync = "Local only — progress is saved on this device.";
                    button = "Sign in";
                    break;
                default: // Unknown / not signed in
                    account = "Not signed in";
                    sync = "Local only — progress is saved on this device.";
                    button = "Sign in";
                    break;
            }

            if (accountStatusText) accountStatusText.text = account;
            if (syncStatusText) syncStatusText.text = sync;
            if (signOutLabel) signOutLabel.text = button;
        }

        private static string ProviderName(string provider)
        {
            if (provider == AuthProviders.GooglePlayGames) return "Google Play Games";
            if (provider == AuthProviders.GameCenter) return "Game Center";
            if (provider == AuthProviders.Google) return "Google";
            if (provider == AuthProviders.Apple) return "Apple";
            return "account";
        }

        // ---- sign out / switch account (with confirmation) ----------------------

        private void OnSignOutPressed()
        {
            if (_busy) return;

            if (IdentityService.Default.Current.state == AuthState.Authenticated)
            {
                // Signing out IS a state change → confirm. (Android keeps the device-level Play
                // Games session; only the in-app/Firebase link is dropped.)
                if (confirmMessage)
                    confirmMessage.text = "Sign out? Your progress stays on this device. Note: you " +
                        "may remain signed in to Google Play Games at the system level.";
                ShowConfirm();
            }
            else
            {
                // Guest / not signed in → just go choose an account. Non-destructive, no confirm.
                GoToIdentity();
            }
        }

        private void DoSignOut()
        {
            // Firebase sign-out (if initialized) + reset local identity to Unknown. Progress is
            // preserved. Then return to the Identity screen to continue as Guest or re-link.
            AuthenticationService.Default.SignOut();
            GoToIdentity();
        }

        private void GoToIdentity()
        {
            if (_busy) return;
            _busy = true;
            HideConfirm();
            SceneLoader.GoToIdentity();
        }

        private void ShowConfirm() { if (confirmPanel) confirmPanel.SetActive(true); }
        private void HideConfirm() { if (confirmPanel) confirmPanel.SetActive(false); }
    }
}
