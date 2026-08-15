using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

namespace Characters.Player{
    public class Motor : Component{
        private static readonly int
            AnimatorTurn    = Animator.StringToHash("InputSide"),
            AnimatorForward = Animator.StringToHash("InputForward");

        [SerializeField] private float moveSpeedScale           = 3.5f;
        [SerializeField] private float rotateSpeed              = 3f;
        [SerializeField] private float accelerationCatchUpSpeed = 2f;
        [SerializeField] private float rotationCatchUpSpeed     = 1.5f;

        [SerializeField] private AnimationCurve moveSpeedCurve;

        private Vector2    _rawMoveInput = Vector2.zero;
        private Quaternion _targetRotation;

        private bool  _isMoving;
        private bool  _rawRunningInput;
        private float _rotation;
        private float _speed;
        private Quaternion _lastRotation;

        private Vector3 _targetDestination;
        private bool    _hasDestination;
        [SerializeField] private float stoppingDistance = 0.2f;

        /// Sets a point-and-click destination for the character.
        public void SetDestination(Vector3 destination) {
            _targetDestination = destination;
            _hasDestination = true;
            _isMoving = true;
        }

        /// Stops point-and-click movement.
        public void Stop() {
            _hasDestination = false;
            _isMoving = false;
            _rawMoveInput = Vector2.zero;
        }

        /// Read point-and-click click action or legacy move input.
        public void OnPointAndClickUpdate(InputAction.CallbackContext context) {
            if (context.performed) {
                Camera mainCamera = Camera.main;
                if (mainCamera == null) return;

                Vector2 pointerPos = Pointer.current != null ? Pointer.current.position.ReadValue() : Mouse.current.position.ReadValue();
                Ray ray = mainCamera.ScreenPointToRay(pointerPos);

                if (Physics.Raycast(ray, out RaycastHit hit, 100f)) {
                    // Check if hit an Area or object inside an Area
                    Core.Board.Area area = hit.collider.GetComponentInParent<Core.Board.Area>();
                    Vector3 goalPoint = hit.point;

                    if (area != null) {
                        goalPoint = area.GetNearestGoalPosition(hit.point);
                    }

                    SetDestination(goalPoint);
                }
            }
        }

        /// Read the input system to build a move input vector (legacy or direct stick fallback)
        public void OnMoveInputUpdate(InputAction.CallbackContext context){
            if (context.performed){
                _rawMoveInput = context.ReadValue<Vector2>();
                if (_rawMoveInput.magnitude > 1)
                    _rawMoveInput.Normalize();
                _isMoving = true;
                _hasDestination = false;
            }
            else if (context.canceled){
                _rawMoveInput = Vector2.zero;
                if (!_hasDestination)
                    _isMoving = false;
            }

            Vector3 direction = Compass.Forward * _rawMoveInput.y + Compass.Right * _rawMoveInput.x;
            if (direction.sqrMagnitude < 0.001f)
                return;
            direction.Normalize();
            _targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        /// Read the input system to find out if the run button is being pressed
        public void OnRunInputUpdate(InputAction.CallbackContext context){
            if (context.started)
                _rawRunningInput = true;
            else if (context.canceled)
                _rawRunningInput = false;
        }
        
        /// Verify if the entity can move in the navmesh, if yes, then move forward or towards target destination.
        public void MoveForward(){
            if (!_isMoving) return;

            if (_hasDestination) {
                Vector3 toDest = _targetDestination - Entity.position;
                toDest.y = 0f;

                if (toDest.sqrMagnitude <= stoppingDistance * stoppingDistance) {
                    Stop();
                    return;
                }

                _targetRotation = Quaternion.LookRotation(toDest.normalized, Vector3.up);
            }

            float   speed           = moveSpeedScale * moveSpeedCurve.Evaluate(_speed);
            Vector3 forwardPosition = Entity.forward * (speed * Time.deltaTime) + Entity.transform.position;
            forwardPosition = Actions.ValidateAndConsumeMove(forwardPosition);
            NavMesh.SamplePosition(forwardPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas);
            Entity.position = hit.position;

        }

        private float CalculateAngularVelocity(Quaternion previousRotation){
            const float deadZone     = 0.05f; // tweak this — smaller = more sensitive
            const float maxTurnSpeed = 180f;  // degrees per second for normalization

            // Calculate rotation delta
            Quaternion deltaRotation = Entity.rotation * Quaternion.Inverse(previousRotation);
            deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 axis);

            // Convert to signed turn on Y axis only
            float signedTurn = Vector3.Dot(axis, Vector3.up) * Mathf.DeltaAngle(0f, angleInDegrees) / Time.deltaTime;

            // Normalize to -1..1 range
            float normalizedTurn = signedTurn / maxTurnSpeed;

            // --- Apply deadline ---
            if (Mathf.Abs(normalizedTurn) < deadZone)
                normalizedTurn = 0f;
            else
                normalizedTurn = Mathf.InverseLerp(deadZone, 1f, Mathf.Abs(normalizedTurn)) *
                                 Mathf.Sign(normalizedTurn);

            // Clamp just in case
            return Mathf.Clamp(normalizedTurn, -1f, 1f);
        }

        private float CalculateTargetSpeed(){
            float targetSpeed = _hasDestination ? 1.0f : _rawMoveInput.magnitude;
            float alignment   = Mathf.Abs(Vector3.Dot(Entity.forward, _targetRotation * Vector3.forward));

            targetSpeed *= Mathf.Lerp(0.75f, 1f, Mathf.Abs(alignment));
            if (_rawRunningInput)
                targetSpeed *= 2f;

            if (Mathf.Abs(targetSpeed) < 0.1f)
                targetSpeed = 0f;

            return targetSpeed;
        }

        private void Update(){
            _lastRotation   = Entity.rotation;
            Entity.rotation = Quaternion.Slerp(Entity.rotation, _targetRotation, rotateSpeed * Time.deltaTime);
            
            _speed = Mathf.Lerp(_speed, CalculateTargetSpeed(), 
                                accelerationCatchUpSpeed * 
                                Time.deltaTime);
            
            _rotation = Mathf.Lerp(_rotation,
                                   CalculateAngularVelocity(_lastRotation),
                                   rotationCatchUpSpeed * Time.deltaTime);
            
            Animator.SetFloat(AnimatorForward, _speed);
            Animator.SetFloat(AnimatorTurn,    _rotation);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos(){
            if (!Application.isPlaying)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(Entity.transform.position, _targetRotation * Vector3.forward);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Entity.transform.position, Entity.rotation * Vector3.forward);
        }
#endif
    }
}