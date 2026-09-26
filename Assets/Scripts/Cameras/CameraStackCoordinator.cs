using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Cameras{
    /// Coordinates URP camera stacking by maintaining active base camera and ordered overlay cameras.
    [DisallowMultipleComponent]
    public class CameraStackCoordinator : MonoBehaviour{
        public static CameraStackCoordinator Instance{ get; private set; }
        public static Camera                 ActiveBaseCamera{ get; private set; }
        public static event Action<Camera>   OnActiveBaseCameraChanged;

        [SerializeField] private Camera fallbackCamera;

        private static readonly List<Camera>       BASE_CAMERAS    = new();
        private static readonly List<OverlayEntry> OVERLAY_CAMERAS = new();

        private struct OverlayEntry{
            public Camera Camera;
            public int    Priority;
        }

        /// Initializes singleton instance and builds camera stack.
        private void Awake(){
            if (!Instance)
                Instance = this;
            else if (Instance != this){
                Destroy(gameObject);
                return;
            }

            RebuildStack();
        }

        /// Cleans up singleton instance on destroy.
        private void OnDestroy(){
            if (Instance == this) Instance = null;
        }

        /// Registers base camera in active pool and rebuilds stack.
        public static void RegisterBase(Camera baseCam){
            if (!BASE_CAMERAS.Contains(baseCam)) BASE_CAMERAS.Add(baseCam);
            Instance?.RebuildStack();
        }

        /// Unregisters base camera from pool and rebuilds stack.
        public static void UnregisterBase(Camera baseCam){
            BASE_CAMERAS.Remove(baseCam);
            Instance?.RebuildStack();
        }

        /// Registers overlay camera with priority and sorts stack.
        public static void RegisterOverlay(Camera overlayCam, int priority){
            OVERLAY_CAMERAS.RemoveAll(e => e.Camera == overlayCam);
            OVERLAY_CAMERAS.Add(new OverlayEntry{ Camera = overlayCam, Priority = priority });
            OVERLAY_CAMERAS.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            Instance?.RebuildStack();
        }

        /// Unregisters overlay camera and rebuilds stack.
        public static void UnregisterOverlay(Camera overlayCam){
            OVERLAY_CAMERAS.RemoveAll(e => e.Camera == overlayCam);
            Instance?.RebuildStack();
        }

        /// Rebuilds URP camera stack onto the highest priority active base camera.
        public void RebuildStack(){
            Camera activeBase = fallbackCamera;
            foreach (Camera cam in BASE_CAMERAS)
                if (cam && cam.gameObject.activeInHierarchy)
                    activeBase = cam;

            if (!activeBase) return;

            if (ActiveBaseCamera != activeBase){
                ActiveBaseCamera = activeBase;
                OnActiveBaseCameraChanged?.Invoke(activeBase);
            }

            if (fallbackCamera) fallbackCamera.enabled = (activeBase == fallbackCamera);

            foreach (Camera cam in BASE_CAMERAS)
                if (cam) cam.enabled = (cam == activeBase);

            UniversalAdditionalCameraData baseData = activeBase.GetUniversalAdditionalCameraData();
            baseData.cameraStack.Clear();

            foreach (OverlayEntry entry in OVERLAY_CAMERAS){
                if (!entry.Camera) continue;
                entry.Camera.enabled = true;
                baseData.cameraStack.Add(entry.Camera);
            }
        }
    }
}
