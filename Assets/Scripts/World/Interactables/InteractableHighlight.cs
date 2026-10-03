using System.Collections.Generic;
using UnityEngine;
using World.Actors.Brains;

namespace World.Interactables{
    /// Manages transient layer swapping and per-object outline coloring during hover and Alt reveal.
    [DisallowMultipleComponent]
    public class InteractableHighlight : MonoBehaviour{
        [Tooltip("Semantic highlight color category or Custom for arbitrary color.")]
        [SerializeField] private HighlightCategory category = HighlightCategory.Pickup;

        [Tooltip("Used when category is set to Custom.")]
        [SerializeField] private Color customColor = new(1f, 0.9f, 0.2f, 1f);

        [SerializeField] private List<Renderer> targetRenderers = new();

        private static InteractableHighlight _currentHovered;
        private static readonly HashSet<InteractableHighlight> _allRegistered = new();
        private static readonly int SILHOUETTE_PROP_ID = Shader.PropertyToID("_SilhouetteColor");
        private static bool _isAltHeld;
        private static int _highlightLayer = -1;

        private readonly List<int> _cachedLayers = new();
        private MaterialPropertyBlock _mpb;
        private bool _isHovered;
        private bool _isHighlighted;

        public HighlightCategory Category{
            get => category;
            set{
                category = value;
                if (_isHighlighted) ApplyHighlightState();
            }
        }

        public Color CustomColor{
            get => customColor;
            set{
                customColor = value;
                if (_isHighlighted && category == HighlightCategory.Custom) ApplyHighlightState();
            }
        }

        public Color ActiveColor => category == HighlightCategory.Custom ? customColor : InteractableOutlineFeature.GetCategoryColor(category);
        public IReadOnlyList<Renderer> TargetRenderers => targetRenderers;
        public static int HighlightLayer => _highlightLayer != -1 ? _highlightLayer : _highlightLayer = LayerMask.NameToLayer("Highlight");

        /// Sets or clears the active hovered instance from cursor raycasts.
        public static void SetHoveredInstance(InteractableHighlight target){
            if (_currentHovered == target) return;
            if (_currentHovered) _currentHovered.SetHovered(false);
            _currentHovered = target;
            if (_currentHovered) _currentHovered.SetHovered(true);
        }

        /// Clears the global hover state.
        public static void ClearHover() => SetHoveredInstance(null);

        /// Globally toggles highlight state for all registered interactables when Alt modifier changes.
        public static void SetAltHeld(bool isHeld){
            if (_isAltHeld == isHeld) return;
            _isAltHeld = isHeld;
            foreach (InteractableHighlight item in _allRegistered)
                item.RefreshHighlightState();
        }

        /// Populates target renderers, caches baseline layers, and initializes property block.
        private void Awake(){
            _mpb = new MaterialPropertyBlock();
            if (targetRenderers.Count == 0)
                targetRenderers.AddRange(GetComponentsInChildren<Renderer>(true));

            CacheLayers();
        }

        /// Registers instance with global registry and hooks into Alt modifier input events.
        private void OnEnable(){
            _allRegistered.Add(this);
            PlayerGestureController.OnAltModifierChanged += HandleAltChanged;
            if (PlayerGestureController.Instance && PlayerGestureController.IsAltPressed)
                SetAltHeld(true);
            RefreshHighlightState();
        }

        /// Unregisters instance and restores baseline renderer layers.
        private void OnDisable(){
            _allRegistered.Remove(this);
            PlayerGestureController.OnAltModifierChanged -= HandleAltChanged;
            if (_currentHovered == this) _currentHovered = null;
            RevertState();
        }

        /// Caches the baseline layer of each target renderer GameObject.
        private void CacheLayers(){
            _cachedLayers.Clear();
            for (int i = 0; i < targetRenderers.Count; i++)
                _cachedLayers.Add(targetRenderers[i].gameObject.layer);
        }

        /// Updates hover state and triggers highlight refresh.
        public void SetHovered(bool hovered){
            if (_isHovered == hovered) return;
            _isHovered = hovered;
            RefreshHighlightState();
        }

        /// Relays Alt modifier input state changes to global registry.
        private void HandleAltChanged(bool altPressed) => SetAltHeld(altPressed);

        /// Evaluates whether this object should currently be rendered on the highlight layer.
        private void RefreshHighlightState(){
            bool shouldHighlight = _isHovered || _isAltHeld;
            if (_isHighlighted == shouldHighlight) return;
            _isHighlighted = shouldHighlight;

            if (_isHighlighted) ApplyHighlightState();
            else RevertState();
        }

        /// Swaps target renderers to the highlight layer and applies the instanced outline color.
        private void ApplyHighlightState(){
            int layer = HighlightLayer;
            _mpb.SetColor(SILHOUETTE_PROP_ID, ActiveColor);
            for (int i = 0; i < targetRenderers.Count; i++){
                targetRenderers[i].gameObject.layer = layer;
                targetRenderers[i].SetPropertyBlock(_mpb);
            }
        }

        /// Restores target renderers to their cached baseline layers and clears property block.
        private void RevertState(){
            for (int i = 0; i < targetRenderers.Count; i++){
                targetRenderers[i].gameObject.layer = _cachedLayers[i];
                targetRenderers[i].SetPropertyBlock(null);
            }
        }
    }
}
