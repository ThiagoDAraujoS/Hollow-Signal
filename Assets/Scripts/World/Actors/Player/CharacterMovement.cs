using UnityEngine;
using UnityEngine.AI;
using World.Tactical;

namespace World.Actors.Player{
    /// Manages entity movement via Unity NavMeshAgent, updates Animator parameters, and handles interaction.
    [RequireComponent(typeof(Animator))]
    public class CharacterMovement : MonoBehaviour{
        private enum InteractionState { None, Moving, Aligning, Using }

        /// NavMeshAgent on the character used for pathfinding.
        private NavMeshAgent Agent => _character.nmAgent;

        /// Animator component driving character animations.
        private Animator Animator => _character.animator;

        /// World transform of the character's physical body.
        private Transform Body => _character.body.transform;

        /// Active phase of pending interaction workflow.
        private InteractionState _interactionState = InteractionState.None;

        /// Pending interaction target when moving to use an object.
        private IUsable _pendingTarget;

        /// Pending user character sheet executing the interaction.
        private CharacterSheet _pendingUserSheet;

        /// Pending slot reserved while en route.
        private AreaSlot _pendingSlot;

        /// Target destination for the pending interaction.
        private Vector3 _pendingDestination;

        /// Character root component reference.
        private Character _character;

        /// Animator hash for forward speed blending parameter.
        private static readonly int INPUT_FORWARD_PARAM = Animator.StringToHash("InputForward");

        /// Animator hash for angular turn blending parameter.
        private static readonly int INPUT_SIDE_PARAM = Animator.StringToHash("InputSide");

        /// Animator hash for the interaction trigger parameter.
        private static readonly int INTERACT_PARAM = Animator.StringToHash("Interact");

        /// Rotation angular speed in degrees per second when aligning to interaction facing.
        private const float TurnSpeedDegPerSec = 360f;

        /// Movement linear speed in meters per second when lerping to exact interaction spot.
        private const float AlignSpeedMetersPerSec = 2.5f;

        /// Cached Y-axis body angle from the previous frame to calculate turning angular rate.
        private float _previousYRotation;

        /// Smoothed turning blend value passed into the Animator.
        private float _currentSide;

        /// Initializes component references and caches starting body rotation.
        private void Awake(){
            _character         = GetComponent<Character>();
            _previousYRotation = Body.eulerAngles.y;
        }

        /// Updates movement and turning animation blend parameters every frame.
        private void Update(){
            Vector3 localVelocity = Body.InverseTransformDirection(Agent.velocity);
            float   forward       = localVelocity.z;

            float deltaAngle = Mathf.DeltaAngle(_previousYRotation, Body.eulerAngles.y);
            _previousYRotation = Body.eulerAngles.y;

            float targetSide;
            if (Time.deltaTime > 0f && Mathf.Abs(deltaAngle) > 0.01f)
                targetSide = Mathf.Clamp(deltaAngle / (Time.deltaTime * 120f), -1f, 1f);
            else if (Agent.velocity.sqrMagnitude > 0.01f && Agent.desiredVelocity.sqrMagnitude > 0.01f){
                Vector3 localDesired = Body.InverseTransformDirection(Agent.desiredVelocity.normalized);
                targetSide = Mathf.Clamp(localDesired.x, -1f, 1f);
            }
            else
                targetSide = 0f;

            _currentSide = Mathf.MoveTowards(_currentSide, targetSide, Time.deltaTime * 6f);

            Animator.SetFloat(INPUT_FORWARD_PARAM, forward);
            Animator.SetFloat(INPUT_SIDE_PARAM,    _currentSide);

            UpdateInteraction();
        }

        /// Halts pending interactions when component is disabled.
        private void OnDisable() => CancelInteraction();

        /// Commands the agent to navigate directly to a world destination.
        public void MoveTo(Vector3 destination){
            CancelInteraction();
            _character.LeaveSlot();
            Agent.destination = destination;
        }

        /// Immediately stops the character and halts all navigation.
        public void Stop(){
            CancelInteraction();
            _character.LeaveSlot();
            if (Agent.hasPath)
                Agent.ResetPath();
        }

        /// Warps the character and NavMeshAgent instantly to a position.
        public void WarpTo(Vector3 position){
            CancelInteraction();
            _character.LeaveSlot();
            Agent.Warp(position);
        }

        /// Navigates the character to an IUsable object and triggers interaction upon arrival.
        public void MoveToAndUse(IUsable target, CharacterSheet userSheet){
            CancelInteraction();
            _character.LeaveSlot();

            if (Agent.hasPath)
                Agent.ResetPath();

            _pendingTarget    = target;
            _pendingUserSheet = userSheet;
            _pendingSlot      = target as AreaSlot;
            if (_pendingSlot == null && target is Component comp)
                _pendingSlot = comp.GetComponent<AreaSlot>();

            if (_pendingSlot != null)
                _pendingSlot.Reserve(_character);

            Vector3 destination = target.UsePosition;
            if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                destination = navHit.position;

            _pendingDestination = destination;
            _interactionState   = InteractionState.Moving;
            Agent.destination   = destination;
        }

        /// Handles navigation arrival, facing alignment, and interaction animation playback.
        private void UpdateInteraction(){
            if (_interactionState == InteractionState.None) return;

            if (_interactionState == InteractionState.Moving){
                bool closeEnough  = Vector3.Distance(Body.position, _pendingDestination) <= Agent.stoppingDistance + 0.75f;
                bool isStopped    = !Agent.pathPending && Agent.velocity.sqrMagnitude < 0.05f;
                bool pathFinished = !Agent.pathPending && Agent.hasPath && Agent.remainingDistance <= Agent.stoppingDistance + 0.15f;

                if (!closeEnough || (!isStopped && !pathFinished)) return;

                if (Agent.hasPath)
                    Agent.ResetPath();

                Agent.updatePosition = false;
                Agent.updateRotation = false;
                _interactionState    = InteractionState.Aligning;
                return;
            }

            if (_interactionState == InteractionState.Aligning){
                Vector3 fwd = _pendingTarget.UseRotation * Vector3.forward;
                fwd.y = 0f;
                Quaternion targetRot = fwd.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(fwd.normalized, Vector3.up)
                    : Quaternion.Euler(0f, _pendingTarget.UseRotation.eulerAngles.y, 0f);

                bool angleAligned = Quaternion.Angle(Body.rotation, targetRot) <= 0.5f;
                bool posAligned   = Vector3.Distance(Body.position, _pendingDestination) <= 0.02f;

                if (!angleAligned || !posAligned){
                    Body.rotation = Quaternion.RotateTowards(Body.rotation, targetRot, TurnSpeedDegPerSec * Time.deltaTime);
                    Body.position = Vector3.MoveTowards(Body.position, _pendingDestination, AlignSpeedMetersPerSec * Time.deltaTime);
                    return;
                }

                Body.position      = _pendingDestination;
                Body.rotation      = targetRot;
                Agent.Warp(_pendingDestination);
                _previousYRotation = Body.eulerAngles.y;
                _interactionState  = InteractionState.Using;
                Animator.SetTrigger(INTERACT_PARAM);
                return;
            }

            if (_interactionState == InteractionState.Using){
                AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Use") && stateInfo.normalizedTime >= 0.95f)
                    TriggerAnimationUse();
            }
        }

        /// Executes the pending interaction when the animation triggers Use or completes.
        public void TriggerAnimationUse(){
            if (_interactionState != InteractionState.Using) return;

            Agent.updatePosition = true;
            Agent.updateRotation = true;
            _interactionState    = InteractionState.None;

            IUsable        target    = _pendingTarget;
            CharacterSheet userSheet = _pendingUserSheet;
            AreaSlot       slot      = _pendingSlot;

            _pendingTarget    = null;
            _pendingUserSheet = null;
            _pendingSlot      = null;

            if (slot != null){
                slot.Claim(_character);
                _character.CurrentSlot = slot;
            }

            target.Use(userSheet);
        }

        /// Cancels any active pending interaction and releases reserved slot.
        private void CancelInteraction(){
            Agent.updatePosition = true;
            Agent.updateRotation = true;
            _interactionState    = InteractionState.None;

            if (_pendingSlot != null){
                _pendingSlot.Release();
                _pendingSlot = null;
            }

            _pendingTarget    = null;
            _pendingUserSheet = null;
        }
    }
}
