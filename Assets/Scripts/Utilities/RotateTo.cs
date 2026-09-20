using UnityEngine;

namespace Utilities{
    public class RotateTo : MonoBehaviour{
        public float speed = 90f;
        public Vector3 rot1;
        public Vector3 rot2;
        public Vector3 target;
        public AnimationCurve curve = AnimationCurve.Linear(0, 1, 1, 1);

        private float _totalAngle;
        private Vector3 _lastTarget;

        /// <summary>Rotates towards target using RotateTowards modulated by an animation curve.</summary>
        private void LateUpdate(){
            if (target != _lastTarget){
                _lastTarget = target;
                _totalAngle = Mathf.Max(0.01f, Quaternion.Angle(transform.localRotation, Quaternion.Euler(target)));
            }

            Quaternion targetRot = Quaternion.Euler(target);
            float remaining = Quaternion.Angle(transform.localRotation, targetRot);
            if (remaining > 0.01f){
                float progress = 1f - Mathf.Clamp01(remaining / _totalAngle);
                float step = Mathf.Max(0.05f, curve.Evaluate(progress)) * speed * Time.deltaTime;
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRot, step);
            }
        }

        /// <summary>Sets target to rot1.</summary>
        [ContextMenu("Set Target Rot 1")]
        public void SetTargetRot1() => target = rot1;

        /// <summary>Sets target to rot2.</summary>
        [ContextMenu("Set Target Rot 2")]
        public void SetTargetRot2() => target = rot2;
    }
}
