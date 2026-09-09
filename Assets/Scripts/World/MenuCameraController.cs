using Core.UI;
using UnityEngine;

namespace World {
    /// Observes menu UI viewpoint changes and aligns the diorama scene camera to designated anchor proxies.
    /// Resides strictly within diorama title scenes (e.g. CoolMenuScene) and decouples 3D rendering from UI logic.
    [DisallowMultipleComponent]
    public class MenuCameraController : MonoBehaviour {
        [Header("Rendering Camera")]
        [SerializeField] private Camera renderingCamera;

        [Header("Anchors (0: Main, 1: Load, 2: Settings, 3: Credits)")]
        [SerializeField] private Transform[] anchors;

        [Header("Motion Configuration")]
        [SerializeField] private bool snapInstant = false;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 6f;

        private Transform _currentTarget;

        private void Awake() {
            if (renderingCamera == null)
                renderingCamera = Camera.main;
        }

        private void OnEnable() {
            MenuEvents.OnMenuViewStateChanged += HandleViewStateChanged;
        }

        private void OnDisable() {
            MenuEvents.OnMenuViewStateChanged -= HandleViewStateChanged;
        }

        private void Start() {
            // Instantly snap to default main viewpoint on launch
            SnapTo(MenuViewState.Main);
        }

        private void LateUpdate() {
            if (_currentTarget == null || snapInstant) return;

            renderingCamera.transform.position = Vector3.Lerp(
                renderingCamera.transform.position,
                _currentTarget.position,
                Time.deltaTime * moveSpeed
            );

            renderingCamera.transform.rotation = Quaternion.Slerp(
                renderingCamera.transform.rotation,
                _currentTarget.rotation,
                Time.deltaTime * rotationSpeed
            );
        }

        /// Instantly snaps the camera to the anchor associated with a given viewpoint.
        public void SnapTo(MenuViewState state) {
            int index = (int)state;
            if (index < 0 || index >= anchors.Length || anchors[index] == null) return;

            _currentTarget = anchors[index];
            renderingCamera.transform.position = _currentTarget.position;
            renderingCamera.transform.rotation = _currentTarget.rotation;
        }

        private void HandleViewStateChanged(MenuViewState state) {
            int index = (int)state;
            if (index < 0 || index >= anchors.Length || anchors[index] == null) return;

            _currentTarget = anchors[index];
            if (snapInstant) {
                renderingCamera.transform.position = _currentTarget.position;
                renderingCamera.transform.rotation = _currentTarget.rotation;
            }
        }
    }
}
