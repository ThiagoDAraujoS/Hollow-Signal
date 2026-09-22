using Core.State;
using UnityEngine;

namespace Narrative.Dialog{
    /// Central persistent proxy holding global, session-wide dialogue variables.
    /// Lives in the persistent GameSession scene and inherits from TrackedBehaviour
    /// to synchronize directly with the Blackboard and SaveSystem across all levels.
    [DisallowMultipleComponent]
    public class SessionDialogVariables : TrackedBehaviour{
        /// Active session singleton instance accessible globally.
        public static SessionDialogVariables Instance { get; private set; }

        /// Registers active session instance on wake.
        private void Awake() => Instance = this;

        /// Unregisters active session instance on destruction.
        private void OnDestroy() => Instance = Instance == this ? null : Instance;
    }
}
