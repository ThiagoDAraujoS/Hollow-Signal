using UnityEngine;

namespace Utilities{
    public class Snap : MonoBehaviour{
        public Transform target;

        private void Update() => transform.position = target.position;
    }
}