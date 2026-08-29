using KanjiMaster.Progression;
using KanjiMaster.Services;
using UnityEngine;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Entry point. Sits in the Boot scene and routes to the right first screen BEFORE any
    /// UI is shown, so there is no flash of the wrong scene. The IDENTITY gate comes first:
    ///
    ///   authenticated            → Main Menu
    ///   guest / unknown          → Identity scene (choose Account or Guest)
    ///
    /// After the Identity scene, the Guest continuation reuses the existing onboarding
    /// check (Onboarding if incomplete, else Main Menu). Existing installs have no identity
    /// file yet, so they default to Unknown and see the Identity scene once; choosing Guest
    /// keeps all their existing PlayerProgress. See docs/IDENTITY.md.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            // TODO (later): warm up KanjiDatabase, load saved settings/services here.
            string destination = IdentityRouter.StartDestination(IdentityService.Default.IsAuthenticated);
            SceneLoader.Load(destination);
        }
    }
}
