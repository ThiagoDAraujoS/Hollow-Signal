using UnityEngine;

namespace Core.Dialog{
    /// Base abstract proxy holding map-wide dialogue variables for an active level.
    /// Each specific map implements a concrete subclass containing its unique Tracked fields.
    /// Exactly one instance exists per active map scene.
    [DisallowMultipleComponent]
    public abstract class MapDialogVariables : TrackedBehaviour{
        /// Active map singleton instance accessible across the current level.
        public static MapDialogVariables Instance { get; private set; }

        protected virtual void Awake(){
            Instance = this;
        }

        protected virtual void OnDestroy(){
            if (Instance == this)
                Instance = null;
        }
    }
}
