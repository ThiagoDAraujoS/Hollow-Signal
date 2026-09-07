using UnityEngine;

namespace Helpers{
    public class Spin : MonoBehaviour{
        public Vector3 speed;

        private void Update(){
            if (speed != Vector3.zero)
                transform.Rotate(speed * Time.deltaTime, Space.Self);
        }
    }
}