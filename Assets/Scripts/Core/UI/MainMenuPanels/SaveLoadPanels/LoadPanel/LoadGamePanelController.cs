using System;
using System.Collections.Generic;
using Core.Managers;
using UnityEngine;

namespace Core.UI.MainMenuPanels.SaveLoadPanels.LoadPanel{
    /// Coordinates the Load Game panel, fetching save data from SaveSystem,
    /// populating the SaveCarouselController, and triggering game loading through SceneCoordinator.
    public class LoadGamePanelController : BaseGameFilePanelController{
        private void Start(){
            BuildList();
        }

        /// Fetches all save files from SaveSystem and passes formatted bullet data to the carousel.
        public override void BuildList(){
            List<GameFileBulletData> bulletDataList = new();

            if (SaveSystem.Instance != null){
                List<SaveFileMetadata> saveFiles = SaveSystem.GetSaveFileList();
                foreach (SaveFileMetadata meta in saveFiles){
                    GameFileBulletData bulletData = new(){
                        slotName      = meta.slotName,
                        location      = string.IsNullOrEmpty(meta.location) ? "Somewhere" : meta.location,
                        timestamp     = meta.lastSaveTime.ToString("yyyy-MM-dd HH:mm"),
                        snapshot      = null,
                        characterName = string.IsNullOrEmpty(meta.characterName) ? "Nameless Hero" : meta.characterName
                    };
                    bulletDataList.Add(bulletData);
                }
            }
            else{
                Debug.LogWarning("[LoadGamePanelController] SaveSystem.Instance is null. Populating mock save bullets for editor testing.");
                for (int i = 1; i <= 3; i++){
                    bulletDataList.Add(new GameFileBulletData{
                        slotName      = $"Slot_{i:D2}",
                        location      = $"Sector {i * 4} - Sublevel B",
                        timestamp     = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd HH:mm"),
                        snapshot      = null,
                        characterName = $"Operator {i}"
                    });
                }
            }

            if (carouselController != null){
                carouselController.BuildCarousel(bulletDataList);
                UpdateActionButtonState(carouselController.SelectedBullet != null);
            }
        }

        /// Triggers game loading using the currently selected save slot in the carousel.
        protected override async void HandleActionClicked(){
            try{
                if (carouselController == null || carouselController.SelectedBullet == null) return;

                string slotToLoad = carouselController.SelectedBullet.SlotName;
                Debug.Log($"[LoadPanelController] Loading save slot: {slotToLoad}");
                if (actionButton != null) actionButton.interactable = false;

                await SceneCoordinator.StartGameSessionAsync(slotToLoad);
                DestroyList();
            }
            catch (Exception e){
                Debug.LogException(e);
            }
        }

        // --- Animation Track & Inspector Compatibility Aliases ---
        [ContextMenu("Build Load List")]
        public void BuildLoadList() => BuildList();

        [ContextMenu("Destroy Load List")]
        public void DestroyLoadList() => DestroyList();

        public void HandleLoadClicked() => HandleActionClicked();
    }
}
