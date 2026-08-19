#if UNITY_EDITOR && LEANTWEEN_PRESENT
using UnityEditor;

namespace KanjiMaster.EditorTools
{
    /// <summary>
    /// Editor-only cleanup. LeanTween lazily creates a hidden "~LeanTween" manager
    /// GameObject the first time a tween runs. In the Editor it is NOT flagged
    /// HideAndDontSave / DontDestroyOnLoad the way it is in builds, so when you stop Play
    /// mode Unity warns: "Some objects were not cleaned up when closing the scene …
    /// ~LeanTween". Calling <c>LeanTween.reset()</c> as we exit Play mode destroys that
    /// object first, so the warning goes away. Runtime behaviour and builds are unaffected
    /// (LeanTween re-initialises itself lazily on the next tween).
    /// </summary>
    [InitializeOnLoad]
    public static class LeanTweenPlayModeCleanup
    {
        static LeanTweenPlayModeCleanup()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                LeanTween.reset(); // safe even if LeanTween never initialised (no-ops)
        }
    }
}
#endif
