using System;
using UnityEngine;
using UnityEngine.AI;
using World.Tactical;

namespace World.Actors.Player{
    /// Manages entity movement via Unity NavMeshAgent, updates Animator blend parameters, and handles slot docking.
    [RequireComponent(typeof(Animator))]
    public class CharacterMovement : MonoBehaviour{
        private enum DockingState { None, Moving, Aligning }

        private NavMeshAgent Agent    => _character.nmAgent;
        private Animator     Animator => _character.animator;
        private Transform    Body     => _character.body.transform;

        private DockingState  _dockingState = DockingState.None;
        private AreaSlot      _pendingSlot;
        private Vector3       _pendingDestination;
        private Action        _onDockedCallback;
        private Character     _character;
        private TurnPlanTrack _activeReplayPlan;
        private int           _nextMilestoneIndex;

        private static readonly int INPUT_FORWARD_PARAM = Animator.StringToHash("InputForward");
        private static readonly int INPUT_SIDE_PARAM    = Animator.StringToHash("InputSide");

        private const float TurnSpeedDegPerSec    = 360f;
        private const float AlignSpeedMetersPerSec = 2.5f;
        private const float ForwardDampTime        = 0.15f;

        private float _previousYRotation;
        private float _currentSide;

        /// Initializes component references and caches starting body rotation.
        private void Awake(){
            _character         = GetComponent<Character>();
            _previousYRotation = Body.eulerAngles.y;
        }

        /// Updates movement, animation blend parameters, and turn plan milestones every frame.
        private void Update(){
            Vector3 localVelocity = Body.InverseTransformDirection(Agent.velocity);
            float   forward       = localVelocity.z;

            float deltaAngle = Mathf.DeltaAngle(_previousYRotation, Body.eulerAngles.y);
            _previousYRotation = Body.eulerAngles.y;

            float targetSide;
            if (Time.deltaTime > 0f && Mathf.Abs(deltaAngle) > 0.01f)
                targetSide = Mathf.Clamp(deltaAngle / (Time.deltaTime * 120f), -1f, 1f);
            else if (Agent.velocity.sqrMagnitude > 0.01f && Agent.desiredVelocity.sqrMagnitude > 0.01f)
                targetSide = Mathf.Clamp(Body.InverseTransformDirection(Agent.desiredVelocity.normalized).x, -1f, 1f);
            else
                targetSide = 0f;

            _currentSide = Mathf.MoveTowards(_currentSide, targetSide, Time.deltaTime * 6f);

            Animator.SetFloat(INPUT_FORWARD_PARAM, forward, ForwardDampTime, Time.deltaTime);
            Animator.SetFloat(INPUT_SIDE_PARAM,    _currentSide);

            UpdateReplayMilestones();
            UpdateDocking();
        }

        /// Halts pending interactions when component is disabled.
        private void OnDisable() => CancelDocking();

        /// Commands the agent to navigate directly to a world destination.
        public void MoveTo(Vector3 destination){
            CancelDocking();
            _character.LeaveSlot();
            Agent.destination = destination;
        }

        /// Immediately stops the character and halts all navigation.
        public void Stop(){
            CancelDocking();
            _character.LeaveSlot();
            if (Agent.hasPath)
                Agent.ResetPath();
        }

        /// Warps the character and NavMeshAgent instantly to a position.
        public void WarpTo(Vector3 position){
            CancelDocking();
            _character.LeaveSlot();
            Agent.Warp(position);
        }

        /// Navigates the character to an AreaSlot and docks upon arrival.
        public void MoveToSlot(AreaSlot slot, Action onDocked = null){
            if (!slot.IsAvailable && slot.Occupant != _character && slot.ReservedBy != _character)
                return;

            CancelDocking();
            _character.LeaveSlot();

            if (Agent.hasPath)
                Agent.ResetPath();

            _pendingSlot      = slot;
            _onDockedCallback = onDocked;
            _pendingSlot.Reserve(_character);

            Vector3 destination = slot.Position;
            if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                destination = navHit.position;

            _pendingDestination = destination;
            _dockingState       = DockingState.Moving;
            Agent.destination   = destination;
        }

        /// Executes a planned turn track, navigating to the target slot and triggering effect milestones along the way.
        public void ReplayPlan(TurnPlanTrack plan, Action onCompleted = null){
            _activeReplayPlan   = plan;
            _nextMilestoneIndex = 0;
            MoveToSlot(plan.TargetSlot, onCompleted);
        }

        /// Checks distance to upcoming effect milestones along the active turn plan.
        private void UpdateReplayMilestones(){
            if (_activeReplayPlan == null || _nextMilestoneIndex >= _activeReplayPlan.Milestones.Count)
                return;

            PlannedEffectMilestone milestone = _activeReplayPlan.Milestones[_nextMilestoneIndex];
            if (Vector3.Distance(Body.position, milestone.Position) <= 1.0f){
                _nextMilestoneIndex++;
                milestone.Effect.Run(milestone.Context);
            }
        }

        /// Handles navigation arrival and facing alignment towards the slot orientation.
        private void UpdateDocking(){
            if (_dockingState == DockingState.None)
                return;

            if (_dockingState == DockingState.Moving){
                bool closeEnough  = Vector3.Distance(Body.position, _pendingDestination) <= Agent.stoppingDistance + 0.75f;
                bool isStopped    = !Agent.pathPending && Agent.velocity.sqrMagnitude < 0.05f;
                bool pathFinished = !Agent.pathPending && Agent.hasPath && Agent.remainingDistance <= Agent.stoppingDistance + 0.15f;

                if (!closeEnough || (!isStopped && !pathFinished))
                    return;

                if (Agent.hasPath)
                    Agent.ResetPath();

                Agent.updatePosition = false;
                Agent.updateRotation = false;
                _dockingState        = DockingState.Aligning;
                return;
            }

            if (_dockingState == DockingState.Aligning){
                Vector3 fwd = _pendingSlot.Rotation * Vector3.forward;
                fwd.y = 0f;
                Quaternion targetRot = fwd.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(fwd.normalized, Vector3.up)
                    : Quaternion.Euler(0f, _pendingSlot.Rotation.eulerAngles.y, 0f);

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

                CompleteDocking();
            }
        }

        /// Finalizes slot occupation and executes the docked callback.
        private void CompleteDocking(){
            Agent.updatePosition = true;
            Agent.updateRotation = true;
            _dockingState        = DockingState.None;

            AreaSlot slot     = _pendingSlot;
            Action   callback = _onDockedCallback;

            _pendingSlot        = null;
            _onDockedCallback   = null;
            _activeReplayPlan   = null;
            _nextMilestoneIndex = 0;

            if (!slot.IsAvailable && slot.Occupant != _character && slot.ReservedBy != _character)
                return;

            slot.Dock(_character);
            callback?.Invoke();
        }

        /// Cancels any active pending docking and releases the reserved slot.
        public void CancelDocking(){
            if (_dockingState == DockingState.None)
                return;

            Agent.updatePosition = true;
            Agent.updateRotation = true;
            _dockingState        = DockingState.None;

            if (_pendingSlot != null && _pendingSlot.ReservedBy == _character)
                _pendingSlot.Vacate();

            _pendingSlot        = null;
            _onDockedCallback   = null;
            _activeReplayPlan   = null;
            _nextMilestoneIndex = 0;
        }
    }
}
