using NUnit.Framework;
using UnityEngine;
using KanjiMaster.UI;

namespace KanjiRush.Tests
{
    /// <summary>
    /// Tests for the UITween animation abstraction. Backend-agnostic contract/lifecycle
    /// checks always run; the exact final-state assertions run only in the no-LeanTween
    /// (fallback) build, where each call applies its end state synchronously. Real motion
    /// (with LEANTWEEN_PRESENT) is verified manually in Play mode — LeanTween needs a
    /// running update loop, which EditMode tests don't drive.
    /// </summary>
    public class UITweenTests
    {
        private GameObject _go;
        private RectTransform _rt;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("tween_target", typeof(RectTransform));
            _rt = _go.GetComponent<RectTransform>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ---- Contract / lifecycle (run in BOTH backends) -----------------------

        [Test]
        public void Null_Target_Is_Always_Safe()
        {
            Assert.DoesNotThrow(() =>
            {
                UITween.FadeIn(null); UITween.FadeOut(null); UITween.FadeTo(null, 0.5f);
                UITween.ScaleTo(null, Vector3.one); UITween.Pop(null);
                UITween.SlideIn(null, Vector2.up); UITween.SlideOut(null, Vector2.up);
                UITween.Press(null); UITween.Cancel(null); UITween.CancelAll();
            });
        }

        [Test]
        public void Fade_Adds_A_CanvasGroup_If_Missing()
        {
            Assert.IsNull(_go.GetComponent<CanvasGroup>());
            UITween.FadeIn(_go);
            Assert.IsNotNull(_go.GetComponent<CanvasGroup>(), "fade ensures a CanvasGroup");
        }

        [Test]
        public void Repeated_And_Rapid_Calls_Do_Not_Throw()
        {
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    UITween.FadeIn(_go); UITween.Press(_go); UITween.Pop(_go);
                    UITween.SlideIn(_go, new Vector2(50, 0)); UITween.ScaleTo(_go, Vector3.one);
                }
            });
        }

        [Test]
        public void Disabled_Target_Is_Safe()
        {
            _go.SetActive(false);
            Assert.DoesNotThrow(() => { UITween.FadeIn(_go); UITween.Press(_go); UITween.Cancel(_go); });
        }

        [Test]
        public void Destroyed_Target_Is_Safe()
        {
            Object.DestroyImmediate(_go);
            Assert.DoesNotThrow(() => { UITween.FadeIn(_go); UITween.Pop(_go); UITween.Cancel(_go); });
            _go = null; // already destroyed
        }

        // ---- Final-state assertions (no-LeanTween fallback build only) ----------
#if !LEANTWEEN_PRESENT
        [Test]
        public void FadeIn_Ends_Fully_Visible()
        {
            UITween.FadeIn(_go);
            Assert.AreEqual(1f, _go.GetComponent<CanvasGroup>().alpha, 1e-4f);
        }

        [Test]
        public void FadeOut_Ends_Hidden()
        {
            UITween.FadeOut(_go);
            Assert.AreEqual(0f, _go.GetComponent<CanvasGroup>().alpha, 1e-4f);
        }

        [Test]
        public void ScaleTo_Ends_At_Target_And_Pop_Ends_At_One()
        {
            UITween.ScaleTo(_go, new Vector3(2, 2, 2));
            Assert.AreEqual(new Vector3(2, 2, 2), _go.transform.localScale);

            UITween.Pop(_go);
            Assert.AreEqual(Vector3.one, _go.transform.localScale);
        }

        [Test]
        public void SlideIn_Rests_At_Origin_And_SlideOut_Applies_Offset()
        {
            _rt.anchoredPosition = new Vector2(10, 20);
            UITween.SlideIn(_go, new Vector2(100, 0));
            Assert.AreEqual(new Vector2(10, 20), _rt.anchoredPosition, "slide-in returns to rest");

            UITween.SlideOut(_go, new Vector2(0, 50));
            Assert.AreEqual(new Vector2(10, 70), _rt.anchoredPosition, "slide-out offsets from rest");
        }

        [Test]
        public void Press_Returns_To_Normal_Scale()
        {
            UITween.Press(_go);
            Assert.AreEqual(Vector3.one, _go.transform.localScale);
        }

        [Test]
        public void OnComplete_Is_Invoked()
        {
            bool done = false;
            UITween.FadeIn(_go, onComplete: () => done = true);
            Assert.IsTrue(done, "completion callback fires");
        }
#endif

        // ---- LeanTween path proof (real backend build only) --------------------
        // With LeanTween, a primitive sets the START state and DEFERS the end to a tween
        // (which does not tick in EditMode), so the target does NOT reach its end value
        // synchronously — unlike the fallback, which would apply the end value instantly.
        // These assertions therefore prove UITween is driving LeanTween, not the fallback.
#if LEANTWEEN_PRESENT
        [Test]
        public void Fade_Uses_LeanTween_Not_Instant_Fallback()
        {
            var cg = _go.AddComponent<CanvasGroup>();
            cg.alpha = 0.5f;
            UITween.FadeIn(_go); // fallback would set alpha = 1 immediately
            Assert.Less(cg.alpha, 1f, "FadeIn deferred to a LeanTween tween (start applied, end pending)");
        }

        [Test]
        public void Pop_Uses_LeanTween_Not_Instant_Fallback()
        {
            UITween.Pop(_go); // fallback would set scale = 1 immediately
            Assert.Less(_go.transform.localScale.x, 1f,
                "Pop set the small start scale and deferred the overshoot-to-one to LeanTween");
        }

        [Test]
        public void SlideIn_Uses_LeanTween_Not_Instant_Fallback()
        {
            _rt.anchoredPosition = new Vector2(10, 20);
            UITween.SlideIn(_go, new Vector2(100, 0)); // fallback would land at rest (10,20)
            Assert.AreEqual(new Vector2(110, 20), _rt.anchoredPosition,
                "SlideIn placed the target at the start offset and deferred the move to LeanTween");
        }
#endif
    }
}
