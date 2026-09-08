using UnityEngine;

namespace Core.Dialog{
    /// Central persistent proxy holding global, session-wide dialogue variables.
    /// Lives in the persistent GameSession scene and inherits from TrackedBehaviour
    /// to synchronize directly with the Blackboard and SaveSystem across all levels.
    [DisallowMultipleComponent]
    public class SessionDialogVariables : TrackedBehaviour{
        /// Active session singleton instance accessible globally.
        public static SessionDialogVariables Instance { get; private set; }

        private void Awake(){
            Instance = this;
        }

        private void OnDestroy(){
            if (Instance == this)
                Instance = null;
        }
    }
}
