using UnityEngine;

namespace World.Tactical{
    /// Represents a discrete standing spot and interaction anchor outside combat.
    public sealed class ExplorationSlot : AreaSlot{
        [Header("Exploration Interaction")]
        [SerializeField] private string interactionPrompt = "Interact";

        public string InteractionPrompt => interactionPrompt;

        /// Draws editor gizmo with distinctive exploration colors.
        protected override void OnDrawGizmos(){
            Gizmos.color = linkedEffect != null ? new Color(0.2f, 0.9f, 0.4f) : new Color(0.9f, 0.7f, 0.2f);
            Vector3 pos = Position;
            Gizmos.DrawWireSphere(pos, 0.35f);
            Gizmos.DrawRay(pos, Rotation * Vector3.forward * 0.7f);
        }
    }
}
