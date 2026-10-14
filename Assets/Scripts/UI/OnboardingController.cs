using System.Collections.Generic;
using KanjiMaster.Progression;
using KanjiMaster.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Drives the first-time onboarding: renders one <see cref="OnboardingPage"/> card at
    /// a time and handles Next / Back / final CTA. It is an AUTHORED scene component —
    /// build the layout in Onboarding.unity and assign the slots in the Inspector
    /// (see docs/ONBOARDING.md). Content is data-driven: leave <c>pages</c> empty to use
    /// the built-in defaults, or fill/reorder them in the Inspector.
    ///
    /// The final CTA marks onboarding complete and PERSISTS it BEFORE navigating to the
    /// Main Menu (guest completion). Duplicate taps are guarded here and by SceneLoader.
    /// </summary>
    public class OnboardingController : MonoBehaviour
    {
        [Header("Card slots")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image image;                 // optional
        [SerializeField] private GameObject imageContainer;   // hidden when a page has no image
        [SerializeField] private TMP_Text pageIndicatorText;  // optional, e.g. "1 / 5"

        [Header("Navigation")]
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonLabel;    // "Next" → CTA on the last page
        [SerializeField] private Button backButton;           // hidden on the first page

        [Header("Content (optional — empty uses built-in defaults)")]
        [SerializeField] private List<OnboardingPage> pages = new List<OnboardingPage>();

        private OnboardingFlow _flow;
        private bool _completing;

        private void Start()
        {
            if (pages == null || pages.Count == 0) pages = OnboardingPages.Default();
            _flow = new OnboardingFlow(pages.Count);

            if (nextButton) nextButton.onClick.AddListener(OnNext);
            if (backButton) backButton.onClick.AddListener(OnBack);

            Render();
        }

        private void Render()
        {
            var page = pages[_flow.Index];

            if (titleText) titleText.text = page.title;
            if (bodyText) bodyText.text = page.body;

            bool hasImage = page.image != null;
            if (imageContainer) imageContainer.SetActive(hasImage);
            if (image && hasImage) { image.sprite = page.image; image.preserveAspect = true; }

            if (pageIndicatorText) pageIndicatorText.text = $"{_flow.Index + 1} / {_flow.Count}";

            if (nextButtonLabel)
                nextButtonLabel.text = _flow.IsLast
                    ? (string.IsNullOrEmpty(page.ctaOverride) ? "Start Learning" : page.ctaOverride)
                    : "Next";

            if (backButton) backButton.gameObject.SetActive(!_flow.IsFirst); // no Back on first
        }

        private void OnNext()
        {
            if (_completing) return;
            if (_flow.Next()) Render();   // advanced to the next card
            else Complete();              // was the last card → finish
        }

        private void OnBack()
        {
            if (_completing) return;
            if (_flow.Back()) Render();
        }

        private void Complete()
        {
            if (_completing) return;      // ignore repeated taps on the final CTA
            _completing = true;

            OnboardingService.CompleteAsGuest(); // 1) mark complete  2) persist
            SceneLoader.GoToMainMenu();          // 3) navigate (only after persistence)
        }
    }
}
