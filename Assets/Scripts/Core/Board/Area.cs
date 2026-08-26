using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Core.Board{
    [RequireComponent(typeof(ZoneShape))]
    public class Area : MonoBehaviour{
        [HideInInspector] public AreaManager manager;
        
        public Area[]    neighbors;
        public ZoneShape shape;
        public Vector3[] anchors = new Vector3[5];

        public void Awake(){
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                transform.position = hit.position;
        }

        public void OnValidate(){
            if (shape == null)
                shape = GetComponent<ZoneShape>();
#if UNITY_EDITOR
            _gizmoColor = Color.HSVToRGB(Random.value, 1f, Random.Range(0.6f, 1f));
#endif
        }


#if UNITY_EDITOR
        private Color _gizmoColor;
        
        private void OnDrawGizmos(){
            if (manager == null || manager.navmeshPolygon2D == null)
                return;

            Vector2 p = To2D(transform.position);

            // START WITH NAVMESH POLYGON
            List<Vector2> poly = new(manager.navmeshPolygon2D);

            // CLIP AGAINST EACH NEIGHBOR
            foreach (Area area in neighbors){
                if (area == null) continue;
                Vector2 n = To2D(area.transform.position);
                poly = Clip(poly, p, n);
            }

            // DRAW FINAL POLYGON
            Gizmos.color = _gizmoColor;
            DrawPolygon(poly, transform.position.y + 0.05f);

            Gizmos.color = _gizmoColor*0.5f;
            foreach (Area area in neighbors){
                
                Vector3 start      = transform.position;
                Vector3 end        = area.transform.position;
                Vector3 sToEVector = end - start;
                Vector3 center     = start + sToEVector * 0.5f;
                Vector3 lineEnd    = (sToEVector.magnitude * 0.5f - 0.1f) * sToEVector.normalized + start;
     
                Gizmos.DrawLine(start, lineEnd);
                Handles.color = Color.red;
                Handles.DrawWireDisc(center, Vector3.up, 0.1f);
            }
        }
        
        private static Vector2 To2D(Vector3 pos) => new(pos.x, pos.z);

        private static List<Vector2> Clip(List<Vector2> poly, Vector2 p, Vector2 n){
            List<Vector2> output = new();

            Vector2 mid    = (p + n) * 0.5f;
            Vector2 normal = (p - n).normalized;

            for (int i = 0; i < poly.Count; i++){
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Count];

                bool aIn = Vector2.Dot(a - mid, normal) >= 0;
                bool bIn = Vector2.Dot(b - mid, normal) >= 0;

                if (aIn && bIn){
                    output.Add(b);
                }
                else if (aIn){
                    output.Add(Intersect(a, b, mid, normal));
                }
                else if (bIn){
                    output.Add(Intersect(a, b, mid, normal));
                    output.Add(b);
                }
            }

            return output;
        }

        private static Vector2 Intersect(Vector2 a, Vector2 b, Vector2 mid, Vector2 normal){
            Vector2 ab = b - a;
            float   t  = Vector2.Dot(mid - a, normal) / Vector2.Dot(ab, normal);
            return a + ab * t;
        }

        private static void DrawPolygon(List<Vector2> poly, float y){
            if (poly.Count < 2) return;

            for (int i = 0; i < poly.Count; i++){
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Count];

                Gizmos.DrawLine(
                    new Vector3(a.x, y, a.y),
                    new Vector3(b.x, y, b.y)
                );
            }
        }
#endif
    }
}