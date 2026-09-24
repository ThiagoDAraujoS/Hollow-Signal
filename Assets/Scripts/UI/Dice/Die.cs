using UnityEngine;

namespace UI.Dice{
    [RequireComponent(typeof(Rigidbody))]
    public class Die : MonoBehaviour{
        public readonly struct Face{
            public readonly int value;
            public readonly Vector3 eulerAngles;
            public readonly Vector3 normal;

            public Face(int val, Vector3 rot, Vector3 norm){
                value = val;
                eulerAngles = rot;
                normal = norm;
            }
        }

        [Header("Reading Vector")]
        public Vector3 readingUpVector = Vector3.up;

        private static readonly Face[] Faces = {
            new(1, new(0f, 0f, 0f),     new(0f, 0f, 1f)),
            new(2, new(-90f, 0f, 90f),  new(-1f, 0f, 0f)),
            new(3, new(-90f, 0f, 180f), new(0f, 1f, 0f)),
            new(4, new(-90f, 0f, 0f),   new(0f, -1f, 0f)),
            new(5, new(-90f, 0f, -90f), new(1f, 0f, 0f)),
            new(6, new(0f, 180f, 180f), new(0f, 0f, -1f))
        };

        private Rigidbody _rb;

        /// Caches Rigidbody reference.
        private void Awake() => _rb = GetComponent<Rigidbody>();

        /// Checks if die has settled below linear and angular motion thresholds.
        public bool IsSettled(float linearThreshold = 0.05f, float angularThreshold = 0.1f) =>
            _rb.linearVelocity.sqrMagnitude < linearThreshold * linearThreshold &&
            _rb.angularVelocity.sqrMagnitude < angularThreshold * angularThreshold;

        /// Toggles Rigidbody kinematic state for showcase positioning.
        public void SetKinematic(bool kinematic) => _rb.isKinematic = kinematic;

        /// Identifies the face most aligned with the reading vector via dot product.
        public Face GetWinningFace(){
            Face winningFace = Faces[0];
            float highestDot = float.MinValue;

            foreach (Face face in Faces){
                float dot = Vector3.Dot(transform.TransformDirection(face.normal), readingUpVector);
                if (dot > highestDot){
                    highestDot = dot;
                    winningFace = face;
                }
            }

            return winningFace;
        }

        /// Computes rotation needed so the winning face points upright directly at the camera.
        public Quaternion GetShowcaseRotation(Face face, Vector3 cameraForward, Vector3 cameraUp) =>
            Quaternion.LookRotation(-cameraForward, cameraUp) * Quaternion.Euler(face.eulerAngles);
    }
}
