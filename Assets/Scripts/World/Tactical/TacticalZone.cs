using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace World.Tactical{
    /// Represents a tactical area node grouping standing slots and defining combat traversal.
    [SelectionBase]
    public class TacticalZone : MonoBehaviour{
        [Header("Identity")]
        [SerializeField] private string zoneDisplayName;

        [Header("Adjacency Graph")]
        [SerializeField] private List<TacticalZone> adjacentZones = new();

        [Header("Slots")]
        [SerializeField] private List<TacticalSlot> childSlots = new();

        public string ZoneDisplayName => zoneDisplayName;
        public IReadOnlyList<TacticalZone> AdjacentZones => adjacentZones;
        public IReadOnlyList<TacticalSlot> ChildSlots => childSlots;

        /// Discovers and registers child slots on awake.
        private void Awake() => RegisterChildSlots();

        /// Discovers and registers child slots on reset.
        private void Reset() => RegisterChildSlots();

        /// Automatically finds child slots and sets their parent zone reference.
        public void RegisterChildSlots(){
            childSlots.Clear();
            childSlots.AddRange(GetComponentsInChildren<TacticalSlot>(true));
            foreach (TacticalSlot slot in childSlots)
                slot.ParentZone = this;
        }

        /// Finds the best available fallback slot closest to a clicked world point.
        public TacticalSlot GetBestAvailableFallbackSlot(Vector3 worldClickPosition){
            List<TacticalSlot> available = childSlots.Where(s => s.IsAvailable).ToList();
            if (available.Count == 0)
                return null;

            List<TacticalSlot> plainSlots = available.Where(s => !s.IsFeatured).ToList();
            if (plainSlots.Count > 0)
                return plainSlots.OrderBy(s => Vector3.Distance(s.Position, worldClickPosition)).First();

            return available.OrderBy(s => Vector3.Distance(s.Position, worldClickPosition)).First();
        }

        /// Activates visual rings on all featured child slots when hovering this zone.
        public void OnZoneHoverEnter(){
            foreach (TacticalSlot slot in childSlots)
                if (slot.IsFeatured && slot.IsAvailable)
                    slot.SetVisualActive(true);
        }

        /// Deactivates visual rings on all child slots when leaving this zone.
        public void OnZoneHoverExit(){
            foreach (TacticalSlot slot in childSlots)
                slot.SetVisualActive(false);
        }

        /// Draws editor gizmos showing zone center and adjacency connections.
        private void OnDrawGizmosSelected(){
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            foreach (TacticalZone neighbor in adjacentZones)
                if (neighbor != null)
                    Gizmos.DrawLine(center, neighbor.transform.position);
        }
    }
}
