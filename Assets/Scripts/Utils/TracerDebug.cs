using UnityEngine;

namespace Utils{
    public class TracerDebug : MonoBehaviour {
        public float range = 10f;
        public float step  = 0.5f;

        private void OnDrawGizmos() {
            if (!Application.isPlaying) return;

            // Trace forward from this object
            Vector3 hitPoint = NavMeshTracer.Trace(
                transform.position,
                transform.forward,
                range,
                step,
                1.0f,
                (pos) => false // Predicate always returns false, so it walks until it falls off
            );

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, hitPoint);
            Gizmos.DrawWireSphere(hitPoint, 0.3f);
        }
    }
}