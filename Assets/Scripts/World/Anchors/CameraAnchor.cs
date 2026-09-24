﻿using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using World.Actors.Player;

namespace World.Anchors{
    public class CameraAnchor : MonoBehaviour{
        private static CameraAnchor _instance;

        [Header("Input")] [SerializeField] private InputActionReference moveActionRef;

        [Header("Movement Settings")] [SerializeField]
        private float moveSpeed = 15f;

        [Header("Zoom Settings")] [SerializeField]
        private float zoomSpeed = 0.5f;

        [SerializeField] private float minZoom = 3f;
        [SerializeField] private float maxZoom = 6f;

        [Header("Camera Reference")] [SerializeField]
        private Camera renderingCamera;

        [SerializeField] private CinemachineCamera cinemachineCamera;

        private Vector2         _moveInput;
        private Transform       _trackedTarget;
        private List<Character> _trackedHeroes;

        [Header("Camera Bounds")] [SerializeField]
        private Bounds bounds = new();

        public Bounds                     CurrentBounds => bounds;
        public static event Action<float> OnZoom;

        /// Initializes singleton instance, default camera references, and retro hard stop component.
        private void Awake(){
            if (_instance == null)
                _instance = this;
            if (renderingCamera == null)
                renderingCamera = Camera.main;
            if (cinemachineCamera == null)
                cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
            EnsureRetroHardStop();
        }

        /// Cleans up singleton instance reference on destroy.
        private void OnDestroy(){
            if (_instance == this)
                _instance = null;
        }

        /// Sets the active world camera and finds the scene's virtual camera.
        public static void SetRenderingCamera(Camera camera){
            if (_instance == null) return;
            _instance.renderingCamera   = camera;
            _instance.cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
            _instance.EnsureRetroHardStop();
        }

        /// Ensures the virtual camera has the retro hard stop extension attached.
        private void EnsureRetroHardStop(){
            if (cinemachineCamera != null && !cinemachineCamera.TryGetComponent<CinemachineRetroHardStop>(out _))
                cinemachineCamera.gameObject.AddComponent<CinemachineRetroHardStop>();
        }

        /// Subscribes to move input actions and enables action map.
        private void OnEnable(){
            if (moveActionRef == null) return;
            moveActionRef.action.performed += OnMovePerformed;
            moveActionRef.action.canceled  += OnMoveCanceled;
            moveActionRef.action.Enable();
        }

        /// Unsubscribes from move input actions and disables action map.
        private void OnDisable(){
            if (moveActionRef == null) return;
            moveActionRef.action.performed -= OnMovePerformed;
            moveActionRef.action.canceled  -= OnMoveCanceled;
            moveActionRef.action.Disable();
            _moveInput = Vector2.zero;
        }

        /// Detaches target follow and reads pan direction vector on move action.
        private void OnMovePerformed(InputAction.CallbackContext context){
            _trackedTarget = null;
            _trackedHeroes = null;
            _moveInput     = context.ReadValue<Vector2>();
        }

        /// Resets move input vector when move action ends.
        private void OnMoveCanceled(InputAction.CallbackContext context) => _moveInput = Vector2.zero;

        /// Applies orthographic camera zoom step and invokes the OnZoom event.
        public static void Zoom(float scrollSign){
            if (_instance == null) return;
            _instance.ApplyZoom(scrollSign);
            OnZoom?.Invoke(scrollSign);
        }

        /// Adjusts and clamps orthographic camera lens size.
        private void ApplyZoom(float scrollSign){
            if (cinemachineCamera == null) return;

            LensSettings lens = cinemachineCamera.Lens;
            lens.OrthographicSize  = Mathf.Clamp(lens.OrthographicSize - scrollSign * zoomSpeed, minZoom, maxZoom);
            cinemachineCamera.Lens = lens;
        }

        /// Updates anchor position via manual input or clamped target tracking.
        private void LateUpdate(){
            if (_moveInput != Vector2.zero)
                MoveAnchor();
            else if (_trackedHeroes != null && _trackedHeroes.Count > 0)
                transform.position = bounds.Clamp(CalculateMidpoint(_trackedHeroes));
            else if (_trackedTarget != null)
                transform.position = bounds.Clamp(_trackedTarget.position);
        }

        /// Calculates center point of all tracked heroes.
        private static Vector3 CalculateMidpoint(List<Character> heroes){
            Vector3 sum = Vector3.zero;
            foreach (Character hero in heroes)
                sum += hero.WorldPosition;
            return sum / heroes.Count;
        }

        /// Configures camera anchor position and map boundaries when a new map loads.
        public static void SetUpCamera(Vector3 position, Bounds newBounds){
            _instance.bounds             = newBounds;
            _instance.transform.position = _instance.bounds.Clamp(position);
        }

        /// Sets the transform target for the camera anchor to follow.
        public static void Track(Transform target) => _instance._trackedTarget = target;

        /// Commands camera anchor to track the midpoint of a group of heroes.
        public static void FollowHeroes(List<Character> heroes) => _instance._trackedHeroes = heroes;

        /// Stops tracking hero group.
        public static void StopFollow() => _instance._trackedHeroes = null;

        /// Clears the camera anchor's tracked target.
        public static void Detach(){
            _instance._trackedTarget = null;
            _instance._trackedHeroes = null;
        }

        /// Translates camera anchor according to input direction relative to rendering camera angle.
        private void MoveAnchor(){
            Vector3 camForward = renderingCamera.transform.forward;
            Vector3 camRight   = renderingCamera.transform.right;
            camForward.y = 0f;
            camRight.y   = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = (camForward * _moveInput.y) + (camRight * _moveInput.x);
            Vector3 newPosition   = transform.position + moveDirection * (moveSpeed * Time.deltaTime);

            transform.position = bounds.Clamp(newPosition);
        }
    }

    [Serializable]
    public class Bounds{
        [Tooltip("Minimum X (horizontal) and Y (depth/Z in world).")]
        public Vector2 min = new(-100f, -100f);

        [Tooltip("Maximum X (horizontal) and Y (depth/Z in world).")]
        public Vector2 max = new(100f, 100f);

        public Bounds(){ }

        public Bounds(Vector2 min, Vector2 max){
            this.min = min;
            this.max = max;
        }

        /// Clamps a 3D position's horizontal and depth coordinates within the 2D bounding rectangle.
        public Vector3 Clamp(Vector3 target) =>
            new(Mathf.Clamp(target.x, min.x, max.x), target.y, Mathf.Clamp(target.z, min.y, max.y));

        /// Calculates center and size of the bounding area at a given world elevation.
        public (Vector3 center, Vector3 size) GetCenterAndSize(float y) =>
            (new Vector3((min.x + max.x) * 0.5f, y, (min.y + max.y) * 0.5f), new Vector3(max.x - min.x, 0.1f, max.y - min.y));
    }
}
