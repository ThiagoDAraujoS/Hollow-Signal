using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace World.Tactical{
    /// Spatial query helper evaluating shallow distance followed by NavMesh path distance.
    public static class TacticalSpatialResolver{
        private static readonly NavMeshPath SharedPath = new();

        /// Finds the closest TacticalSlot from a starting position via shallow filter and NavMesh path comparison.
        public static TacticalSlot FindClosestSlot(Vector3 fromPosition, bool onlyAvailable = true, int candidateCount = 5){
            IEnumerable<TacticalSlot> pool = onlyAvailable ? TacticalSlot.AllSlots.Where(s => s.IsAvailable) : TacticalSlot.AllSlots;

            List<TacticalSlot> candidates = pool
                .OrderBy(s => (s.Position - fromPosition).sqrMagnitude)
                .Take(candidateCount)
                .ToList();

            if (candidates.Count <= 1)
                return candidates.FirstOrDefault();

            TacticalSlot bestSlot = candidates[0];
            float shortestDistance = float.MaxValue;

            foreach (TacticalSlot candidate in candidates){
                if (!NavMesh.CalculatePath(fromPosition, candidate.Position, NavMesh.AllAreas, SharedPath))
                    continue;

                float pathDist = CalculatePathDistance(SharedPath);
                if (pathDist < shortestDistance){
                    shortestDistance = pathDist;
                    bestSlot = candidate;
                }
            }

            return bestSlot;
        }

        /// Finds the closest TacticalZone from a starting position via shallow filter and NavMesh path comparison.
        public static TacticalZone FindClosestZone(Vector3 fromPosition, int candidateCount = 5){
            List<TacticalZone> candidates = TacticalZone.AllZones
                .OrderBy(z => (z.transform.position - fromPosition).sqrMagnitude)
                .Take(candidateCount)
                .ToList();

            if (candidates.Count <= 1)
                return candidates.FirstOrDefault();

            TacticalZone bestZone = candidates[0];
            float shortestDistance = float.MaxValue;

            foreach (TacticalZone candidate in candidates){
                Vector3 target = candidate.ChildSlots.Count > 0 ? candidate.ChildSlots[0].Position : candidate.transform.position;
                if (!NavMesh.CalculatePath(fromPosition, target, NavMesh.AllAreas, SharedPath))
                    continue;

                float pathDist = CalculatePathDistance(SharedPath);
                if (pathDist < shortestDistance){
                    shortestDistance = pathDist;
                    bestZone = candidate;
                }
            }

            return bestZone;
        }

        /// Resolves the intended TacticalSlot when clicking an arbitrary ground position during Crisis mode.
        public static TacticalSlot ResolveClickedSlot(Vector3 worldClickPosition, Vector3 fromPosition, int candidateCount = 5){
            NavMesh.SamplePosition(worldClickPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas);
            Vector3 samplePoint = hit.position;

            TacticalZone targetZone = FindClosestZone(samplePoint, candidateCount);
            TacticalSlot slotInZone = targetZone != null ? targetZone.GetBestAvailableFallbackSlot(samplePoint) : null;

            return slotInZone != null ? slotInZone : FindClosestSlot(samplePoint, true, candidateCount);
        }

        /// Calculates cumulative linear distance across all corner segments of a NavMeshPath.
        public static float CalculatePathDistance(NavMeshPath path){
            float total = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
                total += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            return total;
        }
    }
}
