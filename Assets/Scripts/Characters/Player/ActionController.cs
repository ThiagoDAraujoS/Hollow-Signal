using Core.Board;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;


namespace Characters.Player{
    [DefaultExecutionOrder(100)]
    public class ActionController : Component{
        public Area       currentArea;
        public UnityEvent onOutOfTurnResources;
        public float      boundaryBuffer = 3.5f;
        
        public void OnOutOfTurnResources(){
            if (Sheet.HasAction || Sheet.HasMove) return;
            onOutOfTurnResources?.Invoke();
            TurnUser.PassTurn();
        }

        public void RefreshTurnResources(){
            Sheet.ap.Restore();
            Sheet.mp.Restore();
        }

        public void Awake(){
            Sheet.ap.onValueZeroed.AddListener(OnOutOfTurnResources);
            Sheet.mp.onValueZeroed.AddListener(OnOutOfTurnResources);
            TurnUser.onTurnStart.AddListener(OnTurnStart);
            TurnUser.onEnterTurnSystem.AddListener(OnTurnStart);
            OnTurnStart();
        }

        public void OnDestroy(){
            Sheet.ap.onValueZeroed.RemoveListener(OnOutOfTurnResources);
            Sheet.mp.onValueZeroed.RemoveListener(OnOutOfTurnResources);
            TurnUser.onTurnStart.RemoveListener(OnTurnStart);
            TurnUser.onEnterTurnSystem.RemoveListener(OnTurnStart);
        }
        
        public void OnTurnStart(){
            RefreshTurnResources();
            RecalculateReachableAreas();
        }
        
        private readonly HashSet<Area> _allowedAreas = new();

        public void RecalculateReachableAreas(){
            _allowedAreas.Clear();
            Queue<AreaNode> queue   = new();
            HashSet<Area>   visited = new();

            queue.Enqueue(new AreaNode(currentArea, 0));
            visited.Add(currentArea);
            _allowedAreas.Add(currentArea);

            while (queue.TryDequeue(out AreaNode current)){
                if (current.depth >= Sheet.mp) continue;
                foreach (Area neighbor in current.area.neighbors){
                    if (!visited.Add(neighbor)) continue;
                    _allowedAreas.Add(neighbor);
                    queue.Enqueue(new AreaNode(neighbor, current.depth + 1));
                }
            }

            string debug = _allowedAreas.Aggregate("", (current, area) => current + area.name + " ");
            Debug.Log(debug);
        }

        // Simple helper struct for the BFS queue
        private struct AreaNode{
            public readonly Area area;
            public readonly int  depth;

            public AreaNode(Area a, int d){
                area  = a;
                depth = d;
            }
        }

        public Vector3 ProcessMovementRequest(Vector3 intendedPoint){
            if (currentArea == null) return intendedPoint;

            // 1. VORONOI CHECK (Find the closest area)
            Area  targetArea  = currentArea;
            float closestDist = Vector3.Distance(intendedPoint, currentArea.transform.position);

            foreach (Area neighbor in currentArea.neighbors){
                float dist = Vector3.Distance(intendedPoint, neighbor.transform.position);
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                targetArea  = neighbor;
            }

            // 2. IF STAYING IN SAME AREA
            if (targetArea == currentArea) return intendedPoint;

            if (!_allowedAreas.Contains(targetArea))
                return CalculateClampedPosition(intendedPoint, targetArea);
            currentArea = targetArea;
            return intendedPoint;
        }
        
        private Vector3 CalculateClampedPosition(Vector3 intendedPoint, Area target)
        {
            // Anchors flattened to XZ (wall is vertical)
            Vector3 anchorArea = currentArea.transform.position; anchorArea.y = 0f;
            Vector3 goalArea   = target.transform.position;      goalArea.y   = 0f;

            // Keep original Y to restore later
            float   originalY    = intendedPoint.y;
            Vector3 flatIntended = intendedPoint; flatIntended.y = 0f;

            // Wall definition: center and direction along the wall on XZ
            Vector3 center     = (anchorArea + goalArea) * 0.5f;
            Vector3 wallNormal = (goalArea - anchorArea);
            if (wallNormal.sqrMagnitude < 1e-6f)
                return new Vector3(flatIntended.x, originalY, flatIntended.z); // degenerate (areas coincide)

            wallNormal.Normalize();

            // Wall direction is perpendicular to the normal, lying on XZ
            Vector3 wallDirection = Vector3.Cross(wallNormal, Vector3.up);
            float   dirMagSq      = wallDirection.sqrMagnitude;
            if (dirMagSq < 1e-6f)
            {
                // If up is parallel to wallNormal (shouldn't happen with y=0), fallback to a safe perpendicular
                wallDirection = new Vector3(-wallNormal.z, 0f, wallNormal.x);
                dirMagSq      = wallDirection.sqrMagnitude;
                if (dirMagSq < 1e-6f)
                    return new Vector3(flatIntended.x, originalY, flatIntended.z); // still degenerate
            }
            wallDirection.Normalize();

            // Project flatIntended onto the infinite wall line through center along wallDirection
            Vector3 toPoint   = flatIntended - center;
            float   t         = Vector3.Dot(toPoint, wallDirection);
            Vector3 projected = center + wallDirection * t;

            // Restore original Y (vertical wall)
            projected.y = originalY;
            return projected;
        }

    }
}
