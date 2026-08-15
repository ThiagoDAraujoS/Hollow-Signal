using Core.Board;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Core.TurnManagement;

namespace Characters.Player {

    [DefaultExecutionOrder(100)]
    public class ActionController : Component {
        public Area currentArea;
        
        public int maxSprintDistance = 1;

        public UnityEvent onOutOfTurnResources;

        [SerializeField] private Area turnStartArea;
        
        private readonly Dictionary<Area, int> _areaDistances = new();

        public bool autoPass = true;
        
        private int
            _statMaxMp,
            _currentMpInvested,
            _currentApInvested;
        
        public void Awake(){
            TurnWheel.OnCrisisStart += DeactivateAutoPass;
            TurnWheel.OnCrisisEnd += ActivateAutoPass;
            
            Sheet.ap.onValueZeroed.AddListener(CheckTurnEnd);
            Sheet.mp.onValueZeroed.AddListener(CheckTurnEnd);
            
            TurnUser.onTurnStart.AddListener(OnTurnStart);
            TurnUser.onEnterTurnSystem.AddListener(OnTurnStart);
            OnTurnStart();
        }

        public void OnDestroy() {
            TurnWheel.OnCrisisStart -= DeactivateAutoPass;
            TurnWheel.OnCrisisEnd -= ActivateAutoPass;
            
            Sheet.ap.onValueZeroed.RemoveListener(CheckTurnEnd);
            Sheet.mp.onValueZeroed.RemoveListener(CheckTurnEnd);
            
            TurnUser.onTurnStart.RemoveListener(OnTurnStart);
            TurnUser.onEnterTurnSystem.RemoveListener(OnTurnStart);
        }

        public void ActivateAutoPass() => autoPass = true;
        
        public void DeactivateAutoPass() => autoPass = false;

        public void CheckTurnEnd() {
            if(!autoPass || Sheet.HasAction || Sheet.HasMove) return;
            onOutOfTurnResources?.Invoke();
            TurnUser.PassTurn();
        }

        public void RefreshTurnResources() {
            Sheet.ap.Restore();
            Sheet.mp.Restore();
        }

        public void OnTurnStart() {
            Debug.Log("turn start");
            RefreshTurnResources();
            
            _currentMpInvested = 0;
            _currentApInvested = 0;
            turnStartArea = currentArea;
            _statMaxMp = Sheet.mp.Limit;
            CacheValidMoveGraph();
        }

        public void CacheValidMoveGraph() {
            _areaDistances.Clear();

            Queue<AreaNode> queue = new();
            HashSet<Area> visited = new();

            // Start Node
            queue.Enqueue(new AreaNode(turnStartArea, 0));
            visited.Add(turnStartArea);
            _areaDistances.Add(turnStartArea, 0);

            // Calculate how deep we can go (Walk Range + Run Range)
            int absoluteMaxRange = _statMaxMp + maxSprintDistance;

            while (queue.TryDequeue(out AreaNode current)) {
                if (current.depth >= absoluteMaxRange) continue;

                foreach (Area neighbor in current.area.neighbors) {
                    if (!visited.Add(neighbor)) continue;

                    _areaDistances.Add(neighbor, current.depth + 1);
                    queue.Enqueue(new AreaNode(neighbor, current.depth + 1));
                }
            }
        }

        private struct AreaNode {
            public readonly Area area;
            public readonly int depth;
            public AreaNode(Area a, int d) { area = a; depth = d; }
        }
        
        private (int mp, int ap) GetResourceCost(int distance) {
            int mpNeeded = Mathf.Min(distance, _statMaxMp);
            int apNeeded = distance > _statMaxMp ? 1 : 0;
            return (mpNeeded, apNeeded);
        }

        public Vector3 ValidateAndConsumeMove(Vector3 intendedPoint) {
            if (currentArea == null) return intendedPoint;

            // 1. VORONOI CHECK
            Area targetArea = currentArea;
            float closestDist = Vector3.Distance(intendedPoint, currentArea.transform.position);

            foreach (Area neighbor in currentArea.neighbors) {
                float dist = Vector3.Distance(intendedPoint, neighbor.transform.position);
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                targetArea  = neighbor;
            }

            // 2. STAYING STILL
            if (targetArea == currentArea) return intendedPoint;

            // 3. CHECK LEGALITY AND COSTS
            if (!_areaDistances.TryGetValue(targetArea, out int targetDist))
                return ClampToAreaBoundary(intendedPoint, targetArea);
            
            // A. Calculate what the target area COSTS absolute
            (int reqMp, int reqAp) = GetResourceCost(targetDist);

            // B. Calculate the DIFFERENCE (Delta) from what we are paying NOW
            int mpDelta = reqMp - _currentMpInvested;
            int apDelta = reqAp - _currentApInvested;

            // C. Check affordability using the DELTA
            // We only care if we have enough resource to cover the INCREASE.
            bool canAffordMp = mpDelta <= 0 || Sheet.mp >= mpDelta;
            bool canAffordAp = apDelta <= 0 || Sheet.ap >= apDelta;

            if (!canAffordMp || !canAffordAp)
                return ClampToAreaBoundary(intendedPoint, targetArea);
            
            ApplyMovementCost(mpDelta, apDelta);
            currentArea = targetArea;
            return intendedPoint;
        }

        private void ApplyMovementCost(int mpDelta, int apDelta) {
            Sheet.mp.Value -= mpDelta;
            Sheet.ap.Value -= apDelta;
            _currentMpInvested += mpDelta;
            _currentApInvested += apDelta;
        }

        private Vector3 ClampToAreaBoundary(Vector3 intendedPoint, Area target) {
            // 1. Flatten positions to XZ plane
            Vector3 anchorArea = currentArea.transform.position;
            anchorArea.y = 0f;
            Vector3 goalArea = target.transform.position;
            goalArea.y = 0f;
    
            // 2. Calculate the halfway point (the wall center)
            Vector3 center = (anchorArea + goalArea) * 0.5f;

            // 3. Get the direction from Current -> Target and Normalize it
            // We trust this is never zero-length
            Vector3 wallNormal = (goalArea - anchorArea).normalized;

            // 4. Calculate the direction of the wall itself
            // Since wallNormal is XZ and Up is Y, the result is guaranteed to be normalized.
            Vector3 wallDirection = Vector3.Cross(wallNormal, Vector3.up);

            // 5. Project the intended point onto the wall line
            Vector3 flatIntended = intendedPoint;
            flatIntended.y = 0f;

            float t = Vector3.Dot(flatIntended - center, wallDirection);
    
            // 6. Reconstruct and restore original Y
            Vector3 projected = center + (wallDirection * t);
            projected.y = intendedPoint.y;
    
            return projected;
        }
    }
}