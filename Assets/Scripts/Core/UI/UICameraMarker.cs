using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// Placed on the permanent UI Camera (in Boot scene).
    /// Exposes a fast static reference so any 3D base camera in additively loaded scenes
    /// can easily attach this camera to its URP camera stack.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class UICameraMarker : MonoBehaviour
    {
        public static UICameraMarker Instance { get; private set; }

        public Camera Camera { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Camera = GetComponent<Camera>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
