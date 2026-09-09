using System.Collections;
using Core.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace World
{
    /// <summary>
    /// Automatically discovers the persistent UI Camera from the Boot scene
    /// and attaches it to this 3D Camera's URP Camera Stack.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class AddUICameraToStack : MonoBehaviour
    {
        [Header("Discovery Settings")]
        [Tooltip("Optional: Tag used on the UI Camera in Boot scene as a fallback")]
        [SerializeField] private string uiCameraTag = "UICamera";

        [Tooltip("Try finding the UI Camera again across frames if Boot scene is still loading")]
        [SerializeField] private bool retryUntilFound = true;
        [SerializeField] private float retryInterval = 0.1f;
        [SerializeField] private float maxRetryDuration = 3f;

        private Camera _baseCamera;
        private UniversalAdditionalCameraData _cameraData;
        private Coroutine _discoveryCoroutine;

        private void Awake()
        {
            _baseCamera = GetComponent<Camera>();
            _cameraData = _baseCamera.GetUniversalAdditionalCameraData();
        }

        private void OnEnable()
        {
            TryBindUICamera();
        }

        private void OnDisable()
        {
            if (_discoveryCoroutine != null)
            {
                StopCoroutine(_discoveryCoroutine);
                _discoveryCoroutine = null;
            }
        }

        /// <summary>
        /// Attempts to locate the UI camera and add it to the camera stack.
        /// </summary>
        public void TryBindUICamera()
        {
            if (_cameraData == null) return;

            Camera uiCam = FindUICamera();
            if (uiCam != null)
            {
                AttachToStack(uiCam);
            }
            else if (retryUntilFound && gameObject.activeInHierarchy)
            {
                if (_discoveryCoroutine != null)
                    StopCoroutine(_discoveryCoroutine);

                _discoveryCoroutine = StartCoroutine(RetryDiscoveryRoutine());
            }
        }

        private IEnumerator RetryDiscoveryRoutine()
        {
            float elapsed = 0f;
            while (elapsed < maxRetryDuration)
            {
                yield return new WaitForSeconds(retryInterval);
                elapsed += retryInterval;

                Camera uiCam = FindUICamera();
                if (uiCam != null)
                {
                    AttachToStack(uiCam);
                    _discoveryCoroutine = null;
                    yield break;
                }
            }

            Debug.LogWarning($"[AddUICameraToStack] Could not locate UI Camera after {maxRetryDuration}s on {_baseCamera.name}.", this);
            _discoveryCoroutine = null;
        }

        private Camera FindUICamera()
        {
            // 1. Direct static lookup via marker
            if (UICameraMarker.Instance != null && UICameraMarker.Instance.Camera != null)
                return UICameraMarker.Instance.Camera;

            // 2. Lookup by tag if defined
            if (!string.IsNullOrEmpty(uiCameraTag))
            {
                try
                {
                    var tagged = GameObject.FindWithTag(uiCameraTag);
                    if (tagged != null && tagged.TryGetComponent<Camera>(out var cam))
                        return cam;
                }
                catch
                {
                    // Tag might not be registered in TagManager, safely fallback
                }
            }

            // 3. Fallback: Search all active cameras for an Overlay camera rendering UI layer
            int uiLayerMask = 1 << LayerMask.NameToLayer("UI");
            foreach (var cam in Camera.allCameras)
            {
                var additionalData = cam.GetUniversalAdditionalCameraData();
                if (additionalData != null && additionalData.renderType == CameraRenderType.Overlay)
                {
                    if ((cam.cullingMask & uiLayerMask) != 0)
                        return cam;
                }
            }

            return null;
        }

        private void AttachToStack(Camera uiCam)
        {
            if (_cameraData == null || uiCam == null) return;

            if (!_cameraData.cameraStack.Contains(uiCam))
            {
                _cameraData.cameraStack.Add(uiCam);
            }
        }
    }
}
