using System;
using Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI {
    /// Controller for an individual save slot entry in the load/save menu list.
    public class SaveSlotItem : MonoBehaviour {
        [SerializeField] private TextMeshProUGUI slotNameText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private Button selectButton;

        [Header("Selection Colors")]
        [SerializeField] private Color normalColor = new Color(0.18f, 0.18f, 0.22f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.45f, 0.65f, 1f);

        public SaveFileMetadata Metadata { get; private set; }

        private void Awake() {
            if (selectButton == null)
                selectButton = GetComponentInChildren<Button>();
        }

        public void Bind(SaveFileMetadata metadata, Action<SaveSlotItem> onSelected) {
            Metadata = metadata;
            if (slotNameText != null)
                slotNameText.text = metadata.slotName;
            if (dateText != null)
                dateText.text = metadata.lastSaveTime.ToString("yyyy-MM-dd HH:mm");

            if (selectButton == null)
                selectButton = GetComponentInChildren<Button>();

            if (selectButton != null) {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelected?.Invoke(this));
            }
            SetSelected(false);
        }

        public void SetSelected(bool isSelected) {
            if (selectButton != null && selectButton.targetGraphic != null)
                selectButton.targetGraphic.color = isSelected ? selectedColor : normalColor;
        }
    }
}
