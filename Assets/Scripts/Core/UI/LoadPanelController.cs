using System;
using System.Collections.Generic;
using System.Linq;
using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI{
    /// Coordinates the Load Game panel, fetching save data from SaveSystem,
    /// populating the SaveCarouselController, and triggering game loading through SceneCoordinator.
    [DisallowMultipleComponent]
    public class LoadPanelController : MonoBehaviour{
        [Header("Carousel Reference")] 
        [SerializeField]
        private SaveCarouselController carouselController;

        [Header("Buttons")] 
        [SerializeField] 
        private Button loadButton;
        
        [SerializeField]                     
        private Button backButton;

        private void Awake(){
            carouselController = GetComponentInChildren<SaveCarouselController>();
            loadButton.interactable = false;
            loadButton.onClick.AddListener(HandleLoadClicked);
        }

        private void OnEnable() => carouselController.OnSelectionChanged += HandleSelectionChanged;

        private void OnDisable() => carouselController.OnSelectionChanged -= HandleSelectionChanged;

        private void OnDestroy() => loadButton.onClick.RemoveListener(HandleLoadClicked);

        /// Fetches all save files from SaveSystem and passes formatted bullet data to the carousel.
        /// Can be invoked directly by Animation Events.
        [ContextMenu("Build Load List")]
        public void BuildLoadList(){
            List<SaveFileMetadata> saveFiles      = SaveSystem.GetSaveFileList();
            List<SaveBulletData>   bulletDataList = saveFiles.Select(meta => 
                                                                         new SaveBulletData(
                                                                             meta.slotName, 
                                                                             "Station Outpost", 
                                                                             meta.lastSaveTime.ToString("yyyy-MM-dd HH:mm"))).ToList();
            carouselController.BuildCarousel(bulletDataList);
            UpdateLoadButtonState(carouselController.SelectedBullet != null);
        }

        /// Flushes all save bullets from the carousel to free memory.
        /// Can be invoked directly by Animation Events.
        [ContextMenu("Destroy Load List")]
        public void DestroyLoadList(){
            carouselController.DestroyCarousel();
            UpdateLoadButtonState(false);
        }

        /// Triggers game loading using the currently tagged save slot in the carousel.
        public async void HandleLoadClicked(){
            try{
                if (carouselController.SelectedBullet == null){
                    Debug.LogWarning("[LoadPanelController] Cannot load: No save bullet selected.");
                    return;
                }
                string slotToLoad = carouselController.SelectedBullet.SlotName;
                Debug.Log($"[LoadPanelController] Loading save slot: {slotToLoad}");
                loadButton.interactable = false;
                await SceneCoordinator.StartGameSessionAsync(slotToLoad);
                DestroyLoadList();
            }
            catch (Exception e){
                Debug.LogException(e);
            }
        }

        private void HandleSelectionChanged(SaveBullet selectedBullet) => UpdateLoadButtonState(selectedBullet != null);

        private void UpdateLoadButtonState(bool hasSelection) => loadButton.interactable = hasSelection;

    }
}
