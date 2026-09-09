using System;
using UnityEngine;

namespace Core.UI {
    /// Static event hub broadcasting menu panel and viewpoint transitions.
    /// Completely decoupled from scene logic, cameras, and game state.
    public static class MenuEvents {
        public static event Action<MenuViewState> OnMenuViewStateChanged;

        /// Flares the active menu viewpoint to all subscribed observers.
        public static void SetViewState(MenuViewState state) => OnMenuViewStateChanged?.Invoke(state);

        /// Overload allowing integer-based triggers from UnityEvents and UI buttons.
        public static void SetViewState(int stateIndex) => OnMenuViewStateChanged?.Invoke((MenuViewState)stateIndex);
    }

    /// Helper component attached to UI panels or buttons to broadcast viewpoint changes.
    /// Automatically flares when enabled, or can be triggered via UnityEvents or Animators.
    [DisallowMultipleComponent]
    public class MenuStateNotifier : MonoBehaviour {
        [SerializeField] private MenuViewState viewStateOnEnable;
        [SerializeField] private bool notifyOnEnable = true;

        private void OnEnable() {
            if (notifyOnEnable)
                MenuEvents.SetViewState(viewStateOnEnable);
        }

        /// Broadcasts an integer-mapped viewpoint state (0=Main, 1=Load, 2=Settings, 3=Credits).
        public void NotifyState(int stateIndex) => MenuEvents.SetViewState(stateIndex);

        /// Broadcasts an explicit enum viewpoint state.
        public void NotifyState(MenuViewState state) => MenuEvents.SetViewState(state);
    }
}
