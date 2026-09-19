using UnityEngine;

namespace Utilities{
    public class RotateTo : MonoBehaviour{
        public float speed = 5f;
        public Vector3 rot1;
        public Vector3 rot2;
        public Vector3 target;

        /// <summary>Initializes target rotation to rot1.</summary>
        private void Start() => target = rot1;

        /// <summary>Interpolates rotation spherically towards the target.</summary>
        private void LateUpdate(){
            Quaternion targetRot = Quaternion.Euler(target);
            if (Quaternion.Angle(transform.localRotation, targetRot) > 0.01f)
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, speed * Time.deltaTime);
        }

        /// <summary>Sets target to rot1.</summary>
        [ContextMenu("Set Target Rot 1")]
        public void SetTargetRot1() => target = rot1;

        /// <summary>Sets target to rot2.</summary>
        [ContextMenu("Set Target Rot 2")]
        public void SetTargetRot2() => target = rot2;
    }
}
