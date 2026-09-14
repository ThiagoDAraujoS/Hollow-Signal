using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Cameras{
    [DisallowMultipleComponent]
    public class CameraStackCoordinator : MonoBehaviour{
        public static CameraStackCoordinator Instance{ get; private set; }

        [SerializeField] private Camera fallbackCamera;

        private static readonly List<Camera>       BASE_CAMERAS    = new();
        private static readonly List<OverlayEntry> OVERLAY_CAMERAS = new();

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
            if (!BASE_CAMERAS.Contains(baseCam))
                BASE_CAMERAS.Add(baseCam);
            Instance?.RebuildStack();
        }

        public static void UnregisterBase(Camera baseCam){
            BASE_CAMERAS.Remove(baseCam);
            Instance?.RebuildStack();
        }

        public static void RegisterOverlay(Camera overlayCam, int priority){
            OVERLAY_CAMERAS.RemoveAll(e => e.Camera == overlayCam);
            OVERLAY_CAMERAS.Add(new OverlayEntry{ Camera = overlayCam, Priority = priority });
            OVERLAY_CAMERAS.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            Instance?.RebuildStack();
        }

        public static void UnregisterOverlay(Camera overlayCam){
            OVERLAY_CAMERAS.RemoveAll(e => e.Camera == overlayCam);
            Instance?.RebuildStack();
        }

        public void RebuildStack(){
            Camera activeBase = fallbackCamera;
            foreach (Camera cam in BASE_CAMERAS)
                if (cam != null && cam.gameObject.activeInHierarchy)
                    activeBase = cam;

            if (activeBase == null)
                return;

            if (fallbackCamera != null)
                fallbackCamera.enabled = (activeBase == fallbackCamera);

            foreach (Camera cam in BASE_CAMERAS)
                if (cam != null)
                    cam.enabled = (cam == activeBase);
            
            UniversalAdditionalCameraData baseData = activeBase.GetUniversalAdditionalCameraData();
            baseData.cameraStack.Clear();

            foreach (OverlayEntry entry in OVERLAY_CAMERAS){
                if (entry.Camera == null) continue;
                entry.Camera.enabled = true;
                baseData.cameraStack.Add(entry.Camera);
            }
        }
    }
}
