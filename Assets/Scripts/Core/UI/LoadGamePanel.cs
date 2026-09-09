using System.Collections.Generic;
using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI {
    /// Populates and manages the Load Game screen.
    /// Fetches all valid disk saves, instantiates selectable bullet items,
    /// and triggers session start through SceneCoordinator.
    [DisallowMultipleComponent]
    public class LoadGamePanel : MonoBehaviour {
        [Header("List References")]
        [SerializeField] private Transform contentContainer;
        [SerializeField] private SaveSlotItem bulletTemplate;

        [Header("Buttons")]
        [SerializeField] private Button loadButton;
        [SerializeField] private Button backButton;

        [Header("Menu Controller")]
        [SerializeField] private MenuAnimatorController animatorController;

        private readonly List<SaveSlotItem> _spawnedItems = new();
        private SaveSlotItem _selectedItem;

        private void Awake() {
            if (animatorController == null)
                animatorController = GetComponentInParent<MenuAnimatorController>();

            if (loadButton != null) {
                loadButton.interactable = false;
                loadButton.onClick.AddListener(OnLoadClicked);
            }

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (bulletTemplate != null && bulletTemplate.gameObject.scene.IsValid())
                bulletTemplate.gameObject.SetActive(false);
        }

        private void OnDestroy() {
            if (loadButton != null)
                loadButton.onClick.RemoveListener(OnLoadClicked);
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);
        }

        private void OnEnable() {
            RefreshSaveList();
        }

        private void OnDisable() {
            ClearSaveList();
        }

        /// Destroys all spawned bullet instances and resets selection state.
        public void ClearSaveList() {
            foreach (SaveSlotItem item in _spawnedItems) {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _spawnedItems.Clear();

            _selectedItem = null;
            if (loadButton != null)
                loadButton.interactable = false;
        }

        /// Clears and repopulates the save slots from disk metadata.
        public void RefreshSaveList() {
            ClearSaveList();

            List<SaveFileMetadata> saves = SaveSystem.GetSaveFileList();

            foreach (SaveFileMetadata save in saves) {
                SaveSlotItem item = Instantiate(bulletTemplate, contentContainer, false);
                RectTransform rt = item.GetComponent<RectTransform>();
                if (rt != null) {
                    rt.localPosition = Vector3.zero;
                    rt.localScale = Vector3.one;
                }

                item.gameObject.SetActive(true);
                item.Bind(save, OnSlotSelected);
                _spawnedItems.Add(item);
            }

            if (contentContainer is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private void OnSlotSelected(SaveSlotItem item) {
            _selectedItem = item;

            foreach (SaveSlotItem slot in _spawnedItems)
                slot.SetSelected(slot == _selectedItem);

            if (loadButton != null)
                loadButton.interactable = true;
        }

        private async void OnLoadClicked() {
            if (_selectedItem == null) return;
            if (loadButton != null)
                loadButton.interactable = false;

            if (animatorController != null)
                animatorController.CloseMenu();

            await SceneCoordinator.StartGameSessionAsync(_selectedItem.Metadata.slotName);
        }

        private void OnBackClicked() {
            if (animatorController != null)
                animatorController.OpenMainMenu();
        }
    }
}
