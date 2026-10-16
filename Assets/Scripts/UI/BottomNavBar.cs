using System;
using KanjiMaster.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>The three top-level sections the bottom nav switches between.</summary>
    public enum NavSection { Game, Learn, Settings }

    /// <summary>
    /// Persistent-looking bottom navigation (Game · Learn · Settings) that appears on every
    /// post-onboarding screen. It is NOT a single cross-scene object — an instance of the
    /// <c>BottomNavBar</c> prefab lives in each scene (MainMenu, Game, Results, Learn, Settings),
    /// and each instance declares the <see cref="currentSection"/> it belongs to so the right
    /// icon is shown selected. Style it once by editing the prefab; placement is per scene.
    ///
    /// Section → scenes:
    ///   Game     → MainMenu, Game, Results (incl. revision). Tapping Game goes to MainMenu.
    ///   Learn    → Learn.
    ///   Settings → (scene not built yet). Disabled until <see cref="settingsEnabled"/> is on.
    ///
    /// Tapping an icon loads that section's home scene via <see cref="SceneLoader"/>. If you are
    /// already on the destination scene it does nothing. Build/refresh the prefab with:
    /// Tools ▸ KanjiMaster ▸ UI ▸ Build Bottom Nav Prefab.
    /// </summary>
    public class BottomNavBar : MonoBehaviour
    {
        /// <summary>One tappable item. All refs optional so the bar degrades gracefully.</summary>
        [Serializable]
        public class NavItem
        {
            [Tooltip("The tappable button for this item (its whole cell).")]
            public Button button;
            [Tooltip("The icon graphic — tinted between selected/normal.")]
            public Graphic icon;
            [Tooltip("Optional highlight (underline/pill) shown only when this item is selected.")]
            public GameObject selectedMark;
        }

        [Header("Which section this screen belongs to")]
        [Tooltip("At runtime this is derived from the active scene (see SectionForScene), so it " +
                 "stays correct automatically. The value here is only an editor preview.")]
        [SerializeField] private NavSection currentSection = NavSection.Game;

        [Header("Items")]
        [SerializeField] private NavItem game = new NavItem();
        [SerializeField] private NavItem learn = new NavItem();
        [SerializeField] private NavItem settings = new NavItem();

        [Header("Icon tint (multiplies the icon sprite)")]
        [Tooltip("Tint for the selected item — white keeps the icon's own colours.")]
        [SerializeField] private Color selectedTint = Color.white;
        [Tooltip("Tint for unselected items — a dimmed/translucent look.")]
        [SerializeField] private Color normalTint = new Color(1f, 1f, 1f, 0.40f);

        [Header("Settings section")]
        [Tooltip("Turn on once the Settings scene exists and is in Build Settings.")]
        [SerializeField] private bool settingsEnabled = false;

        private void Start()
        {
            // Derive the selected section from the scene we're in, so each scene's bar highlights
            // the right icon without any per-instance setup and can never drift out of sync.
            currentSection = SectionForScene(SceneManager.GetActiveScene().name);

            Wire(game, NavSection.Game);
            Wire(learn, NavSection.Learn);
            Wire(settings, NavSection.Settings);
            ApplySelection();
        }

        /// <summary>Maps a scene name to the nav section it belongs to.</summary>
        private static NavSection SectionForScene(string sceneName)
        {
            if (sceneName == SceneNames.Learn) return NavSection.Learn;
            if (sceneName == SceneNames.Settings) return NavSection.Settings;
            // MainMenu, Game, Results (incl. revision) all belong to the Game section.
            return NavSection.Game;
        }

        private void Wire(NavItem item, NavSection section)
        {
            if (item?.button == null) return;
            item.button.onClick.AddListener(() => OnTap(section));

            // The Settings cell is visible but inert until its scene exists (unless this IS the
            // settings screen, where it should still read as the current section).
            if (section == NavSection.Settings)
                item.button.interactable = settingsEnabled || currentSection == NavSection.Settings;
        }

        private void OnTap(NavSection section)
        {
            string target = DestinationScene(section);
            if (string.IsNullOrEmpty(target))
            {
                Debug.Log("[Nav] Settings isn't implemented yet.");
                return;
            }
            // Already on that section's home scene → nothing to do.
            if (SceneManager.GetActiveScene().name == target) return;
            SceneLoader.Load(target);
        }

        private string DestinationScene(NavSection section)
        {
            switch (section)
            {
                case NavSection.Game: return SceneNames.MainMenu; // the Game section's home
                case NavSection.Learn: return SceneNames.Learn;
                case NavSection.Settings: return settingsEnabled ? SceneNames.Settings : null;
                default: return null;
            }
        }

        private void ApplySelection()
        {
            Highlight(game, currentSection == NavSection.Game);
            Highlight(learn, currentSection == NavSection.Learn);
            Highlight(settings, currentSection == NavSection.Settings);
        }

        private void Highlight(NavItem item, bool selected)
        {
            if (item == null) return;
            if (item.icon) item.icon.color = selected ? selectedTint : normalTint;
            if (item.selectedMark) item.selectedMark.SetActive(selected);
        }

#if UNITY_EDITOR
        // Live-preview the selected state in the editor as you pick currentSection in the Inspector.
        private void OnValidate()
        {
            if (!Application.isPlaying) ApplySelection();
        }
#endif
    }
}
