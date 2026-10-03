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
        public static System.Action<TacticalZone[], TacticalSlot[]> OnEditorCook;
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
                TacticalZone closestZone = slot.GetComponentInParent<TacticalZone>();
                if (closestZone == null){
                    float shortestDistSqr = float.MaxValue;
                    foreach (TacticalZone zone in zones){
                        float distSqr = (zone.Center - slot.Position).sqrMagnitude;
                        if (distSqr < shortestDistSqr){
                            shortestDistSqr = distSqr;
                            closestZone     = zone;
                        }
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
            OnEditorCook?.Invoke(zones, slots);
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

                for (int i = 0; i < triangulation.indices.Length; i += 3){
                    Vector3 v0 = triangulation.vertices[triangulation.indices[i]];
                    Vector3 v1 = triangulation.vertices[triangulation.indices[i + 1]];
                    Vector3 v2 = triangulation.vertices[triangulation.indices[i + 2]];
                    Vector3 triCenter = (v0 + v1 + v2) / 3f;

                    TacticalZone ownerZone = zones.OrderBy(z => (z.Center - triCenter).sqrMagnitude).First();
                    int zoneIndex = zones.IndexOf(ownerZone);
                    while (_cachedZonePolygons.Count <= zoneIndex){
                        Color zoneColor = Color.HSVToRGB((_cachedZonePolygons.Count * 0.23f) % 1f, 0.7f, 0.85f);
                        zoneColor.a = 0.15f;
                        _cachedZonePolygons.Add((zoneColor, new List<Vector3[]>()));
                    }

                    _cachedZonePolygons[zoneIndex].polygons.Add(new[] { v0, v1, v2 });
                }
            }

            foreach ((Color color, List<Vector3[]> polygons) in _cachedZonePolygons){
                Gizmos.color = color;
                foreach (Vector3[] tri in polygons)
                    Gizmos.DrawMesh(CreateTriangleMesh(tri[0], tri[1], tri[2]));
            }
        }

        private static Mesh CreateTriangleMesh(Vector3 v0, Vector3 v1, Vector3 v2){
            Mesh mesh = new();
            mesh.vertices = new[] { v0, v1, v2 };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }
#endif
    }
}
