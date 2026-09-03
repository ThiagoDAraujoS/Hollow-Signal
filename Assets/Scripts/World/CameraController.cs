using UnityEngine;

namespace World {
    /// A smooth-following isometric camera controller.
    public class CameraController : MonoBehaviour {
        /// The target the camera should currently center on. Null is a valid state if no character is selected.
        public Transform target;

        /// The fixed isometric positional offset relative to the target.
        public Vector3 offset = new Vector3(-10f, 15f, -10f);

        /// Smooth interpolation factor for panning.
        public float smoothSpeed = 5f;

        private void LateUpdate() {
            if (target == null) return;
            
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.LookAt(target);
        }
    }
}