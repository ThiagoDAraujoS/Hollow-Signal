using UnityEngine;

namespace Utilities{
    public class CurveSpin : MonoBehaviour{
        public float period = 1f;
        public AnimationCurve step = AnimationCurve.Linear(0, 1, 1, 1);
        public Vector3 spinSpeed;

        /// <summary>Rotates transform by spinSpeed modulated by the step curve over the period.</summary>
        private void LateUpdate(){
            // Breaking fail-fast paradigm here to allow zero period in inspector without crashing with NaN rotation.
            if (period <= 0f) return;
            float time = (Time.time % period) / period;
            transform.Rotate(spinSpeed * (step.Evaluate(time) * Time.deltaTime), Space.Self);
        }
    }
}
