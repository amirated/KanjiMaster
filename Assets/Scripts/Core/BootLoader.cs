using System.Collections;
using KanjiMaster.Progression;
using KanjiMaster.Services;
using TMPro;
using UnityEngine;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Entry point. Sits in the Boot scene and routes to the right first screen. The IDENTITY
    /// gate decides the destination:
    ///
    ///   authenticated            → Main Menu
    ///   guest / unknown          → Identity scene (choose Account or Guest)
    ///
    /// After the Identity scene, the Guest continuation reuses the existing onboarding check
    /// (Onboarding if incomplete, else Main Menu). Existing installs have no identity file yet,
    /// so they default to Unknown and see the Identity scene once. See docs/IDENTITY.md.
    ///
    /// UI: the splash is AUTHORED in Boot.unity (a Canvas with a logo, title and status line).
    /// Assign the slots below in the Inspector — you can restyle the scene freely without
    /// touching this script. Nothing here is required: if a slot is left empty it is simply
    /// skipped, so the scene still routes correctly. Build or rebuild the starter UI any time
    /// with the menu item: Tools ▸ KanjiMaster ▸ UI ▸ Build Boot Scene UI.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        [Header("Splash UI (assigned by the Boot scene — all optional)")]
        [Tooltip("The whole splash, used for the fade-in. Leave empty to skip fading.")]
        [SerializeField] private CanvasGroup splashGroup;
        [Tooltip("Status line updated while booting (e.g. \"Loading…\" → \"Starting…\").")]
        [SerializeField] private TMP_Text statusLabel;

        [Header("Timing")]
        [Tooltip("Minimum seconds the splash stays on screen so it never flashes past.")]
        [SerializeField, Min(0f)] private float minimumSplashSeconds = 5f;
        [Tooltip("Fade-in duration for the splash. 0 = appear instantly.")]
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.25f;

        [Header("Status copy")]
        [SerializeField] private string loadingText = "Loading…";
        [SerializeField] private string startingText = "Starting…";

        private void Start() => StartCoroutine(BootRoutine());

        private IEnumerator BootRoutine()
        {
            // Unscaled time so the splash timing is unaffected by any Time.timeScale changes.
            float startTime = Time.unscaledTime;

            if (statusLabel) statusLabel.text = loadingText;

            // Fade the splash in (optional, only if a CanvasGroup is wired).
            if (splashGroup && fadeInSeconds > 0f)
            {
                splashGroup.alpha = 0f;
                float t = 0f;
                while (t < fadeInSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    splashGroup.alpha = Mathf.Clamp01(t / fadeInSeconds);
                    yield return null;
                }
            }
            if (splashGroup) splashGroup.alpha = 1f;

            // Decide where to go. (Future warm-up work — database, settings — can await here.)
            string destination = IdentityRouter.StartDestination(IdentityService.Default.IsAuthenticated);

            // Hold the splash for its minimum display time before transitioning.
            float remaining = minimumSplashSeconds - (Time.unscaledTime - startTime);
            if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);

            if (statusLabel) statusLabel.text = startingText;
            SceneLoader.Load(destination);
        }
    }
}
