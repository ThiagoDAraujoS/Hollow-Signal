using UnityEngine;

namespace Cameras{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class RegisterOverlayCamera : MonoBehaviour{
        [SerializeField] private int stackOrder = 100;

        private Camera _camera;

        public int StackOrder => stackOrder;

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnEnable() => CameraStackCoordinator.RegisterOverlay(_camera, stackOrder);

        private void OnDisable() => CameraStackCoordinator.UnregisterOverlay(_camera);
    }
}
