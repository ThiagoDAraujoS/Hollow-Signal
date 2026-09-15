using UnityEngine;
using World.Actors.Brains;
using World.Anchors;

namespace Cameras{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class RegisterBaseCamera : MonoBehaviour{
        private Camera _camera;

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnEnable(){
            CameraStackCoordinator.RegisterBase(_camera);
            PlayerBrain.SetCamera(_camera);
            CameraAnchor.SetRenderingCamera(_camera);
        }

        private void OnDisable() => CameraStackCoordinator.UnregisterBase(_camera);
    }
}
