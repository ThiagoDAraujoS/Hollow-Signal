using UnityEngine;

namespace World {
    /// Represents a specific, pre-defined location in the world for entities to navigate towards or spawn at.
    public class AnchorPoint : MonoBehaviour {
        /// The universally unique identifier for this specific anchor point.
        public string anchorId;

        /// Provides the exact world position of this anchor.
        public Vector3 Position => transform.position;
        
        private void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}