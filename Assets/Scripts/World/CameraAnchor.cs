﻿using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace World {
    public class CameraAnchor : MonoBehaviour {
        private static CameraAnchor _instance;

        [Header("Input")]
        [SerializeField] private InputActionReference moveActionRef;
        [SerializeField] private InputActionReference zoomActionRef;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 15f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 6f;

        [Header("Camera Reference")]
        [SerializeField] private Camera renderingCamera;
        [SerializeField] private CinemachineCamera cinemachineCamera;

        private Vector2   _moveInput;
        private Transform _trackedTarget;

        [Header("Camera Bounds")]
        [SerializeField] private Bounds bounds = new();

        public Bounds CurrentBounds => bounds;
        public static event Action<float> OnZoom;

        private void Awake() {
            if (_instance == null)
                _instance = this;
            if (renderingCamera == null)
                renderingCamera = Camera.main;
            if (cinemachineCamera == null)
                cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        private void OnDestroy() {
            if (_instance == this)
                _instance = null;
        }

        private void OnEnable() {
            if (moveActionRef != null) {
                moveActionRef.action.performed += OnMovePerformed;
                moveActionRef.action.canceled += OnMoveCanceled;
                moveActionRef.action.Enable();
            }

            if (zoomActionRef == null) return;
            zoomActionRef.action.performed += OnZoomPerformed;
            zoomActionRef.action.Enable();
        }

        private void OnDisable() {
            if (moveActionRef != null) {
                moveActionRef.action.performed -= OnMovePerformed;
                moveActionRef.action.canceled -= OnMoveCanceled;
                moveActionRef.action.Disable();
                _moveInput = Vector2.zero;
            }

            if (zoomActionRef == null) return;
            zoomActionRef.action.performed -= OnZoomPerformed;
            zoomActionRef.action.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext context) {
            _trackedTarget = null;
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context) => _moveInput = Vector2.zero;

        private void OnZoomPerformed(InputAction.CallbackContext context) {
            float scrollDelta = context.ReadValue<Vector2>().y;
            if (Mathf.Abs(scrollDelta) < 0.01f) return;

            float scrollSign = Mathf.Sign(scrollDelta);
            ApplyZoom(scrollSign);
            OnZoom?.Invoke(scrollSign);
        }

        private void ApplyZoom(float scrollSign) {
            if (cinemachineCamera == null) return;

            LensSettings lens = cinemachineCamera.Lens;
            lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize - scrollSign * zoomSpeed, minZoom, maxZoom);
            cinemachineCamera.Lens = lens;
        }

        private void Update() {
            if (_moveInput == Vector2.zero) return;
            MoveAnchor();
        }

        private void LateUpdate() {
            if (_trackedTarget == null) return;
            transform.position = bounds.Clamp(_trackedTarget.position);
        }

        /// <summary>
        /// Configures the camera anchor's initial position and map boundaries when a new map is loaded.
        /// </summary>
        public static void SetUpCamera(Vector3 position, Bounds newBounds) {
            _instance.bounds = newBounds;
            _instance.transform.position = _instance.bounds.Clamp(position);
        }

        public static void Track(Transform target) => _instance._trackedTarget = target;

        public static void Detach() => _instance._trackedTarget = null;

        private void MoveAnchor() {
            Vector3 camForward = renderingCamera.transform.forward;
            Vector3 camRight = renderingCamera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = (camForward * _moveInput.y) + (camRight * _moveInput.x);
            Vector3 newPosition = transform.position + moveDirection * (moveSpeed * Time.deltaTime);

            newPosition = bounds.Clamp(newPosition);
            transform.position = newPosition;
        }

        private void OnDrawGizmos() {
            if (bounds == null) return;

            Gizmos.color = Color.yellow;
            (Vector3 center, Vector3 size) = bounds.GetCenterAndSize(transform.position.y);
            Gizmos.DrawWireCube(center, size);
        }
    }

    [Serializable]
    public class Bounds {
        [Tooltip("Minimum X (horizontal) and Y (depth/Z in world).")]
        public Vector2 min = new(-100f, -100f);

        [Tooltip("Maximum X (horizontal) and Y (depth/Z in world).")]
        public Vector2 max = new(100f, 100f);

        public Bounds() { }

        public Bounds(Vector2 min, Vector2 max) {
            this.min = min;
            this.max = max;
        }

        /// <summary>
        /// Calculates and returns the center and size of the bounding area for use with Gizmos or calculations.
        /// </summary>
        public (Vector3 center, Vector3 size) GetCenterAndSize(float elevation = 0f) {
            Vector3 center = new((min.x + max.x) * 0.5f, elevation, (min.y + max.y) * 0.5f);
            Vector3 size = new(Mathf.Abs(max.x - min.x), 0.1f, Mathf.Abs(max.y - min.y));
            return (center, size);
        }

        /// <summary>
        /// Clamps a 3D position's X and Z coordinates within the 2D bounding limits.
        /// </summary>
        public Vector3 Clamp(Vector3 position) {
            position.x = Mathf.Clamp(position.x, Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x));
            position.z = Mathf.Clamp(position.z, Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y));
            return position;
        }
    }
}
