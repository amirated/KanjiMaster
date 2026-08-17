using KanjiMaster.Progression;
using KanjiMaster.Services;
using UnityEngine;

namespace KanjiMaster.Core
{
    /// <summary>
    /// Entry point. Sits in the Boot scene and routes to the right first screen BEFORE
    /// any UI is shown: new players (and anyone with incomplete onboarding) go to the
    /// Onboarding scene; returning players go straight to the Main Menu. The decision is
    /// made from the already-loaded <see cref="PlayerProgress"/> — no duplicate save
    /// loading, no brief flash of the wrong scene.
    ///
    /// Existing players from before onboarding existed are grandfathered to "completed"
    /// on load (see OnboardingService), so they are never sent through onboarding.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            // TODO (later): warm up KanjiDatabase, load saved settings/services here.
            var progress = PlayerProgressService.Default.Current; // single load (cached, spans scenes)
            string destination = OnboardingRouter.StartDestination(OnboardingService.IsComplete(progress));
            SceneLoader.Load(destination);
        }
    }
}
