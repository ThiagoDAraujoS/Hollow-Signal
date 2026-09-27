using System.Collections.Generic;
using System.Linq;
using Core.Attributes;
using Data.Effects;
using UnityEngine;
using UnityEngine.AI;
using World.Actors.Player;

namespace World.Tactical{
    /// Represents a tactical area node grouping standing slots and defining combat traversal.
    [SelectionBase]
    public class TacticalZone : MonoBehaviour{
        public static readonly List<TacticalZone> AllZones = new();

        [Header("Identity")] [SerializeField] private string zoneDisplayName;

        [Header("Adjacency Graph (Auto-Cooked)")] [SerializeField]
        private List<TacticalZone> adjacentZones = new();

        [Header("Slots (Auto-Cooked)")] [SerializeField]
        private List<TacticalSlot> childSlots = new();

        [Header("Area Transition Effects")]
        [RequireInterface(typeof(IEffect))]
        [SerializeField] private List<Object> onEnterEffects = new();
        [RequireInterface(typeof(IEffect))]
        [SerializeField] private List<Object> onExitEffects  = new();

        public string                      ZoneDisplayName => zoneDisplayName;
        public IReadOnlyList<TacticalZone> AdjacentZones   => adjacentZones;
        public IReadOnlyList<TacticalSlot> ChildSlots      => childSlots;
        public IReadOnlyList<Object>       OnEnterEffects  => onEnterEffects;
        public IReadOnlyList<Object>       OnExitEffects   => onExitEffects;
        public Vector3                     Center          => transform.position;

        /// Registers active zone instance in global registry.
        private void OnEnable(){
            if (!AllZones.Contains(this))
                AllZones.Add(this);
        }

        /// Unregisters active zone instance from global registry.
        private void OnDisable() => AllZones.Remove(this);

        /// Snaps zone epicenter to NavMesh and auto-cooks links if needed.
        private void Awake(){
            SnapToNavMesh();
            if (childSlots.Count == 0 || adjacentZones.Count == 0)
                CookAllZones();
        }

        /// Snaps zone epicenter to NavMesh on start.
        private void Start() => SnapToNavMesh();

        /// Automatically finds all zones and slots, binds slots to their closest zone, and builds the adjacency graph.
        [ContextMenu("Cook Tactical Zones & Slots")]
        public static void CookAllZones(){
            TacticalZone[] zones = FindObjectsByType<TacticalZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TacticalSlot[] slots = FindObjectsByType<TacticalSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (zones.Length == 0)
                return;

            foreach (TacticalZone zone in zones){
                zone.SnapToNavMesh();
                zone.childSlots.Clear();
                zone.adjacentZones.Clear();
            }

            foreach (TacticalSlot slot in slots){
                slot.SnapToNavMesh();
                TacticalZone closestZone = null;
                float shortestDistSqr    = float.MaxValue;
                foreach (TacticalZone zone in zones){
                    float distSqr = (zone.Center - slot.Position).sqrMagnitude;
                    if (distSqr < shortestDistSqr){
                        shortestDistSqr = distSqr;
                        closestZone     = zone;
                    }
                }

                if (closestZone == null)
                    continue;

                slot.ParentZone = closestZone;
                closestZone.childSlots.Add(slot);
            }

            NavMeshPath path = new();
            foreach (TacticalZone zone in zones){
                List<(TacticalZone neighbor, float distance)> candidates = new();
                foreach (TacticalZone other in zones){
                    if (other == zone)
                        continue;

                    if (NavMesh.CalculatePath(zone.Center, other.Center, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete){
                        float dist = TacticalSpatialResolver.CalculatePathDistance(path);
                        candidates.Add((other, dist));
                    }
                }

                foreach ((TacticalZone neighbor, float _) in candidates.OrderBy(c => c.distance).Take(4))
                    if (!zone.adjacentZones.Contains(neighbor))
                        zone.adjacentZones.Add(neighbor);
            }
        }

        /// Snaps zone epicenter transform position to the nearest valid point on the NavMesh.
        public void SnapToNavMesh(){
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                transform.position = hit.position;
        }

        /// Appends zone enter effects into a turn plan at the specified world coordinate.
        public void AddEnterEffectsToPlan(TurnPlanTrack plan, Vector3 position, CharacterSheet userSheet){
            EffectContext context = new(userSheet);
            foreach (Object obj in onEnterEffects)
                if (obj is IEffect effect)
                    plan.AddMilestone(position, effect, context);
        }

        /// Appends zone exit effects into a turn plan at the specified world coordinate.
        public void AddExitEffectsToPlan(TurnPlanTrack plan, Vector3 position, CharacterSheet userSheet){
            EffectContext context = new(userSheet);
            foreach (Object obj in onExitEffects)
                if (obj is IEffect effect)
                    plan.AddMilestone(position, effect, context);
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
