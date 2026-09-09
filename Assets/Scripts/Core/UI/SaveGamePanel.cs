using System.Collections.Generic;
using System.IO;
using Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI {
    /// Populates and manages the Save Game screen.
    /// Supports overwriting existing saves via selection and creating new named saves via a prompt modal.
    [DisallowMultipleComponent]
    public class SaveGamePanel : MonoBehaviour {
        [Header("List References")]
        [SerializeField] private Transform contentContainer;
        [SerializeField] private SaveSlotItem bulletTemplate;
        [SerializeField] private Button newSaveButton;

        [Header("Main Buttons")]
        [SerializeField] private Button overwriteSaveButton;
        [SerializeField] private Button backButton;

        [Header("New Save Modal Prompt")]
        [SerializeField] private GameObject promptModal;
        [SerializeField] private TMP_InputField saveNameInput;
        [SerializeField] private Button promptConfirmButton;
        [SerializeField] private Button promptCancelButton;

        [Header("Menu Controller")]
        [SerializeField] private MenuAnimatorController animatorController;

        private readonly List<SaveSlotItem> _spawnedItems = new();
        private SaveSlotItem _selectedItem;

        private void Awake() {
            if (animatorController == null)
                animatorController = GetComponentInParent<MenuAnimatorController>();

            if (bulletTemplate != null && bulletTemplate.gameObject.scene.IsValid())
                bulletTemplate.gameObject.SetActive(false);

            if (promptModal != null)
                promptModal.SetActive(false);

            if (overwriteSaveButton != null) {
                overwriteSaveButton.interactable = false;
                overwriteSaveButton.onClick.AddListener(OnOverwriteSaveClicked);
            }

            if (newSaveButton != null)
                newSaveButton.onClick.AddListener(OnNewSaveButtonClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (promptConfirmButton != null)
                promptConfirmButton.onClick.AddListener(OnPromptConfirmClicked);

            if (promptCancelButton != null)
                promptCancelButton.onClick.AddListener(OnPromptCancelClicked);
        }

        private void OnDestroy() {
            if (newSaveButton != null)
                newSaveButton.onClick.RemoveListener(OnNewSaveButtonClicked);
            if (overwriteSaveButton != null)
                overwriteSaveButton.onClick.RemoveListener(OnOverwriteSaveClicked);
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);

            if (promptConfirmButton != null)
                promptConfirmButton.onClick.RemoveListener(OnPromptConfirmClicked);
            if (promptCancelButton != null)
                promptCancelButton.onClick.RemoveListener(OnPromptCancelClicked);
        }

        private void OnEnable() {
            if (promptModal != null)
                promptModal.SetActive(false);

            RefreshSaveList();
        }

        private void OnDisable() {
            ClearSaveList();
            if (promptModal != null)
                promptModal.SetActive(false);
        }

        /// Clears all dynamically spawned save bullet instances.
        public void ClearSaveList() {
            foreach (SaveSlotItem item in _spawnedItems) {
                if (item != null)
                    Destroy(item.gameObject);
            }
            _spawnedItems.Clear();

            _selectedItem = null;
            if (overwriteSaveButton != null)
                overwriteSaveButton.interactable = false;
        }

        /// Clears dynamic saves and populates the list underneath the permanent new-save button.
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

            if (overwriteSaveButton != null)
                overwriteSaveButton.interactable = true;
        }

        private async void OnOverwriteSaveClicked() {
            if (_selectedItem == null) return;

            if (overwriteSaveButton != null)
                overwriteSaveButton.interactable = false;

            SaveSystem.SetSaveSlot(_selectedItem.Metadata.slotName);
            await SaveSystem.SaveGame();

            if (animatorController != null)
                animatorController.OpenMainMenu();
        }

        private void OnNewSaveButtonClicked() {
            _selectedItem = null;
            foreach (SaveSlotItem slot in _spawnedItems)
                slot.SetSelected(false);

            if (overwriteSaveButton != null)
                overwriteSaveButton.interactable = false;

            if (promptModal != null) {
                promptModal.SetActive(true);
                if (saveNameInput != null) {
                    saveNameInput.text = string.Empty;
                    saveNameInput.ActivateInputField();
                }
            }
        }

        private async void OnPromptConfirmClicked() {
            string rawName = saveNameInput != null ? saveNameInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(rawName)) return;

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                rawName = rawName.Replace(invalidChar, '_');

            if (promptConfirmButton != null)
                promptConfirmButton.interactable = false;

            SaveSystem.SetSaveSlot(rawName);
            await SaveSystem.SaveGame();

            if (promptConfirmButton != null)
                promptConfirmButton.interactable = true;

            if (promptModal != null)
                promptModal.SetActive(false);

            if (animatorController != null)
                animatorController.OpenMainMenu();
        }

        private void OnPromptCancelClicked() {
            if (promptModal != null)
                promptModal.SetActive(false);
        }

        private void OnBackClicked() {
            if (animatorController != null)
                animatorController.OpenMainMenu();
        }
    }
}
