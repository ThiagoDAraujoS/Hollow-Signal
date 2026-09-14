using UnityEngine;

namespace Core.CameraSystem{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class RegisterBaseCamera : MonoBehaviour{
        private Camera _camera;

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnEnable() => CameraStackCoordinator.RegisterBase(_camera);

        private void OnDisable() => CameraStackCoordinator.UnregisterBase(_camera);
    }
}
