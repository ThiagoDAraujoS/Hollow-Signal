using UnityEngine;
using World.Anchors;

namespace Cameras{
    /// Registers host camera as the primary base camera in the camera stack and anchor system.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class RegisterBaseCamera : MonoBehaviour{
        private Camera _camera;

        /// Caches attached camera component.
        private void Awake() => _camera = GetComponent<Camera>();

        /// Registers camera as base with coordinator and anchor.
        private void OnEnable(){
            if (!_camera) _camera = GetComponent<Camera>();
            CameraStackCoordinator.RegisterBase(_camera);
            CameraAnchor.SetRenderingCamera(_camera);
        }

        /// Unregisters camera from coordinator on disable.
        private void OnDisable() => CameraStackCoordinator.UnregisterBase(_camera);
    }
}
