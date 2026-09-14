using UnityEngine;

namespace Utilities
{
    [ExecuteAlways]
    [AddComponentMenu("Diorama/Parallax Frustum Fitter")]
    public class ParallaxFrustumFitter : MonoBehaviour
    {
        [Header("Target Setup")]
        [Tooltip("The camera used to view the diorama. If left empty, Camera.main will be used. Can be a reference camera (even if disabled).")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Parent transform containing all the parallax quads. If null, this GameObject's children will be used.")]
        [SerializeField] private Transform quadsParent;

        [Header("Aspect Ratio (Default 16:9 for 1920x1080)")]
        [SerializeField] private float targetWidth = 1920f;
        [SerializeField] private float targetHeight = 1080f;

        [Header("Leeway / Bleed Margin")]
        [Tooltip("Extra scale multiplier to prevent seeing screen borders when the camera pans around. 1.0 = exact screen fit, 1.10 = 10% safety overscan padding.")]
        [Range(1.0f, 1.5f)]
        [SerializeField] private float overscanMultiplier = 1.10f;

        [Header("3D Alignment & Orientation")]
        [Tooltip("Center each quad directly along the camera's sight-line at its current depth distance (works regardless of camera position or angle).")]
        [SerializeField] private bool centerOnCameraView = true;

        [Tooltip("Rotate each quad to perfectly face the camera lens.")]
        [SerializeField] private bool alignRotationToCamera = true;

        public float AspectRatio => targetHeight > 0 ? (targetWidth / targetHeight) : (16f / 9f);
        public float OverscanMultiplier => overscanMultiplier;

        private void Reset()
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
            quadsParent = transform;
        }

        [ContextMenu("Fit All Parallax Planes Now")]
        public void FitAllPlanes()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                Debug.LogError("[ParallaxFrustumFitter] No Camera assigned or found in scene!");
                return;
            }

            Transform container = quadsParent != null ? quadsParent : transform;
            var renderers = container.GetComponentsInChildren<MeshRenderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[ParallaxFrustumFitter] No MeshRenderers found under {container.name}!");
                return;
            }

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(container, "Fit Parallax Planes");
#endif

            Transform camTransform = targetCamera.transform;
            Vector3 camPos = camTransform.position;
            Vector3 camFwd = camTransform.forward;
            Quaternion camRot = camTransform.rotation;

            int count = 0;
            foreach (var mr in renderers)
            {
                if (mr.gameObject == targetCamera.gameObject) continue;

                Transform t = mr.transform;

#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(t, "Fit Parallax Quad");
#endif

                // Measure depth distance strictly along the camera's forward sight-line in world space
                Vector3 toPlane = t.position - camPos;
                float distance = Vector3.Dot(toPlane, camFwd);

                if (distance <= 0.01f)
                {
                    Debug.LogWarning($"[ParallaxFrustumFitter] Quad '{t.name}' is behind or at camera level (distance={distance:F2}). Skipping.", t);
                    continue;
                }

                // 1. Calculate Frustum dimensions at this plane's depth
                float frustumHeight;
                if (targetCamera.orthographic)
                {
                    frustumHeight = targetCamera.orthographicSize * 2f;
                }
                else
                {
                    frustumHeight = 2.0f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                }

                float frustumWidth = frustumHeight * AspectRatio;

                // Apply overscan bleed margin
                frustumHeight *= overscanMultiplier;
                frustumWidth *= overscanMultiplier;

                // 2. Rotate to face camera lens
                if (alignRotationToCamera)
                {
                    t.rotation = camRot;
                }

                // 3. Center plane directly onto camera's center sight-line at this distance
                if (centerOnCameraView)
                {
                    t.position = camPos + (camFwd * distance);
                }

                // 4. Apply scale (account for parent scale if nested)
                Vector3 parentScale = t.parent != null ? t.parent.lossyScale : Vector3.one;
                float scaleX = parentScale.x != 0 ? (frustumWidth / parentScale.x) : frustumWidth;
                float scaleY = parentScale.y != 0 ? (frustumHeight / parentScale.y) : frustumHeight;

                t.localScale = new Vector3(scaleX, scaleY, 1.0f);
                count++;
            }

            float paddingPercent = (overscanMultiplier - 1.0f) * 100f;
            Debug.Log($"<color=#4AFF70><b>[ParallaxFrustumFitter]</b></color> Successfully fitted {count} plane(s) to camera '{targetCamera.name}' (Pos={camPos}, Rot={camTransform.eulerAngles}) with +{paddingPercent:F0}% overscan bleed margin!");
        }
    }
}
