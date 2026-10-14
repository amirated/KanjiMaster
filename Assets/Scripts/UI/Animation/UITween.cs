using System;
using UnityEngine;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Thin, project-owned animation layer over LeanTween. UI code calls
    /// <c>UITween.FadeIn / FadeOut / ScaleTo / Pop / SlideIn / SlideOut / Press(...)</c>
    /// and never touches LeanTween directly, so the tween backend stays an implementation
    /// detail we can swap later without rewriting UI controllers.
    ///
    /// Backend gating: LeanTween is used only when the <c>LEANTWEEN_PRESENT</c> scripting
    /// define is set (see docs/TOOLS_AND_ASSETS.md for the one-time integration). WITHOUT
    /// that define the layer still compiles and every call applies the animation's FINAL
    /// state instantly — a graceful no-motion fallback — so the project always builds and
    /// the UI still ends up in the correct state.
    ///
    /// Lifecycle: each primitive first CANCELS any tween already running on the same
    /// target (target-based, via LeanTween.cancel), so a second or rapid call replaces the
    /// first rather than fighting it. Null and destroyed targets are ignored. There is no
    /// persistent global manager — cancellation is per target, plus <see cref="CancelAll"/>
    /// for scene changes.
    /// </summary>
    public static class UITween
    {
        // While the app is quitting / exiting Play mode, every entry point is a no-op.
        // Otherwise a call here (e.g. a button's OnDisable → Cancel during scene teardown)
        // would re-initialise LeanTween and spawn its "~LeanTween" manager GameObject inside
        // the scene being closed — which Unity reports as "objects were not cleaned up when
        // closing the scene". Application.quitting fires BEFORE OnDisable/OnDestroy, so the
        // flag is armed in time.
        private static bool _shuttingDown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InstallShutdownGuard()
        {
            _shuttingDown = false;                  // reset for each Play session
            Application.quitting -= OnAppQuitting;   // idempotent across domain reloads
            Application.quitting += OnAppQuitting;
        }

        private static void OnAppQuitting() => _shuttingDown = true;

        // ---- Fade (uses a CanvasGroup, added if missing) ------------------------

        public static void FadeIn(GameObject target, float? duration = null, Action onComplete = null)
            => Fade(target, from: 0f, to: 1f, duration ?? UITweenConfig.FadeDuration, onComplete);

        public static void FadeOut(GameObject target, float? duration = null, Action onComplete = null)
            => Fade(target, from: null, to: 0f, duration ?? UITweenConfig.FadeDuration, onComplete);

        public static void FadeTo(GameObject target, float alpha, float? duration = null, Action onComplete = null)
            => Fade(target, from: null, to: alpha, duration ?? UITweenConfig.FadeDuration, onComplete);

        private static void Fade(GameObject target, float? from, float to, float duration, Action onComplete)
        {
            if (_shuttingDown || !target) return;
            var cg = GetCanvasGroup(target);
            Cancel(target);
            if (from.HasValue) cg.alpha = from.Value;
#if LEANTWEEN_PRESENT
            LeanTween.alphaCanvas(cg, to, duration)
                .setEase(LeanTweenType.easeOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => onComplete?.Invoke());
#else
            cg.alpha = to;
            onComplete?.Invoke();
#endif
        }

        // ---- Scale --------------------------------------------------------------

        public static void ScaleTo(GameObject target, Vector3 scale, float? duration = null, Action onComplete = null)
        {
            if (_shuttingDown || !target) return;
            Cancel(target);
#if LEANTWEEN_PRESENT
            LeanTween.scale(target, scale, duration ?? UITweenConfig.ScaleDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => onComplete?.Invoke());
#else
            target.transform.localScale = scale;
            onComplete?.Invoke();
#endif
        }

        /// <summary>Scale-in "pop": starts small and overshoots to 1.</summary>
        public static void Pop(GameObject target, float? duration = null, Action onComplete = null)
        {
            if (_shuttingDown || !target) return;
            Cancel(target);
#if LEANTWEEN_PRESENT
            target.transform.localScale = Vector3.one * UITweenConfig.PopFromScale;
            LeanTween.scale(target, Vector3.one, duration ?? UITweenConfig.PopDuration)
                .setEase(LeanTweenType.easeOutBack)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => onComplete?.Invoke());
#else
            target.transform.localScale = Vector3.one;
            onComplete?.Invoke();
#endif
        }

        // ---- Slide (RectTransform anchoredPosition) -----------------------------

        /// <summary>Slide in from <paramref name="fromOffset"/> to the element's current
        /// resting position.</summary>
        public static void SlideIn(GameObject target, Vector2 fromOffset, float? duration = null, Action onComplete = null)
        {
            if (_shuttingDown) return;
            var rt = AsRect(target);
            if (rt == null) return;
            Cancel(target);
            Vector2 rest = rt.anchoredPosition;
            rt.anchoredPosition = rest + fromOffset;
#if LEANTWEEN_PRESENT
            LeanTween.move(rt, (Vector3)rest, duration ?? UITweenConfig.SlideDuration)
                .setEase(LeanTweenType.easeOutCubic)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => onComplete?.Invoke());
#else
            rt.anchoredPosition = rest;
            onComplete?.Invoke();
#endif
        }

        /// <summary>Slide out from the current position by <paramref name="toOffset"/>.</summary>
        public static void SlideOut(GameObject target, Vector2 toOffset, float? duration = null, Action onComplete = null)
        {
            if (_shuttingDown) return;
            var rt = AsRect(target);
            if (rt == null) return;
            Cancel(target);
            Vector2 end = rt.anchoredPosition + toOffset;
#if LEANTWEEN_PRESENT
            LeanTween.move(rt, (Vector3)end, duration ?? UITweenConfig.SlideDuration)
                .setEase(LeanTweenType.easeInCubic)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => onComplete?.Invoke());
#else
            rt.anchoredPosition = end;
            onComplete?.Invoke();
#endif
        }

        // ---- Button press feedback (down then back up) --------------------------

        public static void Press(GameObject target, Action onComplete = null)
        {
            if (_shuttingDown || !target) return;
            Cancel(target);
#if LEANTWEEN_PRESENT
            LeanTween.scale(target, Vector3.one * UITweenConfig.PressScale, UITweenConfig.PressDownDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                    LeanTween.scale(target, Vector3.one, UITweenConfig.PressUpDuration)
                        .setEase(LeanTweenType.easeOutQuad)
                        .setIgnoreTimeScale(true)
                        .setOnComplete(() => onComplete?.Invoke()));
#else
            target.transform.localScale = Vector3.one;
            onComplete?.Invoke();
#endif
        }

        // ---- Lifecycle ----------------------------------------------------------

        /// <summary>Cancel any tween currently running on this target. Safe on null.</summary>
        public static void Cancel(GameObject target)
        {
            if (_shuttingDown || !target) return;
#if LEANTWEEN_PRESENT
            LeanTween.cancel(target);
#endif
        }

        /// <summary>Cancel ALL active tweens — e.g. call before a scene change.</summary>
        public static void CancelAll()
        {
            if (_shuttingDown) return;
#if LEANTWEEN_PRESENT
            LeanTween.cancelAll();
#endif
        }

        // ---- Helpers ------------------------------------------------------------

        private static CanvasGroup GetCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            return cg != null ? cg : go.AddComponent<CanvasGroup>();
        }

        private static RectTransform AsRect(GameObject go)
        {
            if (!go) return null;
            return go.transform as RectTransform;
        }
    }
}
