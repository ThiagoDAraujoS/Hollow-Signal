using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Core.CameraSystem{
    [DisallowMultipleComponent]
    public class CameraStackCoordinator : MonoBehaviour{
        public static CameraStackCoordinator Instance{ get; private set; }

        [SerializeField] private Camera fallbackCamera;

        private static readonly List<Camera>       _baseCameras    = new();
        private static readonly List<OverlayEntry> _overlayCameras = new();

        private struct OverlayEntry{
            public Camera Camera;
            public int    Priority;
        }

        private void Awake(){
            if (Instance != null && Instance != this){
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RebuildStack();
        }

        private void OnDestroy(){
            if (Instance == this)
                Instance = null;
        }

        public static void RegisterBase(Camera baseCam){
            if (!_baseCameras.Contains(baseCam))
                _baseCameras.Add(baseCam);

            Instance?.RebuildStack();
        }

        public static void UnregisterBase(Camera baseCam){
            _baseCameras.Remove(baseCam);
            Instance?.RebuildStack();
        }

        public static void RegisterOverlay(Camera overlayCam, int priority){
            _overlayCameras.RemoveAll(e => e.Camera == overlayCam);
            _overlayCameras.Add(new OverlayEntry{ Camera = overlayCam, Priority = priority });
            _overlayCameras.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            Instance?.RebuildStack();
        }

        public static void UnregisterOverlay(Camera overlayCam){
            _overlayCameras.RemoveAll(e => e.Camera == overlayCam);
            Instance?.RebuildStack();
        }

        public void RebuildStack(){
            Camera activeBase = fallbackCamera;
            foreach (Camera cam in _baseCameras){
                if (cam != null && cam.gameObject.activeInHierarchy)
                    activeBase = cam;
            }

            if (activeBase == null)
                return;

            if (fallbackCamera != null)
                fallbackCamera.enabled = (activeBase == fallbackCamera);

            foreach (Camera cam in _baseCameras){
                if (cam != null)
                    cam.enabled = (cam == activeBase);
            }

            UniversalAdditionalCameraData baseData = activeBase.GetUniversalAdditionalCameraData();
            baseData.cameraStack.Clear();

            foreach (OverlayEntry entry in _overlayCameras){
                if (entry.Camera != null){
                    entry.Camera.enabled = true;
                    baseData.cameraStack.Add(entry.Camera);
                }
            }
        }
    }
}
