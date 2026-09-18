using Unity.Cinemachine;
using UnityEngine;

namespace World.Anchors{
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Retro Hard Stop")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CinemachineRetroHardStop : CinemachineExtension{
        [Header("Velocity Cutoff")]
        [Tooltip("Hard-stops the camera when its movement speed drops below this value (units/sec).")]
        [SerializeField] private float minSpeedThreshold = 0.5f;

        [Header("Pixel Snapping")]
        [Tooltip("Snaps camera position to exact retro pixel increments to prevent subpixel jitter.")]
        [SerializeField] private bool snapToPixelGrid = true;

        [Tooltip("Retro pixel size matching the RetroBayerLUT post-process (e.g. 2).")]
        [SerializeField] private int pixelSize = 2;

        private Vector3 _lastPosition;
        private bool    _hasLastPosition;
        private bool    _isHardStopped;

        public bool  IsHardStopped     => _isHardStopped;
        public float MinSpeedThreshold { get => minSpeedThreshold; set => minSpeedThreshold = value; }

        /// Resets previous camera tracking position and hard stop flag.
        protected override void OnEnable(){
            base.OnEnable();
            _hasLastPosition = false;
            _isHardStopped   = false;
        }

        /// Applies velocity hard-stopping and orthographic pixel snapping in the Cinemachine body pipeline stage.
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage        stage,
            ref CameraState              state,
            float                        deltaTime){
            if (stage != CinemachineCore.Stage.Body) return;

            if (deltaTime < 0f){
                _hasLastPosition = false;
                _isHardStopped   = false;
                return;
            }

            Vector3 desiredPos = state.RawPosition;

            if (!_hasLastPosition){
                _lastPosition    = desiredPos;
                _hasLastPosition = true;
                return;
            }

            if (deltaTime > 0.0001f){
                float speed = Vector3.Distance(desiredPos, _lastPosition) / deltaTime;

                if (speed < minSpeedThreshold){
                    _isHardStopped    = true;
                    state.RawPosition = _lastPosition;
                }
                else{
                    _isHardStopped    = false;
                    _lastPosition     = desiredPos;
                }
            }
            else if (_isHardStopped)
                state.RawPosition = _lastPosition;

            if (snapToPixelGrid && state.Lens.Orthographic)
                state.RawPosition = SnapToPixelGrid(state.RawPosition, state.RawOrientation, state.Lens.OrthographicSize);
        }

        /// Snaps position to world units matching retro virtual pixel resolution.
        private Vector3 SnapToPixelGrid(Vector3 position, Quaternion orientation, float orthoSize){
            float screenHeight = Screen.height > 0 ? Screen.height : 1080f;
            float retroHeight  = screenHeight / Mathf.Max(1, pixelSize);
            float unitPerPixel = (2f * orthoSize) / retroHeight;

            if (unitPerPixel <= 0.00001f) return position;

            Vector3 camRight   = orientation * Vector3.right;
            Vector3 camUp      = orientation * Vector3.up;
            Vector3 camForward = orientation * Vector3.forward;

            float x = Vector3.Dot(position, camRight);
            float y = Vector3.Dot(position, camUp);
            float z = Vector3.Dot(position, camForward);

            float xSnapped = Mathf.Round(x / unitPerPixel) * unitPerPixel;
            float ySnapped = Mathf.Round(y / unitPerPixel) * unitPerPixel;

            return (camRight * xSnapped) + (camUp * ySnapped) + (camForward * z);
        }
    }
}
