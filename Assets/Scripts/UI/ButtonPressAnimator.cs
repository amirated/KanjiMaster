using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KanjiMaster.UI
{
    /// <summary>
    /// Subtle press feedback for a UI button: it scales down slightly on pointer-down and
    /// smoothly returns on release (or if the finger drags away). All motion goes through
    /// <see cref="UITween"/> (→ LeanTween) — never LeanTween directly.
    ///
    /// It respects the button's ACTUAL resting scale captured at startup (it does not
    /// assume <see cref="Vector3.one"/>), applies <see cref="UITweenConfig.PressScale"/> as
    /// a multiplier of that rest scale, and always returns to the exact rest scale — so it
    /// never permanently changes or drifts the transform. Add it to a button root; it
    /// coexists with the Unity Button component and only animates while interactable.
    /// </summary>
    [DisallowMultipleComponent]
    public class ButtonPressAnimator : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 _restScale;
        private Selectable _selectable; // optional; used to skip animation when not interactable
        private bool _pressed;

        private void Awake()
        {
            _restScale = transform.localScale;      // authored/instance resting scale (e.g. 1.2)
            _selectable = GetComponent<Selectable>();
        }

        // If the button is hidden/disabled mid-press, cancel its tween and snap back to
        // rest so it can never get stuck at the pressed scale.
        private void OnDisable()
        {
            UITween.Cancel(gameObject);
            transform.localScale = _restScale;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;
            _pressed = true;
            UITween.ScaleTo(gameObject, _restScale * UITweenConfig.PressScale, UITweenConfig.PressDownDuration);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        // Dragging the finger off the button releases the press visually.
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            UITween.ScaleTo(gameObject, _restScale, UITweenConfig.PressUpDuration);
        }
    }
}
