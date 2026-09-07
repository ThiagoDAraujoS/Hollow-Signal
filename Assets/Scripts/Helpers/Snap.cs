using UnityEngine;

namespace Helpers{
    public class Snap : MonoBehaviour{
        public Transform target;

        private void Update() => transform.position = target.position;
    }
}