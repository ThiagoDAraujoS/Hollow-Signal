using Data.Effects;
using UnityEngine;

namespace Test{
    /// Simple test effect to verify ExplorationSlot arrival and execution handshake.
    [CreateAssetMenu(fileName = "ExplorationSlotTestEffect", menuName = "CRPG/Test/Exploration Slot Test Effect")]
    public class ExplorationSlotTester : EffectNode{
        [Header("Debug State")]
        [SerializeField] private int interactionCount;
        [SerializeField] private string lastInteractedBy;

        /// Logs arrival and increments interaction count when executed.
        public override void Run(EffectContext context){
            interactionCount++;
            lastInteractedBy = context.Character != null ? context.Character.name : "Unknown";
            Debug.Log($"[ExplorationSlotTester] {lastInteractedBy} interacted! (Total: {interactionCount})");
        }
    }
}
