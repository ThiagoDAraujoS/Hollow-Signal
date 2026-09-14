using System;
using System.Collections.Generic;
using Core.Managers;
using UnityEngine;

namespace UI.Shared.SaveLoad{
    /// Coordinates the Save Game panel, managing the carousel of saves,
    /// generating automatic save names with character info and timestamp, and executing saves.
    public class SaveGamePanelController : BaseGameFilePanelController{
        public const string NewSaveSlotId = "NEW_SAVE";

        protected override void Awake(){
            base.Awake();
            carouselController.PreserveFirstItem = true;
        }

        /// Populates the carousel with existing save slots, preserving the New Save bullet at index 0.
        public override void BuildList(){
            List<SaveFileMetadata>   saveFiles      = SaveSystem.GetSaveFileList();
            List<GameFileBulletData> bulletDataList = new();
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
            carouselController.BuildCarousel(bulletDataList);
            UpdateActionButtonState(carouselController.SelectedBullet != null);
        }

        /// Saves the game to an auto-named slot (for New Save) or overwrites the selected slot.
        protected override async void HandleActionClicked(){
            try{
                if (carouselController.SelectedBullet == null) return;

                actionButton.interactable = false;

                string targetSlot = carouselController.SelectedBullet.SlotName;
                bool isNewSave = string.IsNullOrEmpty(targetSlot)
                              || targetSlot == NewSaveSlotId
                              || carouselController.SelectedBullet.name.Contains("NewSave");

                if (isNewSave){
                    string charName = GameSessionManager.Instance.mainCharacterName.Value;
                    targetSlot = $"{charName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                }

                Debug.Log($"[SavePanelController] Saving game to slot: {targetSlot}");
                SaveSystem.SetSaveSlot(targetSlot);
                await SaveSystem.SaveGame();

                BuildList();
                UpdateActionButtonState(carouselController.SelectedBullet != null);
            }
            catch (Exception e){
                Debug.LogException(e);
            }
        }

        // --- Animation Track & Inspector Compatibility Aliases ---
        [ContextMenu("Build Save List")]
        public void BuildSaveList() => BuildList();

        [ContextMenu("Destroy Save List")]
        public void DestroySaveList() => DestroyList();

        public void HandleSaveClicked() => HandleActionClicked();
    }
}
