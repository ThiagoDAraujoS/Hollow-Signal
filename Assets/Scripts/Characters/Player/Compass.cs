using UnityEngine;

namespace Characters.Player{
    public class Compass : MonoBehaviour{
        public Vector3 Forward => (ForwardTransform.position - transform.position).normalized;
        public Vector3 Right   => (RightTransform.position - transform.position).normalized;
        public Vector3 Back    => (BackTransform.position - transform.position).normalized;
        public Vector3 Left    => (LeftTransform.position - transform.position).normalized;

        public Transform ForwardTransform{ get; private set; }
        public Transform RightTransform  { get; private set; }
        public Transform BackTransform   { get; private set; }
        public Transform LeftTransform   { get; private set; }

        [SerializeField] private float acceleration = 10f;
    
        private Transform _anchor;
    
        private void Awake(){
            _anchor  = Camera.main != null ? Camera.main.transform : transform;
        
            ForwardTransform = transform.Find("Forward");
            RightTransform   = transform.Find("Right");
            BackTransform    = transform.Find("Back");
            LeftTransform    = transform.Find("Left");
        } 
        private void UpdateAnchor(Transform anchor) => _anchor = anchor;

        private void Update(){
            Quaternion targetRotation = Quaternion.Euler(0f, _anchor.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * acceleration);
        }

        private void OnDrawGizmos(){
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 1);
        }
    }
}
