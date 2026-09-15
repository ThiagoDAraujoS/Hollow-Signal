using System;
using System.Collections.Generic;
using Core.Managers;
using TMPro;
using UI.Shared.SaveLoad;
using UnityEngine;
using UnityEngine.UI;

namespace UI.BootMenu{
    /// Manages boot menu actions including starting a new game, continuing, loading, deleting, and updating button states.
    [DisallowMultipleComponent]
    public class BootMenuManager : MonoBehaviour{
        [SerializeField] private Button                  continueButton;
        [SerializeField] private TextMeshProUGUI         continueText;
        [SerializeField] private Color                   activeTextColor   = Color.white;
        [SerializeField] private Color                   disabledTextColor = new(0.314f, 0.314f, 0.314f, 1f);
        [SerializeField] private LoadGamePanelController loadGamePanel;

        /// Evaluates available save files and initializes continue button state.
        private void Start() => RefreshContinueButton();

        /// Refreshes continue button interactability and subscribes to panel delete events.
        private void OnEnable(){
            RefreshContinueButton();
            if (loadGamePanel != null)
                loadGamePanel.OnSaveDeleted += HandleSaveDeleted;
        }

        /// Unsubscribes from panel delete events.
        private void OnDisable(){
            if (loadGamePanel != null)
                loadGamePanel.OnSaveDeleted -= HandleSaveDeleted;
        }

        /// Starts a fresh game session using the default save template.
        public async void StartNewGame(){
            try{
                await SaveSystem.StartNewGameAsync();
            }
            catch (Exception e){
                Debug.LogException(e);
            }
        }

        /// Continues game session from the most recent save file.
        public async void ContinueGame(){
            try{
                await SaveSystem.ContinueGameAsync();
            }
            catch (Exception e){
                Debug.LogException(e);
            }
        }

        /// Loads the save file currently selected in the load game panel.
        public void LoadSelectedGame() => loadGamePanel.HandleActionButtonClicked();

        /// Deletes the save file currently selected in the load game panel.
        public void DeleteSelectedGame() => loadGamePanel.DeleteSelected();

        /// Closes the application or exits play mode in editor.
        public void QuitGame(){
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// Updates continue button interactability and text color based on save files.
        public void RefreshContinueButton(){
            bool hasSaves = SaveSystem.GetSaveFileList().Count > 0;
            continueButton.interactable = hasSaves;
            continueText.color          = hasSaves ? activeTextColor : disabledTextColor;
        }

        /// Refreshes continue button when a save is deleted from the panel.
        private void HandleSaveDeleted(string slotName) => RefreshContinueButton();

        /// Fetches all save files from SaveSystem formatted for carousel bullets.
        public static List<GameFileBulletData> FetchSaveBulletData(){
            List<GameFileBulletData> bulletDataList = new();
            foreach (SaveFileMetadata meta in SaveSystem.GetSaveFileList())
                bulletDataList.Add(new GameFileBulletData{
                    slotName      = meta.slotName,
                    location      = string.IsNullOrEmpty(meta.location) ? "Somewhere" : meta.location,
                    timestamp     = meta.lastSaveTime.ToString("yyyy-MM-dd HH:mm"),
                    snapshot      = null,
                    characterName = string.IsNullOrEmpty(meta.characterName) ? "Nameless Hero" : meta.characterName
                });
            return bulletDataList;
        }
    }
}
