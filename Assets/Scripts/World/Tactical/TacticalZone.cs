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

#if UNITY_EDITOR
        private static bool                                                  _navMeshCacheDirty = true;
        private static readonly List<(Color color, List<Vector3[]> polygons)> _cachedZonePolygons = new();
        private static readonly List<(Vector3 start, Vector3 end)>           _cachedBoundaryEdges = new();
#endif

        public IReadOnlyList<TacticalZone> AdjacentZones   => adjacentZones;
        public IReadOnlyList<TacticalSlot> ChildSlots      => childSlots;
        public IReadOnlyList<Object>       OnEnterEffects  => onEnterEffects;
        public IReadOnlyList<Object>       OnExitEffects   => onExitEffects;
        public Vector3                     Center          => transform.position;

        /// Registers active zone instance in global registry.
        private void OnEnable(){
            if (!AllZones.Contains(this))
                AllZones.Add(this);
#if UNITY_EDITOR
            _navMeshCacheDirty = true;
#endif
        }

        /// Unregisters active zone instance from global registry.
        private void OnDisable(){
            AllZones.Remove(this);
#if UNITY_EDITOR
            _navMeshCacheDirty = true;
#endif
        }

#if UNITY_EDITOR
        /// Marks NavMesh Voronoi visualization cache dirty when inspector values change.
        private void OnValidate() => _navMeshCacheDirty = true;
#endif

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
            _navMeshCacheDirty = true;
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
        public void ToggleVoronoiGizmos(){
            showVoronoiGizmos = !showVoronoiGizmos;
#if UNITY_EDITOR
            _navMeshCacheDirty = true;
#endif
        }

        /// Draws editor gizmos showing zone center, slot ownership, and exact 3D NavMesh Voronoi domains.
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

#if UNITY_EDITOR
            if (transform.hasChanged){
                transform.hasChanged = false;
                _navMeshCacheDirty   = true;
            }

            List<TacticalZone> zones = Application.isPlaying ? AllZones : FindObjectsByType<TacticalZone>(FindObjectsSortMode.None).ToList();
            if (zones.Count > 0 && zones[0] == this)
                DrawAllNavMeshVoronoiCells(zones);
#endif
        }

#if UNITY_EDITOR
        /// Renders exact 3D Voronoi domains painted directly onto baked NavMesh geometry.
        private static void DrawAllNavMeshVoronoiCells(List<TacticalZone> zones){
            if (_navMeshCacheDirty || _cachedZonePolygons.Count != zones.Count){
                _cachedZonePolygons.Clear();
                _cachedBoundaryEdges.Clear();
                _navMeshCacheDirty = false;

                NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
                if (triangulation.vertices == null || triangulation.indices == null || triangulation.indices.Length == 0)
                    return;

                Vector3[] vertices = triangulation.vertices;
                int[]     indices  = triangulation.indices;
                int       triCount = indices.Length / 3;

                for (int z = 0; z < zones.Count; z++){
                    TacticalZone currentZone = zones[z];
                    if (currentZone == null)
                        continue;

                    Color zoneColor = Color.HSVToRGB((z * 0.61803398875f) % 1f, 0.65f, 0.95f);
                    zoneColor.a = 0.28f;
                    List<Vector3[]> zonePolys = new();
                    Vector3         zCenter   = currentZone.Center;

                    for (int t = 0; t < triCount; t++){
                        Vector3 v0 = vertices[indices[t * 3]];
                        Vector3 v1 = vertices[indices[t * 3 + 1]];
                        Vector3 v2 = vertices[indices[t * 3 + 2]];

                        Vector3 triCenter = (v0 + v1 + v2) / 3f;
                        if ((triCenter - zCenter).sqrMagnitude > 2500f)
                            continue;

                        List<Vector3> poly = new(){ v0, v1, v2 };

                        foreach (TacticalZone other in zones){
                            if (other == null || other == currentZone)
                                continue;

                            Vector3 oCenter    = other.Center;
                            Vector3 planePoint = (zCenter + oCenter) * 0.5f;
                            Vector3 normal     = oCenter - zCenter;

                            poly = ClipPolygon3D(poly, planePoint, normal);
                            if (poly.Count < 3)
                                break;
                        }

                        if (poly.Count < 3)
                            continue;

                        Vector3 normal3D = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                        if (normal3D.y < 0f)
                            normal3D = -normal3D;
                        Vector3 lift = (normal3D.sqrMagnitude > 0.01f ? normal3D : Vector3.up) * 0.02f;

                        Vector3[] liftedPoly = new Vector3[poly.Count];
                        for (int p = 0; p < poly.Count; p++)
                            liftedPoly[p] = poly[p] + lift;

                        zonePolys.Add(liftedPoly);

                        for (int p = 0; p < liftedPoly.Length; p++){
                            Vector3 p1 = liftedPoly[p];
                            Vector3 p2 = liftedPoly[(p + 1) % liftedPoly.Length];
                            Vector3 mid = (p1 + p2) * 0.5f;

                            float dCurrent = Vector3.Distance(mid, zCenter);
                            foreach (TacticalZone other in zones){
                                if (other == null || other == currentZone)
                                    continue;
                                float dOther = Vector3.Distance(mid, other.Center);
                                if (Mathf.Abs(dCurrent - dOther) < 0.15f){
                                    _cachedBoundaryEdges.Add((p1, p2));
                                    break;
                                }
                            }
                        }
                    }

                    _cachedZonePolygons.Add((zoneColor, zonePolys));
                }
            }

            foreach ((Color color, List<Vector3[]> polys) in _cachedZonePolygons){
                UnityEditor.Handles.color = color;
                foreach (Vector3[] poly in polys)
                    UnityEditor.Handles.DrawAAConvexPolygon(poly);
            }

            Gizmos.color = Color.cyan;
            foreach ((Vector3 start, Vector3 end) in _cachedBoundaryEdges)
                Gizmos.DrawLine(start, end);
        }

        /// Clips a 3D convex polygon against a half-plane defined by a point on the plane and outward normal direction.
        private static List<Vector3> ClipPolygon3D(List<Vector3> poly, Vector3 planePoint, Vector3 normal){
            List<Vector3> output = new();
            for (int i = 0; i < poly.Count; i++){
                Vector3 current = poly[i];
                Vector3 next    = poly[(i + 1) % poly.Count];
                float   dCurr   = Vector3.Dot(current - planePoint, normal);
                float   dNext   = Vector3.Dot(next - planePoint, normal);
                bool    currIn  = dCurr <= 0.001f;
                bool    nextIn  = dNext <= 0.001f;

                if (currIn)
                    output.Add(current);

                if (currIn != nextIn){
                    float diff = dCurr - dNext;
                    float t    = Mathf.Abs(diff) > 1e-6f ? Mathf.Clamp01(dCurr / diff) : 0.5f;
                    output.Add(Vector3.Lerp(current, next, t));
                }
            }
            return output;
        }
#endif
    }
}
