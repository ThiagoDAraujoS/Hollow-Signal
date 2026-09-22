using UnityEngine;
using World;
using World.Actors.Player;
using World.Tactical;

namespace Test{
    /// Simple test interactable to verify ExplorationSlot pathing, rotation, and arrival handshake.
    [DisallowMultipleComponent]
    public class ExplorationSlotTester : MonoBehaviour, IUsable{
        [Header("Slot Reference")]
        [SerializeField] private ExplorationSlot slot;

        [Header("Debug State")]
        [SerializeField] private int interactionCount;
        [SerializeField] private string lastInteractedBy;

        public Vector3    UsePosition => slot != null ? slot.Position : transform.position;
        public Quaternion UseRotation => slot != null ? slot.Rotation : transform.rotation;

        /// Resolves sibling ExplorationSlot reference on reset.
        private void Reset() => slot = GetComponent<ExplorationSlot>();

        /// Logs arrival and increments interaction count when used by a character.
        public void Use(CharacterSheet whosUsing){
            interactionCount++;
            lastInteractedBy = whosUsing.name;
            Debug.Log($"[ExplorationSlotTester] {lastInteractedBy} interacted with '{gameObject.name}'! (Total: {interactionCount})");
        }
    }
}
