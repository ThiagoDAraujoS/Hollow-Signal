using Core.State;
using UnityEngine;
using UnityEngine.AI;
using World.Actors.Player;

namespace World.Tactical{
    /// Master base class representing a discrete standing spot and spatial anchor in the world.
    [SelectionBase]
    public abstract class AreaSlot : TrackedBehaviour{
        [Header("Anchor Pose (Widget)")]
        [SerializeField] protected SpatialPose anchorPose = SpatialPose.Default;

        [Header("Identity")]
        [SerializeField] protected string slotDisplayName;

        [Header("Attached Interactable (Optional)")]
        [SerializeField] protected MonoBehaviour linkedInteractable;

        [Header("Visual Indicator (Optional)")]
        [SerializeField] protected GameObject visualRing;

        public Character Occupant{ get; protected set; }
        public Character ReservedBy{ get; protected set; }

        public SpatialPose AnchorPose      => anchorPose;
        public string      SlotDisplayName => slotDisplayName;
        public bool        IsAvailable     => Occupant == null && ReservedBy == null;

        public Vector3    Position => anchorPose.GetWorldPosition(transform);

        /// Evaluates world rotation locked strictly to world UP (Y-axis yaw only).
        public Quaternion Rotation{
            get{
                Vector3 fwd = anchorPose.GetWorldRotation(transform) * Vector3.forward;
                fwd.y = 0f;
                return fwd.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(fwd.normalized, Vector3.up)
                    : Quaternion.Euler(0f, anchorPose.GetWorldRotation(transform).eulerAngles.y, 0f);
            }
        }

        /// Resolves linked interactable component, falling back to any sibling IUsable on this GameObject.
        public IUsable LinkedUsable{
            get{
                if (linkedInteractable is IUsable usable)
                    return usable;

                foreach (IUsable comp in GetComponents<IUsable>())
                    if (!ReferenceEquals(comp, this))
                        return comp;

                return null;
            }
        }

        /// Snaps anchor pose to NavMesh at start.
        protected virtual void Start() => SnapToNavMesh();

        /// Snaps world anchor position to the nearest valid point on the NavMesh.
        public void SnapToNavMesh(){
            if (NavMesh.SamplePosition(Position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                SetAnchorPoseFromWorld(hit.position, Rotation);
        }

        /// Sets default anchor pose 1 unit in front and facing inward on reset.
        private void Reset() => anchorPose = SpatialPose.Default;

        /// Sets anchor pose coordinates from world position and rotation.
        public void SetAnchorPoseFromWorld(Vector3 worldPos, Quaternion worldRot) =>
            anchorPose.SetFromWorld(transform, worldPos, worldRot);

        /// Reserves this slot for an approaching character.
        public virtual void Reserve(Character character) => ReservedBy = character;

        /// Claims this slot when the character arrives.
        public virtual void Claim(Character character){
            Occupant = character;
            ReservedBy = null;
        }

        /// Releases any reservation or occupancy on this slot.
        public virtual void Release(){
            Occupant = null;
            ReservedBy = null;
        }

        /// Toggles the visual ring indicator for this slot if assigned.
        public virtual void SetVisualActive(bool active){
            // Paradigm exception: visualRing is an optional visual indicator that level designers may leave unassigned.
            if (visualRing != null)
                visualRing.SetActive(active);
        }

        /// Draws editor gizmo representing slot position and facing direction.
        protected virtual void OnDrawGizmos(){
            Gizmos.color = Color.white;
            Vector3 pos = Position;
            Gizmos.DrawWireSphere(pos, 0.35f);
            Gizmos.DrawRay(pos, Rotation * Vector3.forward * 0.7f);
        }
    }
}
