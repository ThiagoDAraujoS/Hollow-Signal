using System;
using System.Collections.Generic;
using Core.Managers;
using UnityEngine;

namespace UI.Shared.SaveLoad{
    /// Coordinates the Save Game panel, managing carousel saves and executing saves on bullet click.
    public class SaveGamePanelController : BaseGameFilePanelController{
        public const string NewSaveSlotId = "NEW_SAVE";

        /// Configures carousel to preserve the New Save bullet slot.
        protected override void Awake(){
            base.Awake();
            carouselController.PreserveFirstItem = true;
        }

        /// Populates the carousel with existing save slots, preserving the New Save bullet at index 0.
        public override void BuildList(){
            List<GameFileBulletData> bulletDataList = new();
            foreach (SaveFileMetadata meta in SaveSystem.GetSaveFileList())
                bulletDataList.Add(new GameFileBulletData{
                    slotName      = meta.slotName,
                    location      = string.IsNullOrEmpty(meta.location) ? "Somewhere" : meta.location,
                    timestamp     = meta.lastSaveTime.ToString("yyyy-MM-dd HH:mm"),
                    snapshot      = null,
                    characterName = string.IsNullOrEmpty(meta.characterName) ? "Nameless Hero" : meta.characterName
                });

            carouselController.BuildCarousel(bulletDataList);
        }

        /// Saves the game to an auto-named slot or overwrites the clicked slot by committing the active session.
        protected override async void HandleAction(GameFileBullet bullet){
            try{
                string targetSlot = bullet.SlotName;
                bool isNewSave = string.IsNullOrEmpty(targetSlot) || targetSlot == NewSaveSlotId || bullet.name.Contains("NewSave");

                if (isNewSave){
                    string charName = GameSessionManager.Instance.mainCharacterName.Value;
                    targetSlot = $"{charName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                }

                Debug.Log($"[SavePanelController] Saving game to slot: {targetSlot}");
                await SaveSystem.SaveGame(targetSlot);

                BuildList();
            }
            catch (Exception e){
                Debug.LogException(e);
            }
        }
    }
}
