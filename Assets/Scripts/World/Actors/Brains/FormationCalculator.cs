using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using World.Actors.Player;

namespace World.Actors.Brains{
    /// Calculates offset destination positions for squads moving together in real-time exploration.
    public static class FormationCalculator{
        private const float DefaultSpacing        = 1.5f;
        private const float DefaultSampleRadius   = 2.0f;
        private const float MinSeparationRatio    = 0.7f;
        private const float DisplacementThreshold = 0.15f;
        private const int   RelaxationIterations  = 2;

        /// Calculates individual destinations for all selected units around the target ground point.
        public static Dictionary<Character, Vector3> CalculateFormationPositions(
            Vector3                        targetPoint,
            Character                      lead,
            IReadOnlyCollection<Character> units,
            float                          spacing        = DefaultSpacing,
            float                          sampleRadius   = DefaultSampleRadius,
            Quaternion?                    overrideFacing = null){

            Dictionary<Character, Vector3> destinations = new(units.Count);
            if (units.Count == 0) return destinations;

            Character effectiveLead = units.Contains(lead) ? lead : units.First();
            destinations[effectiveLead] = targetPoint;

            if (units.Count == 1) return destinations;

            Quaternion facing = overrideFacing ?? DetermineArrivalFacing(effectiveLead.WorldPosition, targetPoint, effectiveLead.WorldRotation);

            bool wasAnyUnitDisplaced = false;
            int  followerIndex       = 0;

            foreach (Character unit in units){
                if (unit == effectiveLead) continue;

                Vector3 localOffset  = GetWedgeOffset(followerIndex, spacing);
                Vector3 candidatePos = targetPoint + (facing * localOffset);

                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas)){
                    destinations[unit] = hit.position;
                    if (Vector3.Distance(candidatePos, hit.position) > DisplacementThreshold)
                        wasAnyUnitDisplaced = true;
                }
                else{
                    destinations[unit]  = targetPoint;
                    wasAnyUnitDisplaced = true;
                }

                followerIndex++;
            }

            if (wasAnyUnitDisplaced)
                ResolveSeparation(destinations, effectiveLead, spacing * MinSeparationRatio, sampleRadius);

            return destinations;
        }

        /// Computes the squad facing rotation upon arrival using the final NavMesh path segment or direct heading.
        private static Quaternion DetermineArrivalFacing(Vector3 startPos, Vector3 targetPoint, Quaternion fallbackRotation){
            NavMeshPath path = new();
            if (NavMesh.CalculatePath(startPos, targetPoint, NavMesh.AllAreas, path) && path.corners.Length >= 2){
                Vector3 lastLeg = path.corners[^1] - path.corners[^2];
                lastLeg.y = 0f;
                if (lastLeg.sqrMagnitude > 0.01f)
                    return Quaternion.LookRotation(lastLeg.normalized);
            }

            Vector3 direct = targetPoint - startPos;
            direct.y = 0f;
            return direct.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direct.normalized) : fallbackRotation;
        }

        /// Executes iterative relaxation passes to push overlapping units apart while keeping them on the NavMesh.
        private static void ResolveSeparation(
            Dictionary<Character, Vector3> destinations,
            Character                      effectiveLead,
            float                          minSeparation,
            float                          sampleRadius){

            List<Character> keys = destinations.Keys.ToList();

            foreach (int _ in Enumerable.Range(0, RelaxationIterations))
                foreach ((Character unitA, int i) in keys.Select((u, idx) => (u, idx)))
                    foreach (Character unitB in keys.Skip(i + 1)){
                        Vector3 posA = destinations[unitA];
                        Vector3 posB = destinations[unitB];

                        Vector3 delta = posB - posA;
                        delta.y = 0f;
                        float dist = delta.magnitude;

                        if (dist >= minSeparation || dist < 0.001f) continue;

                        Vector3 pushDir     = delta / dist;
                        float   overlapHalf = (minSeparation - dist) * 0.5f;

                        if (unitA != effectiveLead){
                            Vector3 candidateA = posA - pushDir * overlapHalf;
                            if (NavMesh.SamplePosition(candidateA, out NavMeshHit hitA, sampleRadius, NavMesh.AllAreas))
                                destinations[unitA] = hitA.position;
                        }

                        if (unitB != effectiveLead){
                            Vector3 candidateB = posB + pushDir * overlapHalf;
                            if (NavMesh.SamplePosition(candidateB, out NavMeshHit hitB, sampleRadius, NavMesh.AllAreas))
                                destinations[unitB] = hitB.position;
                        }
                    }
        }

        /// Computes the alternating tactical wedge local-space offset for a follower index.
        private static Vector3 GetWedgeOffset(int followerIndex, float spacing){
            int   tier = (followerIndex / 2) + 1;
            float sign = (followerIndex % 2 == 0) ? 1.0f : -1.0f;
            return new Vector3(sign * tier * spacing, 0f, -tier * spacing * 0.85f);
        }
    }
}
