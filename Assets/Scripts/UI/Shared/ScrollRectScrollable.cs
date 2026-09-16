using Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Shared{
    /// Forwards IScrollable input from PlayerBrain to a target ScrollRect.
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectScrollable : MonoBehaviour, IScrollable{
        [SerializeField] private ScrollRect scrollRect;

        private void Awake() => scrollRect = scrollRect != null ? scrollRect : GetComponent<ScrollRect>();

        /// Advances the target ScrollRect using native scroll physics and sensitivity.
        public void OnScroll(float delta){
            if (scrollRect == null) return;
            float normalizedDelta = Mathf.Abs(delta) > 1f ? delta / 120f : delta;
            scrollRect.OnScroll(new PointerEventData(EventSystem.current){
                scrollDelta = new Vector2(0f, normalizedDelta)
            });
        }
    }
}
