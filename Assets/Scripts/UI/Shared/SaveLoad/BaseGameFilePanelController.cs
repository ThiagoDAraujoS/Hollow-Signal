using System;
using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared.SaveLoad{
    /// Abstract base controller for save and load panels handling bullet selection, actions, and back navigation.
    [DisallowMultipleComponent]
    public abstract class BaseGameFilePanelController : MonoBehaviour{
        [Header("Carousel Reference")] [SerializeField]
        protected GameFileCarouselController carouselController;

        [SerializeField] protected Button backButton;

        public event Action         OnBackRequested;
        public event Action<string> OnSaveDeleted;

        /// Binds back button listener.
        protected virtual void Awake() => backButton.onClick.AddListener(HandleBackClicked);

        /// Subscribes to carousel selection changes.
        protected virtual void OnEnable() => carouselController.OnSelectionChanged += HandleSelectionChanged;

        /// Unsubscribes from carousel selection changes.
        protected virtual void OnDisable() => carouselController.OnSelectionChanged -= HandleSelectionChanged;

        /// Unregisters back button listener.
        protected virtual void OnDestroy() => backButton.onClick.RemoveListener(HandleBackClicked);

        /// Flushes save bullets from the carousel.
        protected virtual void DestroyList() => carouselController.DestroyCarousel();

        /// Triggers back navigation event.
        public void HandleBackClicked() => OnBackRequested?.Invoke();

        /// Invokes action for the currently selected bullet.
        public virtual void HandleActionButtonClicked() => HandleAction(carouselController.SelectedBullet);

        /// Deletes the currently selected save file from disk and removes its bullet from the carousel.
        public virtual void DeleteSelected(){
            GameFileBullet selected = carouselController.SelectedBullet;
            if (selected == null) return;
            if (string.IsNullOrEmpty(selected.SlotName) || selected.SlotName == "NEW_SAVE") return;

            string slotName = selected.SlotName;
            SaveSystem.DeleteSave(slotName);
            carouselController.DeleteBullet(selected);
            OnSaveDeleted?.Invoke(slotName);
        }

        /// Responds to carousel bullet selection updates.
        protected virtual void HandleSelectionChanged(GameFileBullet selectedBullet){}

        public abstract    void BuildList();
        protected abstract void HandleAction(GameFileBullet bullet);
    }
}
