#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using World.Tactical;

namespace CRPG.Editor.Tactical{
    /// Editor-only utility that bakes wall clearance and NavMesh FOV orientation directly into TacticalSlots.
    [InitializeOnLoad]
    public static class TacticalSlotCooker{
        public const float MinWallClearance = 0.6f;
        public const float WallNudgeSampleRadius = 1.2f;

        static TacticalSlotCooker(){
            TacticalZone.OnEditorCook = CookZonesAndSlots;
        }

        [MenuItem("Tools/Tactical/Align & Nudge Non-Featured Slots (All)")]
        public static void CookAllInActiveScene(){
            TacticalZone[] zones = Object.FindObjectsByType<TacticalZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            TacticalSlot[] slots = Object.FindObjectsByType<TacticalSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            CookZonesAndSlots(zones, slots);
            SceneView.RepaintAll();
        }

        /// Editor callback executed automatically whenever TacticalZone.CookAllZones() runs.
        public static void CookZonesAndSlots(TacticalZone[] zones, TacticalSlot[] slots){
            if (zones == null || zones.Length == 0) return;

            NavMeshPath path = new();

            foreach (TacticalZone zone in zones){
                if (!zone) continue;
                List<TacticalSlot> zoneSlots = GetNonFeaturedSlotsForZone(zone, slots);
                CookSlotsForZone(zone, zoneSlots, path);
            }
        }

        /// Cooks non-featured slots specifically for a single zone.
        public static void CookSingleZone(TacticalZone zone){
            if (!zone) return;

            TacticalSlot[] allSlots = Object.FindObjectsByType<TacticalSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<TacticalSlot> zoneSlots = GetNonFeaturedSlotsForZone(zone, allSlots);

            NavMeshPath path = new();
            CookSlotsForZone(zone, zoneSlots, path);

            SceneView.RepaintAll();
        }

        /// Gathers all non-featured slots belonging to a zone (on the zone GameObject itself, child objects, or cooked references).
        public static List<TacticalSlot> GetNonFeaturedSlotsForZone(TacticalZone zone, IEnumerable<TacticalSlot> allSlotsPool){
            HashSet<TacticalSlot> gathered = new();

            // Slots directly on the zone GameObject (supports multiple TacticalSlot components on same GameObject)
            foreach (TacticalSlot s in zone.GetComponents<TacticalSlot>()){
                if (s && !s.IsFeatured)
                    gathered.Add(s);
            }

            // Slots on child objects of the zone
            foreach (TacticalSlot s in zone.GetComponentsInChildren<TacticalSlot>(true)){
                if (s && !s.IsFeatured)
                    gathered.Add(s);
            }

            // Cooked references or slots assigned to this zone
            if (allSlotsPool != null){
                foreach (TacticalSlot s in allSlotsPool){
                    if (s && !s.IsFeatured && s.ParentZone == zone)
                        gathered.Add(s);
                }
            }

            return gathered.ToList();
        }

        /// Core algorithm: Nudges slots away from walls and calculates rotation facing the nearest zone with clear NavMesh FOV.
        private static void CookSlotsForZone(TacticalZone zone, List<TacticalSlot> slots, NavMeshPath path){
            if (slots == null || slots.Count == 0) return;

            // Prepare candidate zones for FOV checks (prefer adjacent zones first)
            List<TacticalZone> candidateZones = new();
            if (zone.AdjacentZones.Count > 0){
                candidateZones.AddRange(zone.AdjacentZones.Where(z => z != null && z != zone));
            }

            if (candidateZones.Count == 0){
                TacticalZone[] allZones = Object.FindObjectsByType<TacticalZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                candidateZones.AddRange(allZones.Where(z => z != null && z != zone));
            }

            foreach (TacticalSlot slot in slots){
                if (!slot || slot.IsFeatured) continue;

                Vector3 currentPos = slot.Position;

                // -------------------------------------------------------------
                // 1. Nudge away from walls if closer than MinWallClearance
                // -------------------------------------------------------------
                if (NavMesh.FindClosestEdge(currentPos, out NavMeshHit edgeHit, NavMesh.AllAreas)){
                    if (edgeHit.distance < MinWallClearance){
                        Vector3 pushDir = edgeHit.normal;
                        pushDir.y = 0f;
                        if (pushDir.sqrMagnitude > 0.001f){
                            float neededPush = MinWallClearance - edgeHit.distance;
                            Vector3 targetPos = currentPos + pushDir.normalized * neededPush;
                            if (NavMesh.SamplePosition(targetPos, out NavMeshHit sampleHit, WallNudgeSampleRadius, NavMesh.AllAreas)){
                                currentPos = sampleHit.position;
                            }
                        }
                    }
                }

                // -------------------------------------------------------------
                // 2. Find nearest zone with unobstructed NavMesh FOV
                // -------------------------------------------------------------
                TacticalZone bestFovZone = null;
                float shortestDistSqr = float.MaxValue;

                foreach (TacticalZone candidate in candidateZones){
                    Vector3 candidateCenter = candidate.Center;

                    // NavMesh.Raycast returns true if blocked by a NavMesh boundary edge / wall
                    bool isBlocked = NavMesh.Raycast(currentPos, candidateCenter, out NavMeshHit _, NavMesh.AllAreas);
                    if (!isBlocked){
                        float distSqr = (candidateCenter - currentPos).sqrMagnitude;
                        if (distSqr < shortestDistSqr){
                            shortestDistSqr = distSqr;
                            bestFovZone = candidate;
                        }
                    }
                }

                // -------------------------------------------------------------
                // 3. Fallback: If no adjacent zone has clear FOV, look at zone center
                // -------------------------------------------------------------
                Vector3 lookTarget;
                if (bestFovZone != null){
                    lookTarget = bestFovZone.Center;
                }
                else{
                    Vector3 zoneCenter = zone.Center;
                    // If line to center is also occluded (e.g. around an L-corner/pillar),
                    // look toward next path corner toward center so character faces the corridor/opening.
                    if (NavMesh.Raycast(currentPos, zoneCenter, out NavMeshHit _, NavMesh.AllAreas) &&
                        NavMesh.CalculatePath(currentPos, zoneCenter, NavMesh.AllAreas, path) &&
                        path.corners.Length >= 2){
                        lookTarget = path.corners[1];
                    }
                    else{
                        lookTarget = zoneCenter;
                    }
                }

                // -------------------------------------------------------------
                // 4. Calculate rotation & cook into slot anchor pose
                // -------------------------------------------------------------
                Vector3 lookDir = lookTarget - currentPos;
                lookDir.y = 0f;

                Quaternion finalRot = lookDir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(lookDir.normalized, Vector3.up)
                    : slot.Rotation;

                Undo.RecordObject(slot, "Cook Tactical Slot Pose");
                slot.SetAnchorPoseFromWorld(currentPos, finalRot);
                EditorUtility.SetDirty(slot);
            }
        }
    }
}
#endif
