using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace World{
    public class CameraController : MonoBehaviour{
        private static CameraController _instance;
        
        [Header("Input")][SerializeField] 
        private InputActionReference moveActionRef;

        [Header("Movement Settings")][SerializeField]
        private float moveSpeed = 15f;

        [Header("Camera Reference")][SerializeField]
        private Camera renderingCamera;

        private Vector2 _moveInput;
        
        [Header("CameraBounds")][SerializeField]
        private Vector2 mapsBoundsMin;
        [SerializeField]
        private Vector2 mapBoundsMax;

        public static  Vector2 MapsBoundsMin => _instance.mapsBoundsMin;
        private static Vector2 MapBoundsMax => _instance.mapBoundsMax;
        
        private void Awake(){
            if (_instance == null)
                _instance = this;
            if (renderingCamera == null)
                renderingCamera = Camera.main;
        }

        private void OnDestroy(){
            if (_instance == this) 
                _instance = null;
        }

        private void OnEnable(){
            if (moveActionRef == null) return;
            moveActionRef.action.performed += OnMovePerformed;
            moveActionRef.action.canceled  += OnMoveCanceled;
            moveActionRef.action.Enable();
        }

        private void OnDisable(){
            if (moveActionRef == null) return;
            moveActionRef.action.performed -= OnMovePerformed;
            moveActionRef.action.canceled  -= OnMoveCanceled;
            moveActionRef.action.Disable();
            _moveInput = Vector2.zero;
        }

        private void OnMovePerformed(InputAction.CallbackContext context) => _moveInput = context.ReadValue<Vector2>();

        private void OnMoveCanceled(InputAction.CallbackContext context) => _moveInput = Vector2.zero;

        private void Update(){
            if (_moveInput == Vector2.zero) return;
            MoveAnchor();
        }


        private void MoveAnchor(){
            Vector3 camForward = renderingCamera.transform.forward;
            Vector3 camRight   = renderingCamera.transform.right;
            camForward.y = 0f;
            camRight.y   = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = (camForward * _moveInput.y) + (camRight * _moveInput.x);
            Vector3 newPosition   = transform.position + moveDirection * (moveSpeed * Time.deltaTime);

            newPosition.x = Mathf.Clamp(newPosition.x, mapsBoundsMin.x, mapBoundsMax.x);
            newPosition.z = Mathf.Clamp(newPosition.z, mapsBoundsMin.y, mapBoundsMax.y);

            transform.position = newPosition;
        }
    }
}