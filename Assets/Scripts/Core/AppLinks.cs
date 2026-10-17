using UnityEngine;

namespace KanjiMaster.Core
{
    /// <summary>
    /// External links used by the app. Supply real URLs here.
    ///
    /// >>> ACTION REQUIRED: replace <see cref="PrivacyPolicyUrl"/> with the real privacy policy
    ///     URL. Until it starts with "http", the Settings privacy row is treated as not-yet-
    ///     configured and does nothing (it logs a warning) instead of opening a bogus page.
    /// </summary>
    public static class AppLinks
    {
        // Currently points at the support page (doubles as the privacy/support destination).
        // Swap in a dedicated privacy policy URL here later if you add one.
        public const string PrivacyPolicyUrl = "https://loen.in/kanji-kashikoi-support.html";

        /// <summary>True when the URL looks like a real, openable web link.</summary>
        public static bool HasPrivacyPolicyUrl =>
            !string.IsNullOrEmpty(PrivacyPolicyUrl) &&
            PrivacyPolicyUrl.StartsWith("http", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Open the privacy policy in the device browser. Safe no-op (logs) if unset.</summary>
        public static void OpenPrivacyPolicy()
        {
            if (!HasPrivacyPolicyUrl)
            {
                Debug.LogWarning("[AppLinks] Privacy policy URL is not configured (AppLinks.PrivacyPolicyUrl).");
                return;
            }
            Application.OpenURL(PrivacyPolicyUrl);
        }
    }
}
