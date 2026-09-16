using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Dialog{
    /// Handles mouse hover underline feedback and click interactions for dialogue choice options.
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DialogueOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler{
        [SerializeField] private TextMeshProUGUI textMesh;
        private Action _onClick;

        private void Awake() => textMesh = textMesh != null ? textMesh : GetComponent<TextMeshProUGUI>();

        private void OnDisable() => textMesh.fontStyle &= ~FontStyles.Underline;

        /// Sets the formatted option label and binds its selection callback.
        public void Initialize(string text, Action onClick){
            textMesh.text = text;
            _onClick = onClick;
        }

        /// Adds underline formatting while pointer hovers over the option.
        public void OnPointerEnter(PointerEventData eventData) => textMesh.fontStyle |= FontStyles.Underline;

        /// Removes underline formatting when pointer leaves the option.
        public void OnPointerExit(PointerEventData eventData) => textMesh.fontStyle &= ~FontStyles.Underline;

        /// Invokes the registered choice callback when clicked.
        public void OnPointerClick(PointerEventData eventData) => _onClick?.Invoke();
    }
}
