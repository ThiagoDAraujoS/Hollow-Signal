using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Shared.SaveLoad{
    /// Represents a single save file slot bullet in the carousel with neon selector effects.
    [ExecuteAlways]
    public class GameFileBullet : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler{
        [SerializeField] private TextMeshProUGUI nameContainer;
        [SerializeField] private TextMeshProUGUI locationContainer;
        [SerializeField] private TextMeshProUGUI timeContainer;
        [SerializeField] private Image           imageContainer;
        [SerializeField] private Image           selectorContainer;
        [SerializeField] private bool            isSelected;

        [Header("Selector Animation")] [SerializeField]
        private float fadeSpeed = 8f;

        [Tooltip("Cycles per second. 0.25 means one full loop every 4 seconds.")] [SerializeField]
        private float pulseFrequency = 0.25f;

        [SerializeField] private float          minPulseBrightness = 0.05f;
        [SerializeField] private float          maxPulseBrightness = 1.0f;
        [SerializeField] private AnimationCurve pulseCurve;

        private Color _baseColor = Color.white;
        private float _selectionWeight;
        private float _flickerMultiplier = 1f;

        public string                       SlotName   { get; private set; }
        public bool                         IsSelected => isSelected;
        public event Action<GameFileBullet> OnHovered;
        public event Action<GameFileBullet> OnActionRequested;

        /// Initializes selector color and binds fallback button click.
        private void Awake(){
            EnsureBaseColor();
            _selectionWeight = isSelected ? 1f : 0f;
            _flickerMultiplier = isSelected ? 1f : 0f;
            if (TryGetComponent<Button>(out var button))
                button.onClick.AddListener(() => OnActionRequested?.Invoke(this));
        }

        /// Populates text, snapshot, and slot metadata for this save bullet.
        public void Initialize(string charName, string location, string timestamp, Sprite snapshot = null, string slotName = ""){
            SlotName = slotName;
            nameContainer.text = charName;
            locationContainer.text = location;
            timeContainer.text = timestamp;
            if (snapshot != null)
                imageContainer.sprite = snapshot;
        }

        /// Animates selection weight fade and pulse flicker.
        public void Update(){
            EnsureBaseColor();
            float targetWeight = isSelected ? 1f : 0f;
            _selectionWeight = Mathf.MoveTowards(_selectionWeight, targetWeight, Time.unscaledDeltaTime * fadeSpeed);
            if (_selectionWeight > 0.001f)
                _flickerMultiplier = Mathf.Lerp(minPulseBrightness, maxPulseBrightness, pulseCurve.Evaluate((Time.unscaledTime * pulseFrequency) % 1f));
            else
                _flickerMultiplier = 0f;

            ApplyVisualState();
        }

        /// Sets the selection state and refreshes container visibility.
        public void SetSelected(bool selected){
            isSelected = selected;
            if (!selectorContainer.gameObject.activeSelf)
                selectorContainer.gameObject.SetActive(true);
            if (Application.isPlaying) return;
            _selectionWeight = selected ? 1f : 0f;
            _flickerMultiplier = selected ? 1f : 0f;
            ApplyVisualState();
        }

        /// Notifies listeners that cursor hovered over this bullet.
        public void OnPointerEnter(PointerEventData eventData) => OnHovered?.Invoke(this);

        /// Notifies listeners that this bullet was clicked to trigger action.
        public void OnPointerClick(PointerEventData eventData) => OnActionRequested?.Invoke(this);

        /// Caches initial selector container color to prevent dark-out.
        private void EnsureBaseColor(){
            if (_baseColor == default || _baseColor.a <= 0.01f)
                _baseColor = selectorContainer.color.a > 0.01f ? selectorContainer.color : Color.white;
        }

        /// Updates selector container color tint based on weight and flicker.
        private void ApplyVisualState(){
            Color c = _baseColor * _flickerMultiplier;
            c.a = _baseColor.a * _selectionWeight;
            selectorContainer.color = c;
        }
    }
}
