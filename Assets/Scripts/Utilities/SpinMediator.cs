using System;
using UnityEngine;

namespace Utilities{
    [Serializable]
    public class SpinProfile{
        public bool mute;
        public float period = 1f;
        public AnimationCurve step = AnimationCurve.Linear(0, 1, 1, 1);
        public Vector3 speed;

        /// <summary>Calculates the rotation delta for this profile given forward direction and master modifier.</summary>
        public Vector3 Evaluate(float time, float dirMultiplier, float speedModifier){
            // Breaking fail-fast paradigm here to allow zero period in inspector without crashing with NaN rotation.
            if (mute || period <= 0f) return Vector3.zero;
            float t = (time % period) / period;
            return speed * (step.Evaluate(t) * dirMultiplier * speedModifier * Time.deltaTime);
        }
    }

    public class SpinMediator : MonoBehaviour{
        public bool forward = true;
        public float speedModifier = 1f;

        public SpinProfile profile1 = new();
        public SpinProfile profile2 = new();
        public SpinProfile profile3 = new();
        public SpinProfile profile4 = new();

        /// <summary>Composes the total rotation delta from all 4 profiles and applies it.</summary>
        private void LateUpdate(){
            if (Mathf.Approximately(speedModifier, 0f)) return;
            float dirMultiplier = forward ? 1f : -1f;
            Vector3 delta = profile1.Evaluate(Time.time, dirMultiplier, speedModifier)
                          + profile2.Evaluate(Time.time, dirMultiplier, speedModifier)
                          + profile3.Evaluate(Time.time, dirMultiplier, speedModifier)
                          + profile4.Evaluate(Time.time, dirMultiplier, speedModifier);
            transform.Rotate(delta, Space.Self);
        }
    }
}
