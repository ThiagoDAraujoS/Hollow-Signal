using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared.SaveLoad{
    /// Abstract base controller for save and load panels handling bullet action and back navigation.
    [DisallowMultipleComponent]
    public abstract class BaseGameFilePanelController : MonoBehaviour{
        [Header("Carousel Reference")] [SerializeField]
        protected GameFileCarouselController carouselController;

        [SerializeField] protected Button backButton;

        public event Action OnBackRequested;

        /// Binds button listeners and initializes action state.
        protected virtual void Awake() => backButton.onClick.AddListener(HandleBackClicked);
        
        /// Subscribes to carousel bullet action and selection events.
        protected virtual void OnEnable() => carouselController.OnBulletActionRequested += HandleAction;

        /// Unsubscribes from carousel events.
        protected virtual void OnDisable() => carouselController.OnBulletActionRequested -= HandleAction;


        /// Unregisters button listeners.
        protected virtual void OnDestroy() => backButton.onClick.RemoveListener(HandleBackClicked);


        /// Flushes save bullets from the carousel.
        protected virtual void DestroyList() => carouselController.DestroyCarousel();

        /// Triggers back navigation event.
        public void HandleBackClicked() => OnBackRequested?.Invoke();
  
        /// Forwards legacy button click to action handler.
        protected virtual void HandleActionButtonClicked() => HandleAction(carouselController.SelectedBullet);

        public abstract    void BuildList();
        protected abstract void HandleAction(GameFileBullet bullet);
    }
}
