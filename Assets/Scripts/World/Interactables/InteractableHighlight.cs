using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World.Actors.Brains;
using World.Tactical;

namespace World.Interactables{
    /// Controls shader highlight amount on renderers via MaterialPropertyBlock for hover and Alt-reveal.
    [DisallowMultipleComponent]
    public class InteractableHighlight : MonoBehaviour{
        [Header("Renderers to Highlight")]
        [SerializeField] private List<Renderer> targetRenderers = new();

        [Header("Optional Linked Slot Indicator")]
        [SerializeField] private AreaSlot linkedSlot;

        [Header("Fade Tuning")]
        [SerializeField] private float fadeDuration = 0.15f;

        private static readonly int HighlightPropId = Shader.PropertyToID("_HighlightAmount");
        private MaterialPropertyBlock _mpb;
        private Coroutine _fadeRoutine;
        private float _currentAmount;
        private bool _isHovered;
        private bool _isAltRevealed;

        /// Initializes property block and resolves default renderers and linked slot.
        private void Awake(){
            _mpb = new MaterialPropertyBlock();
            if (targetRenderers.Count == 0)
                targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
            if (linkedSlot == null)
                linkedSlot = GetComponentInChildren<AreaSlot>();
        }

        /// Subscribes to Alt-modifier change events.
        private void OnEnable() => PlayerBrain.OnAltModifierChanged += HandleAltChanged;

        /// Unsubscribes from Alt-modifier events and resets highlight.
        private void OnDisable(){
            PlayerBrain.OnAltModifierChanged -= HandleAltChanged;
            SetInstant(0f);
        }

        /// Triggers highlight when mouse enters collider.
        private void OnMouseEnter() => SetHovered(true);

        /// Clears highlight when mouse leaves collider.
        private void OnMouseExit() => SetHovered(false);

        /// Sets hover state and triggers highlight fade.
        public void SetHovered(bool hovered){
            _isHovered = hovered;
            UpdateHighlightTarget();
        }

        /// Updates Alt-reveal state from input coordinator.
        private void HandleAltChanged(bool isPressed){
            _isAltRevealed = isPressed;
            UpdateHighlightTarget();
        }

        /// Evaluates active hover/alt state and starts fade coroutine.
        private void UpdateHighlightTarget(){
            bool shouldHighlight = _isHovered || _isAltRevealed;
            float target = shouldHighlight ? 1f : 0f;

            if (linkedSlot != null)
                linkedSlot.SetVisualActive(shouldHighlight);

            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(FadeTo(target));
        }

        /// Smoothly interpolates highlight property over duration.
        private IEnumerator FadeTo(float target){
            float start = _currentAmount;
            float elapsed = 0f;

            while (elapsed < fadeDuration){
                elapsed += Time.unscaledDeltaTime;
                ApplyAmount(Mathf.Lerp(start, target, elapsed / fadeDuration));
                yield return null;
            }

            ApplyAmount(target);
            _fadeRoutine = null;
        }

        /// Immediately sets highlight amount without transition.
        public void SetInstant(float amount){
            if (_fadeRoutine != null){
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
            ApplyAmount(amount);
        }

        /// Writes float property to renderers via MaterialPropertyBlock.
        private void ApplyAmount(float amount){
            _currentAmount = amount;
            foreach (Renderer r in targetRenderers){
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(HighlightPropId, amount);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
