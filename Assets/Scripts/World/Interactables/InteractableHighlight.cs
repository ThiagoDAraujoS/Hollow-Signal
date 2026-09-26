using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using World.Actors.Brains;
using World.Tactical;

namespace World.Interactables{
    /// Controls shader highlight amount on renderers via MaterialPropertyBlock for hover and Alt-reveal.
    [DisallowMultipleComponent]
    public class InteractableHighlight : MonoBehaviour{
        [Header("Renderers to Highlight")] [SerializeField]
        private List<Renderer> targetRenderers = new();

        [Header("Optional Linked Slot Indicator")] [SerializeField]
        private AreaSlot linkedSlot;

        [Header("Fade Tuning")] [SerializeField]
        private float fadeDuration = 0.15f;

        private static readonly int HIGHLIGHT_PROP_ID = Shader.PropertyToID("_HighlightAmount");
        private static InteractableHighlight _currentHovered;

        private MaterialPropertyBlock _mpb;
        private Coroutine _fadeRoutine;
        private float _currentAmount;
        private bool _isHovered;
        private bool _isAltRevealed;

        /// Globally sets the active hovered instance from gesture coordinator.
        public static void SetHoveredInstance(InteractableHighlight target){
            if (_currentHovered == target) return;
            if (_currentHovered) _currentHovered.SetHovered(false);
            _currentHovered = target;
            if (_currentHovered) _currentHovered.SetHovered(true);
        }

        /// Clears global hover state.
        public static void ClearHover(){
            if (!_currentHovered) return;
            _currentHovered.SetHovered(false);
            _currentHovered = null;
        }

        /// Initializes property block and resolves default renderers and linked slot.
        private void Awake(){
            _mpb = new MaterialPropertyBlock();
            if (targetRenderers.Count == 0) targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
            if (!linkedSlot) linkedSlot = GetComponentInChildren<AreaSlot>();
        }

        /// Subscribes to Alt-modifier change events.
        private void OnEnable(){
            PlayerGestureController.OnAltModifierChanged += HandleAltChanged;
            if (PlayerGestureController.Instance && PlayerGestureController.IsAltPressed) HandleAltChanged(true);
        }

        /// Unsubscribes from Alt-modifier events and resets highlight.
        private void OnDisable(){
            PlayerGestureController.OnAltModifierChanged -= HandleAltChanged;
            if (_currentHovered == this) _currentHovered = null;
            SetInstant(0f);
        }

        /// Monitors keyboard Alt state directly as input fallback.
        private void Update(){
            if (Keyboard.current == null) return;
            bool altPressed = Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;
            if (altPressed != _isAltRevealed) HandleAltChanged(altPressed);
        }

        /// Sets hover state and triggers highlight fade.
        public void SetHovered(bool hovered){
            if (_isHovered == hovered) return;
            _isHovered = hovered;
            UpdateHighlightTarget();
        }

        /// Updates Alt-reveal state from input coordinator.
        private void HandleAltChanged(bool isPressed){
            if (_isAltRevealed == isPressed) return;
            _isAltRevealed = isPressed;
            UpdateHighlightTarget();
        }

        /// Evaluates active hover/alt state and starts fade coroutine.
        private void UpdateHighlightTarget(){
            bool shouldHighlight = _isHovered || _isAltRevealed;
            float target = shouldHighlight ? 1f : 0f;

            if (linkedSlot) linkedSlot.SetVisualActive(shouldHighlight);
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);

            if (isActiveAndEnabled && gameObject.activeInHierarchy)
                _fadeRoutine = StartCoroutine(FadeTo(target));
            else
                SetInstant(target);
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
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(HIGHLIGHT_PROP_ID, amount);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
