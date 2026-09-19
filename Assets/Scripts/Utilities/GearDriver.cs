using UnityEngine;

namespace Utilities{
    public class GearDriver : MonoBehaviour{
        public Transform slave;
        public Vector3 axis = Vector3.forward;
        public float teeth = 24;
        public float slaveTeeth = 12;

        private float _lastAngle;

        /// <summary>Captures starting angle.</summary>
        private void Start() => _lastAngle = Vector3.Dot(transform.localEulerAngles, axis);

        /// <summary>Rotates slave based on driver tooth ratio.</summary>
        private void LateUpdate(){
            float current = Vector3.Dot(transform.localEulerAngles, axis);
            slave.Rotate(axis * (-Mathf.DeltaAngle(_lastAngle, current) * (teeth / slaveTeeth)), Space.Self);
            _lastAngle = current;
        }
    }
}
