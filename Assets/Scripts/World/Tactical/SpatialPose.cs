using System;
using UnityEngine;

namespace World.Tactical{
    /// Represents a localized spatial position and orientation anchor in world or local space.
    [Serializable]
    public struct SpatialPose{
        public Vector3 localPosition;
        public Vector3 localEulerAngles;

        public SpatialPose(Vector3 position, Vector3 eulerAngles){
            localPosition    = position;
            localEulerAngles = eulerAngles;
        }

        public static SpatialPose Default => new(new Vector3(0f, 0f, 1f), new Vector3(0f, 180f, 0f));

        /// Evaluates world position relative to owner transform.
        public Vector3 GetWorldPosition(Transform parent) => parent.TransformPoint(localPosition);

        /// Evaluates world rotation relative to owner transform.
        public Quaternion GetWorldRotation(Transform parent) => parent.rotation * Quaternion.Euler(localEulerAngles);

        /// Evaluates world forward facing vector.
        public Vector3 GetForward(Transform parent) => GetWorldRotation(parent) * Vector3.forward;

        /// Updates local pose from world position and rotation.
        public void SetFromWorld(Transform parent, Vector3 worldPos, Quaternion worldRot){
            localPosition    = parent.InverseTransformPoint(worldPos);
            localEulerAngles = (Quaternion.Inverse(parent.rotation) * worldRot).eulerAngles;
        }
    }
}
