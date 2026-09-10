using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.UI
{
    /// Represents a single save file slot bullet in the carousel with neon selector effects.
    [ExecuteAlways]
    public class SaveBullet : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI nameContainer;
        [SerializeField] private TextMeshProUGUI locationContainer;
        [SerializeField] private TextMeshProUGUI timeContainer;
        [SerializeField] private Image           imageContainer;
        [SerializeField] private Image           selectorContainer;
        [SerializeField] private bool            isSelected;

        [Header("Selector Animation")]
        [SerializeField] private float fadeSpeed = 8f;
        [Tooltip("Cycles per second. 0.25 means one full loop every 4 seconds.")]
        [SerializeField] private float pulseFrequency = 0.25f;
        [SerializeField] private float minPulseBrightness = 0.05f;
        [SerializeField] private float maxPulseBrightness = 1.0f;
        [SerializeField] private AnimationCurve pulseCurve;

        private Color _baseColor = Color.white;
        private float _selectionWeight;
        private float _flickerMultiplier = 1f;

        public string SlotName { get; private set; }
        public bool IsSelected => isSelected;
        public event Action<SaveBullet> OnClicked;

        private void Awake()
        {
            EnsureBaseColor();
            _selectionWeight   = isSelected ? 1f : 0f;
            _flickerMultiplier = isSelected ? 1f : 0f;

            if (TryGetComponent<Button>(out var button))
                button.onClick.AddListener(() => OnClicked?.Invoke(this));
        }

        public void Initialize(string charName, string location, string timestamp, Sprite snapshot = null, string slotName = "")
        {
            SlotName = slotName;
            if (nameContainer != null) nameContainer.text = charName;
            if (locationContainer != null) locationContainer.text = location;
            if (timeContainer != null) timeContainer.text = timestamp;
            if (imageContainer != null && snapshot != null) imageContainer.sprite = snapshot;
        }

        public void Update()
        {
            EnsureBaseColor();

            float targetWeight = isSelected ? 1f : 0f;
            _selectionWeight = Mathf.MoveTowards(_selectionWeight, targetWeight, Time.unscaledDeltaTime * fadeSpeed);

            if (_selectionWeight > 0.001f)
            {
                float t = (Time.unscaledTime * pulseFrequency) % 1f;
                float curveVal = (pulseCurve != null && pulseCurve.length > 0) ? pulseCurve.Evaluate(t) : 1f;
                _flickerMultiplier = Mathf.Lerp(minPulseBrightness, maxPulseBrightness, curveVal);
            }
            else
            {
                _flickerMultiplier = 0f;
            }

            ApplyVisualState();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectorContainer != null && !selectorContainer.gameObject.activeSelf)
                selectorContainer.gameObject.SetActive(true);

            if (!Application.isPlaying)
            {
                _selectionWeight = selected ? 1f : 0f;
                _flickerMultiplier = selected ? 1f : 0f;
                ApplyVisualState();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[SaveBullet] Click registered on: {gameObject.name}");
            OnClicked?.Invoke(this);
        }

        private void EnsureBaseColor()
        {
            if (_baseColor == default || _baseColor.a <= 0.01f || (_baseColor.r <= 0.01f && _baseColor.g <= 0.01f && _baseColor.b <= 0.01f))
            {
                if (selectorContainer != null && selectorContainer.color.a > 0.01f && (selectorContainer.color.r > 0.01f || selectorContainer.color.g > 0.01f || selectorContainer.color.b > 0.01f))
                    _baseColor = selectorContainer.color;
                else
                    _baseColor = Color.white;
            }
        }

        private void ApplyVisualState()
        {
            if (selectorContainer == null)
                return;

            Color c;
            c.r = _baseColor.r * _flickerMultiplier;
            c.g = _baseColor.g * _flickerMultiplier;
            c.b = _baseColor.b * _flickerMultiplier;
            c.a = _baseColor.a * _selectionWeight;
            selectorContainer.color = c;
        }
    }
}
