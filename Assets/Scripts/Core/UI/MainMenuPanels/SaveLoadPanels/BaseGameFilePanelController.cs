using System;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.MainMenuPanels.SaveLoadPanels{
    /// Abstract base controller for save and load panels.
    /// Handles common carousel lifecycle, action button state, and back navigation.
    [DisallowMultipleComponent]
    public abstract class BaseGameFilePanelController : MonoBehaviour{
        [Header("Carousel Reference")][SerializeField]
        protected GameFileCarouselController carouselController;

        [Header("Buttons")][SerializeField]
        protected Button actionButton;

        [SerializeField] protected Button backButton;

        public event Action OnBackRequested;

        protected virtual void Awake(){
            actionButton.interactable = false;
            actionButton.onClick.AddListener(HandleActionClicked);
            backButton.onClick.AddListener(HandleBackClicked);
        }

        protected virtual void OnEnable(){
            carouselController.OnSelectionChanged += HandleSelectionChanged;
            UpdateActionButtonState(carouselController.SelectedBullet != null);
        }

        protected virtual void OnDisable() => carouselController.OnSelectionChanged -= HandleSelectionChanged;

        protected virtual void OnDestroy(){
            actionButton.onClick.RemoveListener(HandleActionClicked);
            backButton.onClick.RemoveListener(HandleBackClicked);
        }

        /// Flushes save bullets from the carousel.
        protected virtual void DestroyList(){
            carouselController.DestroyCarousel();
            UpdateActionButtonState(false);
        }

        public void HandleBackClicked() => OnBackRequested?.Invoke();

        protected virtual void HandleSelectionChanged(GameFileBullet selectedBullet) =>
            UpdateActionButtonState(selectedBullet != null);

        protected void UpdateActionButtonState(bool hasSelection) => actionButton.interactable = hasSelection;

        public abstract    void BuildList();
        protected abstract void HandleActionClicked();
    }
}
