using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

namespace Characters.Player{
    public class Controller : Component{
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

        /// Read the input system to build a move input vector
        public void OnMoveInputUpdate(InputAction.CallbackContext context){
            if (context.performed){
                _rawMoveInput = context.ReadValue<Vector2>();
                if (_rawMoveInput.magnitude > 1)
                    _rawMoveInput.Normalize();
                _isMoving     = true;
            }
            else if (context.canceled){
                _rawMoveInput = Vector2.zero;
                _isMoving     = false;
            }
            SetTargetRotationFromInput();
        }

        /// Read the input system to find out if the run button is being pressed
        public void OnRunInputUpdate(InputAction.CallbackContext context){
            if (context.started)
                _rawRunningInput = true;
            else if (context.canceled)
                _rawRunningInput = false;
        }
        
        /// Build a Look rotation from the input vector
        private void SetTargetRotationFromInput(){
            Vector3 direction = Compass.Forward * _rawMoveInput.y + Compass.Right * _rawMoveInput.x;
            if(direction.sqrMagnitude < 0.001f)
                return;
            direction.Normalize();
            _targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        /// Verify if the entity can move in the navmesh, if yes, then move forward the look rotation direction.
        public void MoveForward(){
            if (!_isMoving) return;
            float speed = moveSpeedScale * moveSpeedCurve.Evaluate(_speed);
            Vector3 forwardPosition = Entity.forward * (speed * Time.deltaTime) + Entity.transform.position;
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
                normalizedTurn = Mathf.InverseLerp(deadZone, 1f, Mathf.Abs(normalizedTurn)) * Mathf.Sign(normalizedTurn);

            // Clamp just in case
            return Mathf.Clamp(normalizedTurn, -1f, 1f);
        }

        private float CalculateTargetSpeed(){
            float targetSpeed = _rawMoveInput.magnitude;
            float alignment   = Mathf.Abs(Vector3.Dot(Entity.forward, _targetRotation * Vector3.forward));
            
            targetSpeed *= Mathf.Lerp(0.75f, 1f, Mathf.Abs(alignment));
            if (_rawRunningInput)
                targetSpeed *= 2f;

            if (Mathf.Abs(targetSpeed) < 0.1f)
                targetSpeed = 0f;
            
            return targetSpeed;
        }
        
        private void Update(){
            //Face target rotation
            Quaternion previousRotation = Entity.rotation;
            Entity.rotation = Quaternion.Slerp(Entity.rotation, _targetRotation, rotateSpeed * Time.deltaTime);
            _speed = Mathf.Lerp(_speed, CalculateTargetSpeed(), accelerationCatchUpSpeed * Time.deltaTime);
            _rotation = Mathf.Lerp(_rotation, CalculateAngularVelocity(previousRotation), rotationCatchUpSpeed * Time.deltaTime);
            Animator.SetFloat(AnimatorForward, _speed);
            Animator.SetFloat(AnimatorTurn, _rotation);
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