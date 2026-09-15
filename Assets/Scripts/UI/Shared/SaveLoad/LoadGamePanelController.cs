using System;
using Core.Managers;
using UI.BootMenu;
using UnityEngine;

namespace UI.Shared.SaveLoad{
    /// Coordinates the Load Game panel, populating bullets and loading game on request.
    public class LoadGamePanelController : BaseGameFilePanelController{
        /// Populates save list on start.
        private void Start() => BuildList();

        /// Populates carousel with formatted save files from BootMenuManager.
        public override void BuildList() => carouselController.BuildCarousel(BootMenuManager.FetchSaveBulletData());

        /// Triggers game loading from the specified save bullet.
        protected override async void HandleAction(GameFileBullet bullet){
            if (bullet == null) return;
            try{
                string slotToLoad = bullet.SlotName;
                Debug.Log($"[LoadPanelController] Loading save slot: {slotToLoad}");
                await SceneCoordinator.StartGameSessionAsync(slotToLoad);
            }
            catch (Exception e){
                Debug.LogException(e);
            }
        }
    }
}
