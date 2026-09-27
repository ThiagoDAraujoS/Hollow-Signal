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

        [Header("Adjacency Graph (Auto-Cooked)")] [SerializeField]
        private List<TacticalZone> adjacentZones = new();

        [Header("Slots (Auto-Cooked)")] [SerializeField]
        private List<TacticalSlot> childSlots = new();

        [Header("Area Transition Effects")]
        [RequireInterface(typeof(IEffect))]
        [SerializeField] private List<Object> onEnterEffects = new();
        [RequireInterface(typeof(IEffect))]
        [SerializeField] private List<Object> onExitEffects  = new();

        [Header("Gizmos")]
        [SerializeField] private static bool showVoronoiGizmos = true;

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

        /// Context menu trigger for cooking zones and slots from the inspector.
        [ContextMenu("Cook Tactical Zones & Slots")]
        public void CookFromContextMenu() => CookAllZones();

        /// Automatically finds all zones and slots, binds slots to their closest zone, and builds the adjacency graph.
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

#if UNITY_EDITOR
            foreach (TacticalZone zone in zones)
                UnityEditor.EditorUtility.SetDirty(zone);
            foreach (TacticalSlot slot in slots)
                UnityEditor.EditorUtility.SetDirty(slot);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
        }

        /// Snaps zone epicenter transform position to the nearest valid point on the NavMesh.
        public void SnapToNavMesh(){
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                transform.position = hit.position;
        }

        /// Finds the tactical zone whose Voronoi epicenter is closest to the given world point.
        public static TacticalZone GetZoneAt(Vector3 worldPoint){
            TacticalZone bestZone       = null;
            float        closestDistSqr = float.MaxValue;
            foreach (TacticalZone zone in AllZones){
                float distSqr = (zone.Center - worldPoint).sqrMagnitude;
                if (distSqr < closestDistSqr){
                    closestDistSqr = distSqr;
                    bestZone       = zone;
                }
            }
            return bestZone;
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

        /// Toggles display of Voronoi cell gizmos globally.
        [ContextMenu("Toggle Voronoi Gizmos")]
        public void ToggleVoronoiGizmos() => showVoronoiGizmos = !showVoronoiGizmos;

        /// Draws editor gizmos showing zone center, slot ownership, and closed 2D Voronoi cell boundaries.
        private void OnDrawGizmos(){
            if (!showVoronoiGizmos)
                return;

            Vector3 center = Center;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, 0.4f);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            foreach (TacticalSlot slot in childSlots)
                if (slot != null)
                    Gizmos.DrawLine(center, slot.Position);

            DrawVoronoiCell(center);
        }

        /// Computes and renders a closed convex Voronoi cell polygon on the horizontal plane.
        private void DrawVoronoiCell(Vector3 center){
            const float maxRadius            = 25f;
            const float floorHeightTolerance = 2.5f;

            List<Vector2> polygon = new(){
                new(center.x - maxRadius, center.z - maxRadius),
                new(center.x + maxRadius, center.z - maxRadius),
                new(center.x + maxRadius, center.z + maxRadius),
                new(center.x - maxRadius, center.z + maxRadius)
            };

            Vector2 c2D = new(center.x, center.z);
            IEnumerable<TacticalZone> zones = Application.isPlaying ? AllZones : FindObjectsByType<TacticalZone>(FindObjectsSortMode.None);

            foreach (TacticalZone other in zones){
                if (other == null || other == this)
                    continue;

                Vector3 otherCenter = other.Center;
                if (Mathf.Abs(otherCenter.y - center.y) > floorHeightTolerance)
                    continue;

                Vector2 o2D = new(otherCenter.x, otherCenter.z);
                Vector2 dir = o2D - c2D;
                if (dir.sqrMagnitude < 0.01f)
                    continue;

                Vector2 midpoint = (c2D + o2D) * 0.5f;
                polygon = ClipPolygonHalfPlane(polygon, midpoint, dir);
                if (polygon.Count < 3)
                    break;
            }

            if (polygon.Count < 3)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < polygon.Count; i++){
                Vector2 p1 = polygon[i];
                Vector2 p2 = polygon[(i + 1) % polygon.Count];
                Gizmos.DrawLine(new Vector3(p1.x, center.y + 0.05f, p1.y), new Vector3(p2.x, center.y + 0.05f, p2.y));
            }
        }

        /// Clips a 2D convex polygon against a half-plane defined by a point on the line and outward normal direction.
        private static List<Vector2> ClipPolygonHalfPlane(List<Vector2> poly, Vector2 pointOnLine, Vector2 normal){
            List<Vector2> output = new();
            for (int i = 0; i < poly.Count; i++){
                Vector2 current = poly[i];
                Vector2 next    = poly[(i + 1) % poly.Count];
                bool    currIn  = Vector2.Dot(current - pointOnLine, normal) <= 0f;
                bool    nextIn  = Vector2.Dot(next - pointOnLine, normal) <= 0f;

                if (currIn)
                    output.Add(current);

                if (currIn != nextIn){
                    Vector2 edge  = next - current;
                    float   denom = Vector2.Dot(edge, normal);
                    if (Mathf.Abs(denom) > 1e-5f){
                        float t = Vector2.Dot(pointOnLine - current, normal) / denom;
                        output.Add(current + edge * Mathf.Clamp01(t));
                    }
                }
            }
            return output;
        }
    }
}
