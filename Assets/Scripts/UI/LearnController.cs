using KanjiMaster.Learn;
using KanjiMaster.Services;
using UnityEngine;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// The Learn section controller for an AUTHORED scene. It consumes the existing
    /// kanji data layer via <see cref="LearnCatalog"/> (no duplicate data), fills a
    /// scrollable list by instantiating a <see cref="KanjiListItem"/> prefab into an
    /// authored ScrollRect content, and drives the reusable <see cref="KanjiDetailView"/>.
    ///
    /// Build the UI in the Editor (Canvas Scaler, layout groups, ScrollRect, SafeArea)
    /// like the other scenes and wire the references below. Navigation lives here, not
    /// in the views. See docs/LEARN_SETUP.md.
    /// </summary>
    public class LearnController : MonoBehaviour
    {
        [Header("Screens (toggled)")]
        [SerializeField] private GameObject listScreen;
        [SerializeField] private GameObject detailScreen;

        [Header("List")]
        [SerializeField] private RectTransform listContent;     // the ScrollRect's Content
        [SerializeField] private KanjiListItem listItemPrefab;  // authored row prefab

        [Header("Detail")]
        [SerializeField] private KanjiDetailView detailView;

        [Header("Buttons")]
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;

        private LearnCatalog _catalog;
        private IKanjiIllustrationProvider _illustrations;
        private int _currentIndex;

        private void Start()
        {
            _catalog = LearnCatalog.Load();
            // Resources-backed today; falls back to the view's placeholder when absent.
            _illustrations = new ResourcesKanjiIllustrationProvider();

            PopulateList();

            if (mainMenuButton) mainMenuButton.onClick.AddListener(SceneLoader.GoToMainMenu);
            if (backButton) backButton.onClick.AddListener(ShowList);
            if (previousButton) previousButton.onClick.AddListener(() => OpenDetail(_currentIndex - 1));
            if (nextButton) nextButton.onClick.AddListener(() => OpenDetail(_currentIndex + 1));

            ShowList();
        }

        private void PopulateList()
        {
            if (listContent == null || listItemPrefab == null)
            {
                Debug.LogError("LearnController: assign 'listContent' and 'listItemPrefab' in the Inspector.");
                return;
            }

            var all = _catalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                int index = i; // capture for the callback
                var row = Instantiate(listItemPrefab, listContent);
                row.Bind(all[i], index, OpenDetail);
            }
        }

        private void OpenDetail(int index)
        {
            if (_catalog == null || _catalog.Count == 0) return;
            _currentIndex = Mathf.Clamp(index, 0, _catalog.Count - 1);

            var item = _catalog.All[_currentIndex];
            if (detailView) detailView.Show(item.Kanji, _illustrations, item.PlayerLevel);

            // Disable at the boundaries (no wrap-around).
            if (previousButton) previousButton.interactable = _currentIndex > 0;
            if (nextButton) nextButton.interactable = _currentIndex < _catalog.Count - 1;

            if (listScreen) listScreen.SetActive(false);
            if (detailScreen) detailScreen.SetActive(true);
        }

        private void ShowList()
        {
            if (detailScreen) detailScreen.SetActive(false);
            if (listScreen) listScreen.SetActive(true);
        }
    }
}
