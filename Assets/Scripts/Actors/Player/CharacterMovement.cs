using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using World;

namespace Actors.Player {
    /// <summary>
    /// Manages entity movement via Unity NavMeshAgent, updates Animator parameters
    /// (InputForward for movement speed and InputSide for turning/rotation variation),
    /// and handles interaction pathing with IUsable objects with smooth arrival alignment.
    /// Position tracking is handled by the Character's Sheet (TrackedTransform).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterMovement : MonoBehaviour {
        private NavMeshAgent Agent    => _character.nmAgent;
        private Animator     Animator => _character.animator;
        private Coroutine    _interactionRoutine;
        private Character    _character;
        private Transform    Body => _character.body.transform;

        
        private static readonly int INPUT_FORWARD_PARAM = Animator.StringToHash("InputForward");
        private static readonly int INPUT_SIDE_PARAM = Animator.StringToHash("InputSide");
        private const float ArrivalThreshold = 0.2f;
        private const float TurnSpeedDegPerSec = 720f;

        private float _previousYRotation;
        private float _currentSide;

        private void Awake(){
            _character         = GetComponent<Character>();
            _previousYRotation = Body.eulerAngles.y;
        }

        private void Update() => UpdateAnimationParameters();

        private void UpdateAnimationParameters() {
            Vector3 localVelocity = Body.InverseTransformDirection(Agent.velocity);
            float   forward       = localVelocity.z;

            float deltaAngle = Mathf.DeltaAngle(_previousYRotation, Body.eulerAngles.y);
            _previousYRotation = Body.eulerAngles.y;

            float targetSide;
            if (Time.deltaTime > 0f && Mathf.Abs(deltaAngle) > 0.01f) {
                targetSide = Mathf.Clamp(deltaAngle / (Time.deltaTime * 120f), -1f, 1f);
            }
            else if (Agent.velocity.sqrMagnitude > 0.01f && Agent.desiredVelocity.sqrMagnitude > 0.01f) {
                Vector3 localDesired = Body.InverseTransformDirection(Agent.desiredVelocity.normalized);
                targetSide = Mathf.Clamp(localDesired.x, -1f, 1f);
            }
            else {
                targetSide = 0f;
            }

            _currentSide = Mathf.MoveTowards(_currentSide, targetSide, Time.deltaTime * 6f);

            Animator.SetFloat(INPUT_FORWARD_PARAM, forward);
            Animator.SetFloat(INPUT_SIDE_PARAM, _currentSide);
        }

        private void OnDisable() => CancelInteraction();

        /// <summary>
        /// Commands the agent to navigate directly to a world destination.
        /// Overrides and cancels any pending interaction.
        /// </summary>
        public void MoveTo(Vector3 destination) {
            CancelInteraction();
            Agent.destination = destination;
        }

        /// <summary>
        /// Immediately stops the character and halts all navigation.
        /// </summary>
        public void Stop() {
            CancelInteraction();
            if (Agent.hasPath)
                Agent.ResetPath();
        }

        /// <summary>
        /// Warps the character and NavMeshAgent instantly to a position.
        /// </summary>
        public void WarpTo(Vector3 position) {
            CancelInteraction();
            Agent.Warp(position);
        }

        /// <summary>
        /// Navigates the character to the UseSpot of an IUsable object,
        /// smoothly turns to face UseRotation upon arrival, and triggers Use().
        /// </summary>
        public void MoveToAndUse(IUsable target, CharacterSheet userSheet) {
            CancelInteraction();
            _interactionRoutine = StartCoroutine(InteractRoutine(target, userSheet));
        }

        private IEnumerator InteractRoutine(IUsable target, CharacterSheet userSheet) {
            Agent.destination = target.UseSpot.position;

            while (Agent.pathPending || Agent.remainingDistance > Agent.stoppingDistance + ArrivalThreshold)
                yield return null;

            while (Quaternion.Angle(Body.rotation, target.UseRotation) > 1f) {
                Body.rotation = Quaternion.RotateTowards(
                    Body.rotation,
                    target.UseRotation,
                    TurnSpeedDegPerSec * Time.deltaTime);
                yield return null;
            }

            Body.rotation = target.UseRotation;
            target.Use(userSheet);
            _interactionRoutine = null;
        }

        private void CancelInteraction() {
            if (_interactionRoutine == null) return;
            StopCoroutine(_interactionRoutine);
            _interactionRoutine = null;
        }
    }
}
