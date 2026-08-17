using KanjiMaster.Progression;
using TMPro;
using UnityEngine;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Shows the player's current Global XP (e.g. "1250 XP") in a TMP text slot — for
    /// the Main Menu navbar, but not menu-specific so it can be reused anywhere.
    ///
    /// It is a pure CONSUMER of progression state: it reads <see cref="XpService.TotalXp"/>
    /// (the shared <see cref="PlayerProgressService.Default"/> progress) and never
    /// calculates, awards, or persists XP. The XP system does not depend on this.
    ///
    /// Refresh: reads on <see cref="OnEnable"/>, so it always reflects the latest value
    /// when the Main Menu scene loads / the object re-activates (Results → Main Menu,
    /// Revision → Main Menu, launch, …). No per-frame polling.
    /// </summary>
    public class XpDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text xpText;

        private void OnEnable() => Refresh();

        /// <summary>Read the current TotalXP and update the assigned text. Public so it
        /// can also be refreshed manually if ever needed.</summary>
        public void Refresh()
        {
            if (xpText) xpText.text = Format(XpService.TotalXp);
        }

        /// <summary>Display formatting only: "{n} XP". Never computes XP. Static + pure
        /// so the formatting/binding can be unit-tested without a scene.</summary>
        public static string Format(int totalXp) => $"{totalXp} XP";
    }
}
