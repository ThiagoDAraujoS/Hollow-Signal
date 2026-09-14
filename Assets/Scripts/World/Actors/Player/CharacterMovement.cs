using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace World.Actors.Player{
    /// Manages entity movement via Unity NavMeshAgent, updates Animator parameters
    /// (InputForward for movement speed and InputSide for turning/rotation variation),
    /// and handles interaction pathing with IUsable objects with smooth arrival alignment.
    [RequireComponent(typeof(Animator))]
    public class CharacterMovement : MonoBehaviour{
        /// NavMeshAgent on the character used for pathfinding.
        private NavMeshAgent Agent => _character.nmAgent;

        /// Animator component driving character animations.
        private Animator Animator => _character.animator;

        /// World transform of the character's physical body.
        private Transform Body => _character.body.transform;

        /// Active coroutine handling navigation and execution of an IUsable interaction.
        private Coroutine _interactionRoutine;

        /// Character root component reference.
        private Character _character;

        /// Animator hash for forward speed blending parameter.
        private static readonly int INPUT_FORWARD_PARAM = Animator.StringToHash("InputForward");

        /// Animator hash for angular turn blending parameter.
        private static readonly int INPUT_SIDE_PARAM = Animator.StringToHash("InputSide");

        /// Distance tolerance added to stopping distance for arriving at interaction spots.
        private const float ArrivalThreshold = 0.2f;

        /// Rotation angular speed in degrees per second when aligning to interaction facing.
        private const float TurnSpeedDegPerSec = 720f;

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
        }

        /// Halts pending interactions when component is disabled.
        private void OnDisable() => CancelInteraction();

        /// Commands the agent to navigate directly to a world destination.
        public void MoveTo(Vector3 destination){
            CancelInteraction();
            Agent.destination = destination;
        }

        /// Immediately stops the character and halts all navigation.
        public void Stop(){
            CancelInteraction();
            if (Agent.hasPath)
                Agent.ResetPath();
        }

        /// Warps the character and NavMeshAgent instantly to a position.
        public void WarpTo(Vector3 position){
            CancelInteraction();
            Agent.Warp(position);
        }

        /// Navigates the character to an IUsable object and triggers interaction upon arrival.
        public void MoveToAndUse(IUsable target, CharacterSheet userSheet){
            CancelInteraction();
            _interactionRoutine = StartCoroutine(InteractRoutine(target, userSheet));
        }

        /// Coroutine navigating to the interaction spot and rotating towards UseRotation before execution.
        private IEnumerator InteractRoutine(IUsable target, CharacterSheet userSheet){
            Agent.destination = target.UseSpot.position;

            while (Agent.pathPending || Agent.remainingDistance > Agent.stoppingDistance + ArrivalThreshold)
                yield return null;

            while (Quaternion.Angle(Body.rotation, target.UseRotation) > 1f){
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

        /// Stops any currently active interaction coroutine.
        private void CancelInteraction(){
            if (_interactionRoutine == null) return;
            StopCoroutine(_interactionRoutine);
            _interactionRoutine = null;
        }
    }
}
