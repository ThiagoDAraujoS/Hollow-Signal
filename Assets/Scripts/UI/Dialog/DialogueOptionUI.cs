using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Dialog{
    /// Handles mouse hover underline feedback, tooltips, and click interactions for dialogue choice options.
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DialogueOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler{
        [SerializeField] private TextMeshProUGUI textMesh;

        private Action                  _onClick;
        private Action<Vector2, string> _onHoverEnter;
        private Action                  _onHoverExit;
        private string                  _tooltipText;
        private bool                    _isInteractable = true;

        public bool IsInteractable => _isInteractable;

        /// Caches reference to the TextMeshProUGUI component.
        private void Awake() => textMesh = GetComponent<TextMeshProUGUI>();

        /// Removes underline style and dismisses active tooltip when disabled.
        private void OnDisable(){
            textMesh.fontStyle &= ~FontStyles.Underline;
            _onHoverExit?.Invoke();
        }

        /// Sets the formatted option label, interactivity, optional hover tooltip, and binds callbacks.
        public void Initialize(
            string text,
            Action onClick,
            bool isInteractable = true,
            string tooltipText = null,
            Action<Vector2, string> onHoverEnter = null,
            Action onHoverExit = null){
            textMesh.text = text;
            _onClick = onClick;
            _isInteractable = isInteractable;
            _tooltipText = tooltipText;
            _onHoverEnter = onHoverEnter;
            _onHoverExit = onHoverExit;
        }

        /// Adds underline formatting if interactable and dispatches tooltip event on hover.
        public void OnPointerEnter(PointerEventData eventData){
            if (_isInteractable)
                textMesh.fontStyle |= FontStyles.Underline;

            if (!string.IsNullOrEmpty(_tooltipText))
                _onHoverEnter?.Invoke(eventData.position, _tooltipText);
        }

        /// Removes underline formatting and clears tooltip on pointer exit.
        public void OnPointerExit(PointerEventData eventData){
            textMesh.fontStyle &= ~FontStyles.Underline;
            _onHoverExit?.Invoke();
        }

        /// Invokes the registered choice callback when clicked.
        public void OnPointerClick(PointerEventData eventData){
            if (!_isInteractable)
                return;
            _onHoverExit?.Invoke();
            _onClick();
        }
    }
}
