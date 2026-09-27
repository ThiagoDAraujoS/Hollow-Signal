using Core.Attributes;
using Core.State;
using Data.Effects;
using UnityEngine;
using UnityEngine.AI;
using World.Actors.Player;

namespace World.Tactical{
    /// Discrete occupancy states of a spatial standing slot.
    public enum SlotOccupancyState { Vacant, Reserved, Occupied }

    /// Master base class representing a discrete standing spot and spatial anchor in the world.
    [SelectionBase]
    public abstract class AreaSlot : TrackedBehaviour{
        [Header("Anchor Pose (Widget)")]
        [SerializeField] protected SpatialPose anchorPose = SpatialPose.Default;

        [Header("Identity")]
        [SerializeField] protected string slotDisplayName;

        [Header("Attached Effect (Optional)")]
        [RequireInterface(typeof(IEffect))]
        [SerializeField] protected Object linkedEffect;

        [Header("Visual Indicator (Optional)")]
        [SerializeField] protected GameObject visualRing;

        public Character Occupant   { get; protected set; }
        public Character ReservedBy { get; protected set; }

        public SpatialPose        AnchorPose      => anchorPose;
        public string             SlotDisplayName => slotDisplayName;
        public IEffect            LinkedEffect    => linkedEffect as IEffect;
        public Vector3            Position        => anchorPose.GetWorldPosition(transform);
        public SlotOccupancyState State           => Occupant != null ? SlotOccupancyState.Occupied : (ReservedBy != null ? SlotOccupancyState.Reserved : SlotOccupancyState.Vacant);
        public bool               IsAvailable     => State == SlotOccupancyState.Vacant;

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

        /// Snaps anchor pose to NavMesh at start.
        protected virtual void Start() => SnapToNavMesh();

        /// Snaps world anchor position to the nearest valid point on the NavMesh.
        public void SnapToNavMesh(){
            if (NavMesh.SamplePosition(Position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                SetAnchorPoseFromWorld(hit.position, Rotation);
        }

        /// Sets default anchor pose on reset.
        private void Reset() => anchorPose = SpatialPose.Default;

        /// Sets anchor pose coordinates from world position and rotation.
        public void SetAnchorPoseFromWorld(Vector3 worldPos, Quaternion worldRot) =>
            anchorPose.SetFromWorld(transform, worldPos, worldRot);

        /// Reserves this slot for an approaching character.
        public virtual void Reserve(Character character){
            if (!IsAvailable && Occupant != character && ReservedBy != character)
                return;
            ReservedBy = character;
        }

        /// Docks character into slot, updates reciprocal references, and clears reservations.
        public virtual void Dock(Character character){
            Occupant              = character;
            ReservedBy            = null;
            character.CurrentSlot = this;
        }

        /// Vacates slot, clearing occupant and reservation references.
        public virtual void Vacate(){
            Occupant   = null;
            ReservedBy = null;
        }

        /// Executes the linked effect on this slot.
        public virtual void Use(CharacterSheet whosUsing) => LinkedEffect?.Run(new EffectContext(whosUsing));

        /// Toggles the visual ring indicator for this slot if assigned.
        public virtual void SetVisualActive(bool active){
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
