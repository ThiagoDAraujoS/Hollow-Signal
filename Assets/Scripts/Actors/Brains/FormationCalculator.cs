using System.Collections.Generic;
using System.Linq;
using Actors.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Actors.Brains {
    /// <summary>
    /// Calculates offset destination positions for squads moving together in real-time exploration.
    /// Uses NavMesh path arrival tangents to face the true direction of arrival (handling winding
    /// corridors and U-turns), applies a tactical wedge offset, and conditionally executes a relaxation pass
    /// only when a wall or obstacle displaces units from their ideal positions.
    /// </summary>
    public static class FormationCalculator {
        private const float DefaultSpacing = 1.5f;
        private const float DefaultSampleRadius = 2.0f;
        private const float MinSeparationRatio = 0.7f;
        private const float DisplacementThreshold = 0.15f;
        private const int RelaxationIterations = 2;

        /// <summary>
        /// Calculates individual destinations for all selected units around the target ground point.
        /// </summary>
        /// <param name="targetPoint">The point clicked on the ground.</param>
        /// <param name="lead">The squad leader designated to receive slot 0.</param>
        /// <param name="units">All units currently selected and ordered to move.</param>
        /// <param name="spacing">Separation distance between units in meters.</param>
        /// <param name="sampleRadius">Radius to search for a valid NavMesh point around each offset.</param>
        /// <returns>A dictionary mapping each Character to their assigned world destination.</returns>
        public static Dictionary<Character, Vector3> CalculateFormationPositions(
            Vector3 targetPoint,
            Character lead,
            IReadOnlyCollection<Character> units,
            float spacing = DefaultSpacing,
            float sampleRadius = DefaultSampleRadius) {

            Dictionary<Character, Vector3> destinations = new(units.Count);
            if (units.Count == 0) return destinations;

            Character effectiveLead = lead != null && units.Contains(lead) ? lead : GetFirstUnit(units);
            destinations[effectiveLead] = targetPoint;

            if (units.Count == 1) return destinations;

            Quaternion facing = DetermineArrivalFacing(effectiveLead.WorldPosition, targetPoint, effectiveLead.WorldRotation);

            bool wasAnyUnitDisplaced = false;
            int followerIndex = 0;

            foreach (Character unit in units) {
                if (unit == effectiveLead) continue;

                Vector3 localOffset = GetWedgeOffset(followerIndex, spacing);
                Vector3 candidatePos = targetPoint + (facing * localOffset);

                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas)) {
                    destinations[unit] = hit.position;
                    if (Vector3.Distance(candidatePos, hit.position) > DisplacementThreshold)
                        wasAnyUnitDisplaced = true;
                }
                else {
                    destinations[unit] = targetPoint;
                    wasAnyUnitDisplaced = true;
                }

                followerIndex++;
            }

            if (wasAnyUnitDisplaced)
                ResolveSeparation(destinations, effectiveLead, spacing * MinSeparationRatio, sampleRadius);

            return destinations;
        }

        private static Quaternion DetermineArrivalFacing(Vector3 startPos, Vector3 targetPoint, Quaternion fallbackRotation) {
            NavMeshPath path = new();
            if (NavMesh.CalculatePath(startPos, targetPoint, NavMesh.AllAreas, path) && path.corners.Length >= 2) {
                Vector3 lastLeg = path.corners[^1] - path.corners[^2];
                lastLeg.y = 0f;
                if (lastLeg.sqrMagnitude > 0.01f)
                    return Quaternion.LookRotation(lastLeg.normalized);
            }

            Vector3 direct = targetPoint - startPos;
            direct.y = 0f;
            return direct.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direct.normalized) : fallbackRotation;
        }

        private static void ResolveSeparation(
            Dictionary<Character, Vector3> destinations,
            Character effectiveLead,
            float minSeparation,
            float sampleRadius) {

            List<Character> keys = destinations.Keys.ToList();

            for (int iter = 0; iter < RelaxationIterations; iter++) {
                for (int i = 0; i < keys.Count; i++) {
                    Character unitA = keys[i];
                    Vector3 posA = destinations[unitA];

                    for (int j = i + 1; j < keys.Count; j++) {
                        Character unitB = keys[j];
                        Vector3 posB = destinations[unitB];

                        Vector3 delta = posA - posB;
                        delta.y = 0f;
                        float distance = delta.magnitude;

                        if (distance >= minSeparation) continue;

                        Vector3 pushDir = distance > 0.001f ? delta / distance : Vector3.right;
                        float overlap = minSeparation - distance;

                        if (unitA == effectiveLead) {
                            Vector3 newB = posB - (pushDir * overlap);
                            if (NavMesh.SamplePosition(newB, out NavMeshHit hitB, sampleRadius, NavMesh.AllAreas))
                                destinations[unitB] = hitB.position;
                        }
                        else if (unitB == effectiveLead) {
                            Vector3 newA = posA + (pushDir * overlap);
                            if (NavMesh.SamplePosition(newA, out NavMeshHit hitA, sampleRadius, NavMesh.AllAreas))
                                destinations[unitA] = hitA.position;
                        }
                        else {
                            Vector3 newA = posA + (pushDir * (overlap * 0.5f));
                            Vector3 newB = posB - (pushDir * (overlap * 0.5f));

                            if (NavMesh.SamplePosition(newA, out NavMeshHit hitA, sampleRadius, NavMesh.AllAreas))
                                destinations[unitA] = hitA.position;
                            if (NavMesh.SamplePosition(newB, out NavMeshHit hitB, sampleRadius, NavMesh.AllAreas))
                                destinations[unitB] = hitB.position;
                        }
                    }
                }
            }
        }

        private static Vector3 GetWedgeOffset(int index, float spacing) {
            return index switch {
                0 => new Vector3(-spacing, 0f, -spacing),
                1 => new Vector3(spacing, 0f, -spacing),
                2 => new Vector3(0f, 0f, -spacing * 2f),
                _ => new Vector3((index % 2 == 0 ? -1f : 1f) * spacing * (1f + index * 0.5f), 0f, -spacing * (1f + index * 0.5f))
            };
        }

        private static Character GetFirstUnit(IEnumerable<Character> units) {
            using IEnumerator<Character> enumerator = units.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }
    }
}
