using System;
using NUnit.Framework;
using KanjiMaster.Core;
using KanjiMaster.Progression;
using KanjiMaster.UI;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the Main Menu XP display's data-binding/formatting. The MonoBehaviour
    /// glue (OnEnable → set xpText.text) is trivial and needs a scene, so these test the
    /// pure formatter and that it reads the shared XP source without calculating or
    /// mutating XP.
    /// </summary>
    public class XpDisplayTests
    {
        private Action<PlayerProgress> _savedPersist;

        [SetUp]
        public void SetUp()
        {
            _savedPersist = XpService.Persist;
            XpService.Progress = new PlayerProgress(); // in-memory shared progress
            XpService.Persist = _ => { };
        }

        [TearDown]
        public void TearDown() => XpService.Persist = _savedPersist;

        // 1. Default/new progress → "0 XP"
        [Test]
        public void Shows_Zero_Xp_For_Default_Progress()
            => Assert.AreEqual("0 XP", XpDisplay.Format(XpService.TotalXp));

        // 2. Displays the current TotalXP
        [Test]
        public void Shows_Current_Total_Xp()
        {
            XpService.Progress.progression.xp.totalXp = 1250;
            Assert.AreEqual("1250 XP", XpDisplay.Format(XpService.TotalXp));
        }

        // 3. Reflects an updated TotalXP when refreshed (re-read of the source)
        [Test]
        public void Reflects_Updated_Total_Xp()
        {
            XpService.Progress.progression.xp.totalXp = 1000;
            Assert.AreEqual("1000 XP", XpDisplay.Format(XpService.TotalXp));

            XpService.AwardSessionCompletionXp(); // +50 via the existing system → 1050
            Assert.AreEqual("1050 XP", XpDisplay.Format(XpService.TotalXp), "re-reading shows the new value");
        }

        // 4. The display does not calculate XP — Format is a pure passthrough
        [Test]
        public void Does_Not_Calculate_Xp()
        {
            Assert.AreEqual("0 XP", XpDisplay.Format(0));
            Assert.AreEqual("10 XP", XpDisplay.Format(10));
            Assert.AreEqual("12500 XP", XpDisplay.Format(12500));
        }

        // 5. Reading for display does not modify TotalXP
        [Test]
        public void Reading_Does_Not_Modify_Total_Xp()
        {
            XpService.Progress.progression.xp.totalXp = 777;
            _ = XpDisplay.Format(XpService.TotalXp);
            _ = XpDisplay.Format(XpService.TotalXp);
            Assert.AreEqual(777, XpService.TotalXp, "display is read-only");
        }

        // 6. Reads the shared source of truth (no second PlayerProgress/persistence)
        [Test]
        public void Reads_The_Shared_Progress_Instance()
        {
            Assert.AreSame(PlayerProgressService.Default.Current, XpService.Progress,
                "Xp display reads via XpService → the shared PlayerProgressService.Default");
        }
    }
}
