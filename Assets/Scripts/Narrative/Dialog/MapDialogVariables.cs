using Core.State;
using UnityEngine;

namespace Narrative.Dialog{
    /// Base abstract proxy holding map-wide dialogue variables for an active level.
    /// Each specific map implements a concrete subclass containing its unique Tracked fields.
    /// Exactly one instance exists per active map scene.
    [DisallowMultipleComponent]
    public abstract class MapDialogVariables : TrackedBehaviour{
        /// Active map singleton instance accessible across the current level.
        public static MapDialogVariables Instance { get; private set; }

        /// Registers active map instance on wake.
        protected virtual void Awake() => Instance = this;

        /// Unregisters active map instance on destruction.
        protected virtual void OnDestroy() => Instance = Instance == this ? null : Instance;
    }
}
