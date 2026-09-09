using System;
using Core.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.UI {
    /// Coordinates UI panel transitions by driving an Animator controller 
    /// and flaring MenuEvents for external observers (like the 3D diorama camera).
    [DisallowMultipleComponent]
    public class MenuAnimatorController : MonoBehaviour {
        [SerializeField] private Animator animator;

        private static readonly int PANEL_INDEX_HASH = Animator.StringToHash("PanelIndex");

        private void Awake() {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInParent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable() {
            SceneCoordinator.OnTitleSceneLoaded += HandleTitleSceneLoaded;
            SceneCoordinator.OnTitleSceneUnloaded += HandleTitleSceneUnloaded;
        }

        private void OnDisable() {
            SceneCoordinator.OnTitleSceneLoaded -= HandleTitleSceneLoaded;
            SceneCoordinator.OnTitleSceneUnloaded -= HandleTitleSceneUnloaded;
        }

        private void HandleTitleSceneLoaded(Scene scene) => OpenMainMenu();

        private void HandleTitleSceneUnloaded() => CloseMenu();

        // --- 0-Argument Button Handlers ---

        /// Transitions to the Main Menu panel (Index 0).
        public void OpenMainMenu() => TransitionTo(MenuViewState.Main);

        /// Transitions to the Load Game panel (Index 1).
        public void OpenLoadMenu() => TransitionTo(MenuViewState.Load);

        /// Transitions to the Save Game panel (Index 2).
        public void OpenSaveMenu() => TransitionTo(MenuViewState.Save);

        /// Transitions to the Settings panel (Index 3).
        public void OpenSettingsMenu() => TransitionTo(MenuViewState.Settings);

        /// Transitions to the Credits panel (Index 4).
        public void OpenCreditsMenu() => TransitionTo(MenuViewState.Credits);

        /// Closes the menu completely, transitioning to the hidden state (Index 5).
        public void CloseMenu() => TransitionTo(MenuViewState.Closed);

        /// Starts a new game session using the default template.
        public async void StartNewGame() {
            CloseMenu();
            await SceneCoordinator.StartGameSessionAsync(SaveSystem.DefaultSaveTemplate);
        }

        /// Closes application or exits play mode in editor.
        public void QuitGame() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void TransitionTo(MenuViewState state) {
            if (animator != null) {
                if ((int)state == animator.GetInteger(PANEL_INDEX_HASH)) return;
                animator.SetInteger(PANEL_INDEX_HASH, (int)state);
            }

            MenuEvents.SetViewState(state);
        }
    }
}
