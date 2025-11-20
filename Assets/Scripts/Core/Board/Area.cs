using UnityEngine;

namespace Core.Board{
    public class Area : MonoBehaviour
    {
        public Area[] areas;
        
#if UNITY_EDITOR
        private Color _gizmoColor;
        private void  OnValidate()=> _gizmoColor = Color.HSVToRGB(Random.value, 1f, Mathf.Lerp(0.6f, 1f,Random.value));
        private void OnDrawGizmos(){
            Gizmos.color = _gizmoColor;
            foreach (Area area in areas)
                Gizmos.DrawLine((transform.position+area.transform.position)/2.0f, transform.position);
        }
#endif
    }
}

